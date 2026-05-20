#pragma once

#define WIN32_LEAN_AND_MEAN             // 从 Windows 头文件中排除极少使用的内容
// Windows 头文件
#include <windows.h>

// 宏定义：确保函数以 C 语言编译方式导出，方便外部调用
#ifdef TOPMOSTHOOK_EXPORTS
    // 如果发现了 "TOPMOSTHOOK_EXPORTS" 这个标记，就把 HOOK_API 翻译成 “导出”
    #define HOOK_API __declspec(dllexport)
#else
    // 如果没有发现这个标记，就把 HOOK_API 翻译成 “导入”
    #define HOOK_API __declspec(dllimport)
#endif

extern "C" {
    // 安装全局钩子：传入主程序的模块句柄
    HOOK_API BOOL InstallHook();
    // 卸载全局钩子
    HOOK_API void UninstallHook();
}