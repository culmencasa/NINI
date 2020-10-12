using System;
using System.Collections.Generic;
using System.Security.Policy;
using System.Text;

namespace NINI.Models
{
    public class NotifyIconViewMessage
    {
        public enum Signals
        {
            None,
            OpenWindow, 
            SyncTime,
            ShowTodo
        }

        public Signals Signal { get; set; }

        public object Parameter { get; set; }
    }
}
