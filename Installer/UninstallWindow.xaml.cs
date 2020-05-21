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
            // kill process of tray program
            KillProcesses("NINI");

            // delete a scheduler task
            string taskName = "NINITray";
            using (TaskService ts = new TaskService())
            {
                Task t = ts.GetTask(taskName);
                if (t != null)
                {
                    ts.RootFolder.DeleteTask(taskName);
                }
            }

            // delete files
            foreach (string file in Directory.GetFiles("."))
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

            // delete registry
            Registry.LocalMachine.OpenSubKey("SOFTWARE", true).DeleteSubKey("NINI");
            MessageBox.Show("Uninstall completed.");

            // afterwards
            DeleteItselfByCMD();
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


        private void DeleteItselfByCMD()
        {
            string installPath = AppDomain.CurrentDomain.BaseDirectory;
            try
            {
                string regPath = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\NINI").GetValue("Path").ToString();
                if (!string.IsNullOrEmpty(regPath))
                {
                    installPath = regPath;
                }
            }
            catch { }

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
