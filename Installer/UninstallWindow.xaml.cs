using Microsoft.Win32;
using Microsoft.Win32.TaskScheduler;
using System;
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // 1.kill process of tray program
            KillProcesses("NINI");

            string installPath = AppDomain.CurrentDomain.BaseDirectory;

            #region 卸载守护进程

            string consoleFileName = "NINI.Console.exe";
            string consoleFullPath = System.IO.Path.Combine(installPath, consoleFileName);

            if (File.Exists(consoleFullPath))
            {
                Process.Start(consoleFullPath, new[] { "uninstall" }).WaitForExit();
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
            MessageBox.Show("Uninstall completed.");

            // 6.afterwards, clean dir
            DeleteItselfByCMD(installPath);

            // 7.quit
            Application.Current.Shutdown();
        }


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

    }
}
