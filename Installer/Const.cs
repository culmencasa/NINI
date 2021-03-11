using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Installer
{
    static class Const
    {
        /// <summary>
        /// 程序名
        /// <para>默认:NINI</para>
        /// </summary>
        public static string AppName { get; } = "NINI";
        /// <summary>
        /// 计划任务名称
        /// <para>默认: "NINITray"</para>
        /// </summary>
        public static string SchedulerTaskName { get; } = "NINITray";
        /// <summary>
        /// 计划任务执行程序名
        /// <para>默认:NINI.exe</para>
        /// </summary>
        public static string SchedulerExecuteFileName { get; } = "NINI.exe";

        /// <summary>
        /// 服务执行程序名
        /// <para>默认:NINI.Console.exe</para>
        /// </summary>
        public static string ConsoleFileName { get; } = "NINI.Console.exe";
    }
}
