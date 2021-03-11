using Microsoft.Win32;
using Microsoft.Win32.TaskScheduler;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Shapes;

namespace Installer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class UninstallWindow : Window
    {
        public UninstallWindow()
        {
            InitializeComponent();
            
        }

        /// <summary>
        /// 点击重装按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnReinstall_Click(object sender, RoutedEventArgs e)
        {
            Debugger.Launch();

            string installPath = null;

            //1.获取安装路径
            try
            {
                string regPath = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\NINI").GetValue("Path").ToString();
                if (!string.IsNullOrEmpty(regPath))
                {
                    installPath = regPath;

                    // 忽略检查文件. 下面只检查服务,计划任务和注册表
                }
                else
                {
                    // 获取程序安装路径失败, 使用默认路径. 
                    installPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), Const.AppName);
                     
                }
            }
            catch { }


            // 2.判断安装程序的位置, 如果与目标位置不一致. 则重新复制文件到目标位置. 
            string sourcePath = AppDomain.CurrentDomain.BaseDirectory;
            if (new Uri(installPath) == new Uri(sourcePath))
            {
                return;
            }
            else
            {
                // 复制文件, 不检查完整性
                foreach (string file in GetFiles())
                {
                    string sourceFileName = System.IO.Path.Combine(sourcePath, file);
                    string destFileName = System.IO.Path.Combine(installPath, file);

                    // 创建子文件夹
                    string destPath = destFileName.Substring(0, destFileName.LastIndexOf("\\"));
                    if (!Directory.Exists(destPath))
                    {
                        Directory.CreateDirectory(destPath);
                    }

                    // 复制文件
                    if (File.Exists(sourceFileName))
                    {
                        File.Copy(sourceFileName, destFileName, true);
                    }
                }
            }


            // 3.修复自启动
            string schedulerTaskFullPath = System.IO.Path.Combine(installPath, Const.SchedulerExecuteFileName);
            using (TaskService ts = new TaskService())
            {
                Task t = ts.GetTask(Const.SchedulerTaskName);
                // 如果计划任务不存在, 则新建
                if (t == null)
                {
                    TaskDefinition td = ts.NewTask();
                    td.RegistrationInfo.Description = "";
                    td.Triggers.Add(new LogonTrigger());
                    td.Actions.Add(new ExecAction(schedulerTaskFullPath, installPath));
                    td.Principal.RunLevel = TaskRunLevel.Highest;
                    td.Settings.WakeToRun = false;
                    td.Settings.RunOnlyIfNetworkAvailable = false;
                    td.Settings.StopIfGoingOnBatteries = false;
                    td.Settings.AllowHardTerminate = true;
                    td.Settings.DisallowStartIfOnBatteries = false;
                    td.Settings.MultipleInstances = TaskInstancesPolicy.IgnoreNew;
                    td.Settings.DeleteExpiredTaskAfter = TimeSpan.Zero; //never
                    td.Settings.ExecutionTimeLimit = TimeSpan.Zero; //never                

                    td.RegistrationInfo.Author = "culmencasa";
                    td.RegistrationInfo.Description = "do small things";
                    td.RegistrationInfo.Documentation = "try to make something useful";

                    ts.RootFolder.RegisterTaskDefinition(Const.SchedulerTaskName, td);
                }
            }

            // 4.修复服务
            string consoleFullPath = System.IO.Path.Combine(installPath, Const.ConsoleFileName);
            if (File.Exists(consoleFullPath))
            {
                Process.Start(consoleFullPath, new[] { "install", "start" });
            }

            // 5.修复注册表
            Registry.LocalMachine.OpenSubKey("SOFTWARE", true).CreateSubKey("NINI").SetValue("Path", installPath);

            MessageBox.Show("Repair completed.", "Cheers", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 点击卸载按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnUninstall_Click(object sender, RoutedEventArgs e)
        {
            // 1.kill process of tray program
            KillProcesses("NINI.Console");
            KillProcesses("NINI");


            string installPath = AppDomain.CurrentDomain.BaseDirectory;

            #region 卸载守护进程

            string consoleFileName = "NINI.Console.exe";
            string consoleFullPath = System.IO.Path.Combine(installPath, consoleFileName);

            if (File.Exists(consoleFullPath))
            {
                Process.Start(consoleFullPath, new[] { " uninstall" });
            }

            #endregion


            // 2.delete a scheduler task
            string taskName = "NINITray";
            using (TaskService ts = new TaskService())
            {
                Task t = ts.GetTask(taskName);
                if (t != null)
                {
                    ts.RootFolder.DeleteTask(taskName);
                }
            }

            // 3.get user install path
            try
            {
                string regPath = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\NINI").GetValue("Path").ToString();
                if (!string.IsNullOrEmpty(regPath))
                {
                    installPath = regPath;
                }
            }
            catch { }


            // 4.delete files
            foreach (string file in Directory.GetFiles(installPath))
            {
                if (file.Contains("Installer.dll") || file.Contains("Installer.exe"))
                    continue;

                try
                {
                    File.Delete(file);
                }
                catch (Exception ex)
                {

                }
            }

            // 5.delete registry
            Registry.LocalMachine.OpenSubKey("SOFTWARE", true).DeleteSubKey("NINI");
            MessageBox.Show("Uninstall completed.", "Goodbye", MessageBoxButton.OK, MessageBoxImage.Information);

            // 6.afterwards, clean dir
            DeleteItselfByCMD(installPath);

            // 7.quit
            Application.Current.Shutdown();
        }

        /// <summary>
        /// 杀掉进程
        /// </summary>
        /// <param name="processName">进程名</param>
        public void KillProcesses(string processName)
        {
            Process[] processes = Process.GetProcessesByName(processName);

            foreach (Process process in processes)
            {
                process.Kill();
            }
        }


        /// <summary>
        /// 使用控制台命令清理安装残余文件
        /// </summary>
        /// <param name="installPath"></param>
        private void DeleteItselfByCMD(string installPath)
        {
            string[] remnants = new string[] {
                System.IO.Path.Combine(installPath, "Installer.exe"),
                System.IO.Path.Combine(installPath, "Installer.dll"),
                System.IO.Path.Combine(installPath, "Microsoft.Win32.TaskScheduler.dll")
            };
            StringBuilder sbDelete = new StringBuilder();
            foreach(string file in remnants)
            {
                sbDelete.Append("& ");
                sbDelete.Append("del \"");
                sbDelete.Append(file);
                sbDelete.Append("\"");
            }

            sbDelete.Append("& rd ");
            sbDelete.Append("\"");
            sbDelete.Append(installPath);
            sbDelete.Append("\"");

            ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", "/C ping 1.1.1.1 -n 1 -w 2000 > Nul " + sbDelete.ToString());
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            psi.CreateNoWindow = true;
            Process.Start(psi);
        }

        /// <summary>
        /// 文件清单
        /// </summary>
        /// <returns></returns>
        private IList<string> GetFiles()
        {
            List<string> files = new List<string>();
            using (StreamReader sr = new StreamReader("manifest.txt"))
            {
                string line;

                while ((line = sr.ReadLine()) != null)
                {
                    if (line.Length > 0)
                    {
                        files.Add(line);
                    }
                }
            }

            return files;
        }
    }
}
