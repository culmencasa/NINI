using Microsoft.Win32.TaskScheduler;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Windows.Forms;

namespace NINI.Install
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //bool result = IsExists("Jazz");

            //foreach (IRegisteredTask item in GetAllTasks())
            //{
                
            //    Console.WriteLine(item.Xml.ToString());
            //}
        }
        private void button1_Click(object sender, EventArgs e)
        {
            // Run a program every day on the local machine
            //TaskService.Instance.AddTask("JazzTray", QuickTriggerType.Logon, @"C:\Works\NINI\publish\NINI.exe", "");

            string taskName = "JazzTray";
            string taskFile = @"C:\Works\NINI\publish\NINI.exe";
            string taskDir = @"C:\Works\NINI\publish\";


            using (TaskService ts = new TaskService())
            {
                Task t = ts.GetTask(taskName);
                if (t != null)
                {
                    ts.RootFolder.DeleteTask(taskName);
                }

                TaskDefinition td = ts.NewTask();
                td.RegistrationInfo.Description = "Some shortcuts in system tray";
                td.Triggers.Add(new LogonTrigger());
                td.Actions.Add(new ExecAction(taskFile, taskDir));
                td.Principal.RunLevel = TaskRunLevel.Highest;
                ts.RootFolder.RegisterTaskDefinition(taskName, td);

            }

            MessageBox.Show("安装成功");

        }
    }
}
