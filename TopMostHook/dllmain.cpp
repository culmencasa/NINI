// dllmain.cpp : 定义 DLL 应用程序的入口点。
#include "pch.h"


#pragma region 定义一个跨进程共享的数据段


#pragma data_seg(".shared")
HHOOK g_hHookHandle = NULL;           // 全局钩子句柄
HINSTANCE g_hInstanceHandle = NULL;    // DLL 自身的实例句柄
#pragma data_seg()
#pragma comment(linker, "/SECTION:.shared,RWS") // 设置该数据段为 可读、可写、可共享(S)

// 自定义菜单项的 ID
#define IDM_TOPMOST (WM_USER + 0x1001)

#pragma endregion


// 参考 MenuTools 的实现
namespace SystemWindowMenu_Tools {


	BOOL IsMyMenuInstalled(HMENU hMenu)
	{
		return (GetMenuState(hMenu, IDM_TOPMOST, MF_BYCOMMAND) != -1);
	}

	void UpdateMenuStatus(HWND hWnd, HMENU hMenu)
	{
		LONG_PTR style = GetWindowLongPtr(hWnd, GWL_EXSTYLE);
		if (style & WS_EX_TOPMOST) {
			CheckMenuItem(hMenu, IDM_TOPMOST, MF_BYCOMMAND | MF_CHECKED);
		}
		else {
			CheckMenuItem(hMenu, IDM_TOPMOST, MF_BYCOMMAND | MF_UNCHECKED);
		}
	}


	static VOID CALLBACK RefreshMenuTimer(HWND hWnd, UINT uMsg, UINT_PTR idEvent, DWORD dwTime)
	{
		KillTimer(hWnd, idEvent);
		HMENU hMenu = GetSystemMenu(hWnd, FALSE);
		if (hMenu) {
			UpdateMenuStatus(hWnd, hMenu);
		}
	}

	void InstallMenu(HWND hWnd, HMENU hMenu)
	{
		if (!hMenu || IsMyMenuInstalled(hMenu))
			return;

		// 找到"关闭"(SC_CLOSE)的位置
		int closePos = -1;
		int itemCount = GetMenuItemCount(hMenu);
		for (int i = 0; i < itemCount; i++)
		{
			if (GetMenuItemID(hMenu, i) == SC_CLOSE)
			{
				closePos = i;
				break;
			}
		}

		if (closePos == -1)
		{
			AppendMenuW(hMenu, MF_SEPARATOR, 0, NULL);
			AppendMenuW(hMenu, MF_STRING, IDM_TOPMOST, L"窗口置顶");
		}
		else
		{
			InsertMenuW(hMenu, closePos, MF_BYPOSITION | MF_SEPARATOR, 0, NULL);

			InsertMenuW(hMenu, closePos, MF_BYPOSITION | MF_STRING, IDM_TOPMOST, L"窗口置顶");
		}

		UpdateMenuStatus(hWnd, hMenu);
	}

	void UninstallMenu(HMENU hMenu)
	{
		if (!hMenu || !IsMyMenuInstalled(hMenu))
			return;

		// 找自定义菜单位置
		int itemCount = GetMenuItemCount(hMenu);
		int targetPos = -1;
		for (int i = 0; i < itemCount; i++)
		{
			if (GetMenuItemID(hMenu, i) == IDM_TOPMOST)
			{
				targetPos = i;
				break;
			}
		}

		if (targetPos == -1)
			return;

		if (targetPos + 1 < GetMenuItemCount(hMenu))
		{
			UINT state = GetMenuState(hMenu, targetPos + 1, MF_BYPOSITION);
			if (state & MF_SEPARATOR)
			{
				DeleteMenu(hMenu, targetPos + 1, MF_BYPOSITION);
			}
		}

		DeleteMenu(hMenu, targetPos, MF_BYPOSITION);
	}
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
	if (nCode < 0) {
		return CallNextHookEx(g_hHookHandle, nCode, wParam, lParam);
	}


