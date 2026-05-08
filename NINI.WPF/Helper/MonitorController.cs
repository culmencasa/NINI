using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace NINI.Helper
{
    public static class MonitorController
    {
        // 定义 Windows API 常量
        private const int WM_SYSCOMMAND = 0x0112;
        private const int SC_MONITORPOWER = 0xF170;
        private const int HWND_BROADCAST = 0xFFFF; // 广播到所有窗口

        // 关闭显示器的参数值
        // 2: 关闭, 1: 低功耗, -1: 打开
        private const int MONITOR_OFF = 2;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        public static void TurnOff()
        {
            // 调用 API 发送关闭显示器信号
            SendMessage((IntPtr)HWND_BROADCAST, WM_SYSCOMMAND, (IntPtr)SC_MONITORPOWER, (IntPtr)MONITOR_OFF);
        }
    }
}
