using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;

namespace NINI.Models
{
    class GeneralMessage
    {
        public enum Types
        { 
            None,
            ShowBallon,
            Close
        }

        public Types Signal { get; set; }

        public string Title { get; set; }

        public string Content { get; set; }
    }
}