	// 转换成 WH_CALLWNDPROCRET 钩子的消息结构体 CWPRETSTRUCT
	CWPRETSTRUCT* pMsg = (CWPRETSTRUCT*)lParam;
	if (!pMsg || !pMsg->hwnd) {
		return CallNextHookEx(g_hHookHandle, nCode, wParam, lParam);
	}

	// 菜单弹出时，加入自定义菜单项
	if (pMsg->message == WM_INITMENUPOPUP || pMsg->message == WM_INITMENU)
	{
		HMENU hMenu = (HMENU)pMsg->wParam;
		// 侧面验证是否为系统菜单
		if (hMenu && GetMenuState(hMenu, SC_CLOSE, MF_BYCOMMAND) != -1)
		{
			SystemWindowMenu_Tools::InstallMenu(pMsg->hwnd, hMenu);
		}
	}
	// 响应用户点击事件
	else if (pMsg->message == WM_SYSCOMMAND)
	{
		if ((pMsg->wParam & 0xFFFF) == IDM_TOPMOST)
		{
			LONG_PTR style = GetWindowLongPtr(pMsg->hwnd, GWL_EXSTYLE);

			// 切换置顶状态
			BOOL success = FALSE;
			if (style & WS_EX_TOPMOST) {
				success = SetWindowPos(pMsg->hwnd, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
			}
			else {
				success = SetWindowPos(pMsg->hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
			}

			// 为避免SetWindowPos会让菜单又弹出一次，用Timer延时关闭菜单
			if (success) {
				SetTimer(pMsg->hwnd, 1, 50, SystemWindowMenu_Tools::RefreshMenuTimer);
			}
		}
	}
	

	return CallNextHookEx(g_hHookHandle, nCode, wParam, lParam);
}






#pragma region 导出函数实现



/// <summary>
/// 安装钩子
/// </summary>
/// <returns></returns>
HOOK_API BOOL InstallMenuToolsHook()
{
	if (g_hHookHandle != NULL) {
		return TRUE;
	}

	// 使用 WH_CALLWNDPROCRET 钩子，它能在目标窗口处理完消息后拦截到消息
	g_hHookHandle = SetWindowsHookEx(
		WH_CALLWNDPROCRET,
		CallWndRetProc,
		g_hInstanceHandle,
		0
	);

	return (g_hHookHandle != NULL);
}

/// <summary>
/// 枚举窗口回调：卸载每个窗口的自定义菜单
/// </summary>
static BOOL CALLBACK EnumWindowsProc(HWND hWnd, LPARAM lParam)
{
	// 只处理可见的、有系统菜单的窗口
	if (IsWindowVisible(hWnd) && GetWindowLongPtr(hWnd, GWL_STYLE) & WS_SYSMENU)
	{
		HMENU hMenu = GetSystemMenu(hWnd, FALSE);
		if (hMenu)
		{
			SystemWindowMenu_Tools::UninstallMenu(hMenu);
		}
	}
	return TRUE; // 继续枚举
}

/// <summary>
/// 卸载钩子
/// </summary>
/// <returns></returns>
HOOK_API void UninstallMenuToolsHook()
{
	if (g_hHookHandle != NULL)
	{
		// 先卸载钩子，停止注入新进程
		UnhookWindowsHookEx(g_hHookHandle);
		g_hHookHandle = NULL;

		// 遍历所有顶层窗口，清理已注入进程的菜单项
		EnumWindows(EnumWindowsProc, 0);
	}
}


#pragma endregion


#pragma region DLL 的入口点

BOOL APIENTRY DllMain(HMODULE hModule,
	DWORD  ul_reason_for_call,
	LPVOID lpReserved
)
{
	switch (ul_reason_for_call)
	{
	case DLL_PROCESS_ATTACH:
		// DLL被加载到进程时

		if (g_hInstanceHandle == NULL) {

			// 在现代 Windows 里：模块的起始地址(hModule)，就是实例的起始地址。
			g_hInstanceHandle = (HINSTANCE)hModule;
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

#pragma endregion
