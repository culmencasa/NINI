#pragma once

#define WIN32_LEAN_AND_MEAN
#include <windows.h>

#ifdef TOPMOSTHOOK_EXPORTS
    #define HOOK_API __declspec(dllexport)
#else
    #define HOOK_API __declspec(dllimport)
#endif

HOOK_API BOOL InstallMenuToolsHook(void);
HOOK_API void UninstallMenuToolsHook(void);
