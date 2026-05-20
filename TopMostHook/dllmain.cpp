// dllmain.cpp : 定义 DLL 应用程序的入口点。
#include "pch.h"


#pragma region 定义一个跨进程共享的数据段


#pragma data_seg(".shared")
HHOOK g_hHook = NULL;           // 全局钩子句柄
HINSTANCE g_hInstDll = NULL;    // DLL 自身的实例句柄
#pragma data_seg()
#pragma comment(linker, "/SECTION:.shared,RWS") // 设置该数据段为 可读、可写、可共享(S)

// 自定义菜单项的 ID（Windows 系统菜单 ID 必须小于 0xF000）
#define IDM_TOPMOST 10001

#pragma endregion

/// <summary>
/// 主函数：DLL 的入口点，Windows 在加载或卸载 DLL 时会调用这个函数
/// </summary>
/// <param name="hModule"></param>
/// <param name="ul_reason_for_call"></param>
/// <param name="lpReserved"></param>
/// <returns></returns>
BOOL APIENTRY DllMain(HMODULE hModule,
	DWORD  ul_reason_for_call,
	LPVOID lpReserved
)
{
	switch (ul_reason_for_call)
	{
	case DLL_PROCESS_ATTACH:
		// DLL被加载到进程时

		if (g_hInstDll == NULL) {

			// 在现代 Windows 里：模块的起始地址(hModule)，就是实例的起始地址。
			g_hInstDll = (HINSTANCE)hModule;
		}
		break;
	case DLL_THREAD_ATTACH:
		// 新线程创建时
		break;
	case DLL_THREAD_DETACH:
		// 线程结束时
		break;
	case DLL_PROCESS_DETACH:
		// DLL被卸载时
		break;
	}
	return TRUE;
}



/// <summary>
/// 钩子回调函数：当全系统发生窗口消息时，Windows 会调用这个函数
/// </summary>
/// <param name="nCode">状态码</param>
/// <param name="wParam">参数 W</param>
/// <param name="lParam">参数 L: 消息的结构体指针</param>
/// <returns></returns>
LRESULT CALLBACK CallWndRetProc(int nCode, WPARAM wParam, LPARAM lParam)
{
	// 如果 nCode 小于 0，说明发生了一些异常或者系统在做别的事
	if (nCode >= 0)
	{
		// 转换成 WH_CALLWNDPROCRET 钩子的消息结构体 CWPRETSTRUCT
		CWPRETSTRUCT* pMsg = (CWPRETSTRUCT*)lParam;

		// 情况 A：捕获到窗口即将弹出系统菜单的消息
		if (pMsg->message == WM_INITMENUPOPUP)
		{
			HMENU hMenu = (HMENU)pMsg->wParam;
			// 检查这个菜单是不是窗口的系统菜单（通常可以通过判断有没有“关闭”菜单项 SC_CLOSE 来侧面验证）
			if (GetMenuState(hMenu, SC_CLOSE, MF_BYCOMMAND) != -1)
			{
				// 检查是否已经添加过我们的菜单项，防止重复添加
				if (GetMenuState(hMenu, IDM_TOPMOST, MF_BYCOMMAND) == -1)
				{
					AppendMenuW(hMenu, MF_SEPARATOR, 0, NULL); // 加个分割线

					// 检查目标窗口当前是否已经是置顶状态，动态赋予勾选状态
					LONG_PTR style = GetWindowLongPtr(pMsg->hwnd, GWL_EXSTYLE);
					UINT flags = MF_STRING;
					if (style & WS_EX_TOPMOST) {
						flags |= MF_CHECKED;
					}

					AppendMenuW(hMenu, flags, IDM_TOPMOST, L"窗口置顶 (C# & DLL)");
				}
			}
		}
		// 情况 B：捕获到用户点击了系统菜单项的消息
		else if (pMsg->message == WM_SYSCOMMAND)
		{
			if ((pMsg->wParam & 0xFFFF) == IDM_TOPMOST)
			{
				// 切换目标窗口的置顶状态
				LONG_PTR style = GetWindowLongPtr(pMsg->hwnd, GWL_EXSTYLE);
				if (style & WS_EX_TOPMOST)
				{
					// 取消置顶
					SetWindowPos(pMsg->hwnd, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
				}
				else
				{
					// 设置置顶
					SetWindowPos(pMsg->hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
				}
			}
		}
	}
	// 传递给下一个钩子（必须调用，否则系统会卡死）
	return CallNextHookEx(g_hHook, nCode, wParam, lParam);
}




#pragma region 导出函数实现



/// <summary>
/// 安装钩子
/// </summary>
/// <returns></returns>
HOOK_API BOOL InstallHook()
{
	if (g_hHook != NULL) {
		return TRUE;
	}

	// 使用 WH_CALLWNDPROCRET 钩子，它能在目标窗口处理完消息后拦截到消息
	g_hHook = SetWindowsHookEx(
		WH_CALLWNDPROCRET,
		CallWndRetProc,
		g_hInstDll,
		0
	);

	return (g_hHook != NULL);
}

/// <summary>
/// 卸载钩子
/// </summary>
/// <returns></returns>
HOOK_API void UninstallHook()
{
	if (g_hHook != NULL)
	{
		UnhookWindowsHookEx(g_hHook);
		g_hHook = NULL;
	}
}


#pragma endregion

