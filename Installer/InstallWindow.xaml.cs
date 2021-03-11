using Microsoft.Win32;
using Microsoft.Win32.TaskScheduler;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Shapes;
using Utils.Misc;

namespace Installer
{
    public partial class InstallWindow : Window
    {
        public InstallWindow()
        {
            InitializeComponent();

            string defaultFolderName = "NINI";
            string defaultInstallPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), defaultFolderName);

            InstallPath = defaultInstallPath;
        }

        public string InstallPath
        {
            get
            {
                return txtInstallPath.Text;
            }
            set
            {
                txtInstallPath.Text = value;
            }
        }

        private void btnSelectInstallPath_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "选择安装目标文件夹",
                UseDescriptionForTitle = true,  
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                this.InstallPath = dialog.SelectedPath;
            }
        }

        // 点击Install按钮
        private void btnInstall_Click(object sender, RoutedEventArgs e)
        {
            string sourcePath = AppDomain.CurrentDomain.BaseDirectory;

            // check integrity
            foreach (string file in GetFiles())
            {
                string sourceFileName = System.IO.Path.Combine(sourcePath, file);
                if (!File.Exists(sourceFileName))
                {
                    MessageBox.Show("缺少文件:" + file, "错误", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                    return;
                }
            }

            string installPath = this.InstallPath;
            string schedulerTaskFile = "NINI.exe";
            string schedulerTaskFullPath = System.IO.Path.Combine(installPath, schedulerTaskFile);

            // create directory
            if (!Directory.Exists(installPath))
            {
                try
                {
                    Directory.CreateDirectory(installPath);
                }
                catch (System.UnauthorizedAccessException)
                {
                    MessageBox.Show("请使用管理员身份运行.");
                    return;
                }
            }

            foreach (string file in GetFiles())
            {
                // 源文件全路径
                string sourceFileName = System.IO.Path.Combine(sourcePath, file);
                // 目标文件全路径(目标路径+相对路径文件名)
                string destFileName = System.IO.Path.Combine(installPath, file);

                // 创建子文件夹
                string destPath = destFileName.Substring(0, destFileName.LastIndexOf("\\"));
                if (!Directory.Exists(destPath))
                {
                    Directory.CreateDirectory(destPath);
                }

                // 复制文件
                File.Copy(sourceFileName, destFileName, true);
            }

            // setup parameters for the logon scheduler task.
            string taskName = "NINITray";
            string taskFile = schedulerTaskFullPath;
            string taskDir = installPath;

            // create a scheduler task
            #region 计划任务

            using (TaskService ts = new TaskService())
            {
                Task t = ts.GetTask(taskName);
                if (t != null)
                {
                    ts.RootFolder.DeleteTask(taskName);
                }

                TaskDefinition td = ts.NewTask();
                td.RegistrationInfo.Description = "";
                td.Triggers.Add(new LogonTrigger());
                td.Actions.Add(new ExecAction(taskFile, taskDir));
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

                ts.RootFolder.RegisterTaskDefinition(taskName, td);
            }

            #endregion

            #region 守护进程

            string consoleFileName = "NINI.Console.exe";
            string consoleFullPath = System.IO.Path.Combine(installPath, consoleFileName);

            if (File.Exists(consoleFullPath))
            {
                Process.Start(consoleFullPath, new[] { "install" }).WaitForExit();
            }

            #endregion

            this.pbPercent.Value = pbPercent.Maximum;

            Registry.LocalMachine.OpenSubKey("SOFTWARE", true).CreateSubKey("NINI").SetValue("Path", installPath);


            MessageBox.Show("Install completed.");
            Application.Current.Shutdown();
        }


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
