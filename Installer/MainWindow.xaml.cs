using Microsoft.Win32;
using Microsoft.Win32.TaskScheduler;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Shapes;
using Utils.Misc;

namespace Installer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // check integrity
            if (!File.Exists("NINI.exe"))
            {
                MessageBox.Show("NINI.exe File not found.");
                return;
            }

            string folderName = "NINI";
            string installPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), folderName);
            string fileName = "NINI.exe";
            string schedulerTaskFile = "NINI.Header.exe";
            string schedulerTaskFullPath = System.IO.Path.Combine(installPath, schedulerTaskFile);

            // create directory
            if (!Directory.Exists(installPath))
            {
                Directory.CreateDirectory(installPath);
            }

            string sourcePath = AppDomain.CurrentDomain.BaseDirectory;
            foreach (string file in GetFiles())
            {
                string sourceFileName = System.IO.Path.Combine(sourcePath, file);
                string destFileName = System.IO.Path.Combine(installPath, file);
                string destPath = destFileName.Substring(0, destFileName.LastIndexOf("\\"));
                if (!Directory.Exists(destPath))
                {
                    Directory.CreateDirectory(destPath);
                }
                File.Copy(sourceFileName, destFileName, true);
            }

            // setup parameters for the logon scheduler task.
            string taskName = "NINITray";
            string taskFile = schedulerTaskFullPath;
            string taskDir = installPath;

            // create a scheduler task
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
