﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniblinkCore
{
    internal delegate IntPtr WndProcCallback(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
}
