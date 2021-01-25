﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;

namespace MiniblinkCore
{
    public interface ILoadResource
    {
        byte[] ByUri(Uri uri);
        string Domain { get; }
    }
}
