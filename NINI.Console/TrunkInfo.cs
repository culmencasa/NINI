﻿namespace NINI.Console
{
    internal class TrunkInfo
    {
        /// <summary>
        /// 进程中显示的名称
        /// </summary>
        public string ProcessName { get; set; }
        /// <summary>
        /// 应用程序安装路径
        /// </summary>
        public string AppFilePath { get; set; }
        /// <summary>
        /// 应用程序的名称
        /// </summary>
        public string AppDisplayName { get; set; }

        /// <summary>
        /// 参数
        /// </summary>
        public string Args { get; set; }
    }
}