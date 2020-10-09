using LiteDB;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;

namespace NINI.Models
{
    public class Shortcut
    {
        public ObjectId ShortcutId { get; set; } 

        public string Title { get; set; }

        public string Description { get; set; }

        public string Icon { get; set; }

        public string Command { get; set; }

        [BsonIgnore]
        public ImageSource IconStream { get; set; }
    }
}
