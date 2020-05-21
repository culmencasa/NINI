using Microsoft.Win32;
using Microsoft.Win32.TaskScheduler;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows;

namespace Installer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (!IsAdministrator())
            {
                RunThisAsAdmin();
                return;
            }

            bool uninstall = false;
            if (e.Args != null && e.Args.Length > 0)
            {
                if (e.Args[0] == "-u")
                {
                    uninstall = true;
                }
            }


            if (Registry.LocalMachine.OpenSubKey(@"SOFTWARE\NINI") != null)
            {
                uninstall = true;
            }
             
            if (uninstall)
            {
                new UninstallWindow().ShowDialog();
            }
            else
            {
                new MainWindow().ShowDialog();
            }
            Environment.Exit(0);
        }


        private static bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private static void RunThisAsAdmin()
        {
            if (!IsAdministrator())
            {
                var exe = Process.GetCurrentProcess().MainModule.FileName;
                var startInfo = new ProcessStartInfo(exe)
                {
                    UseShellExecute = true,
                    Verb = "runas",
                    WindowStyle = ProcessWindowStyle.Normal,
                    CreateNoWindow = false
                };
                try
                {
                    Process.Start(startInfo);
                }
                catch (Win32Exception)
                {
                    MessageBox.Show("Installing failed." + Environment.NewLine + "Administator priviledge is required for installing.", "Installer", MessageBoxButton.OK);
                }
                Process.GetCurrentProcess().Kill();
            }
        }

    }
}
