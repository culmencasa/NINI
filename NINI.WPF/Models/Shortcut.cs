using LiteDB;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;

namespace NINI.Models
{
    public class Shortcut
    {
        /// <summary>
        /// Id
        /// </summary>
        public ObjectId ShortcutId { get; set; } 

        /// <summary>
        /// 标题
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 图标的地址
        /// </summary>
        public string Icon { get; set; }

        /// <summary>
        /// 执行命令
        /// </summary>
        public string Command { get; set; }

        /// <summary>
        /// 图标
        /// </summary>
        [BsonIgnore]
        public ImageSource IconStream { get; set; }
    }
}
