using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Utils.Misc.Forms
{
    public static partial class Win32
    {
        [DllImport("kernel32.dll")]
        static extern uint GetLastError();
    }
}
