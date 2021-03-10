using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace NINI.Console
{
    /// <summary>
    /// 守护进程类
    /// </summary>
    class Stub
    {
        private readonly Timer _timer;
        /// <summary>
        /// 检测周期(秒)
        /// </summary>
        private int _monitorInterval = 10;

        /// <summary>
        /// 主程序
        /// </summary>
        private TrunkInfo _trunk { get; set; }

        public Stub()
        {
            _trunk = new TrunkInfo
                {
                    ProcessName = "NINI",	 
                    AppDisplayName = "NINI",
                    AppFilePath = AppDomain.CurrentDomain.BaseDirectory + @"\\NINI.exe" 
                };
            _timer = new Timer(_monitorInterval * 1000) { AutoReset = true };
            _timer.Elapsed += (sender, eventArgs) => Monitor();
        }


        /// <summary>
        /// 守护应用程序的方法
        /// </summary>
        private void Monitor()
        {
            if (Process.GetProcessesByName(_trunk.ProcessName).Length == 0)
            {
                try
                {
                    ProcessExtensions.StartProcessAsCurrentUser(_trunk.AppFilePath, _trunk.Args);
                }
                catch (Exception ex)
                {
                    EventLog.WriteEntry("NINI Console Service:", ex.Message, EventLogEntryType.Error);
                }
            }
        }

        /// <summary>
        /// 服务启动时, 启动计时器
        /// </summary>
        public void Start()
        {
            _timer.Start();
        }

        /// <summary>
        /// 服务停止时, 停止计时器
        /// </summary>
        public void Stop()
        {
            _timer.Stop();
        }
    }
}
