using System;
using System.Collections.Generic;
using System.Text;

namespace NINI
{
    public interface ISingleWindow
    {
        /// <summary>
        /// 设置强制关闭而不是隐藏
        /// </summary>
        bool ForceClose { get; set; }

        /// <summary>
        /// 用隐藏替代关闭
        /// </summary>
        void HideOnClose();
    }
}
