using System;
using System.Collections.Generic;
using System.Security.Policy;
using System.Text;

namespace NINI.Models
{
    public class MainViewMessage
    {
        public enum Signals
        {
            None,
            OpenWindow, 
            SyncTime,
        }

        public Signals Signal { get; set; }

        public object Parameter { get; set; }
    }
}
