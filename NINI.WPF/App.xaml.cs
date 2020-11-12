using Hardcodet.Wpf.TaskbarNotification;
using MVVMLib;
using NINI.Helper;
using NINI.Models;
using NINI.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Windows; 

namespace NINI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        public App()
        { 
        }


        #region 常量

        public const string APP_NAME = "NINI.WPF";

        #endregion



        /// <summary>
        /// Http服务
        /// </summary>
        public static SimpleHttpServer HttpServer { get; private set; }



        private TaskbarIcon notifyIcon;
        private ViewModelLocator locator;
        private Process todoProcess;



        KeyboardHook hook = new KeyboardHook();
         

        protected override void OnStartup(StartupEventArgs e)
        {

            base.OnStartup(e);

            #region 唯一实例判断

            bool isFirstSyncElement;
            _ = new Mutex(false, APP_NAME, out isFirstSyncElement);
            if (isFirstSyncElement == false)
            {
                Process SameAppProc = GetProcInstance();
                if (SameAppProc != null)
                {
                    // 如果找到已有的实例则前置
                    //Win32.SetForegroundWindow(SameAppProc.MainWindowHandle);
                }

                App.Current.Shutdown();
                Environment.Exit(0);
                return;
            }

            #endregion

            #region 管理员启动

            if (!IsAdministrator())
            {
                RunThisAsAdmin();
                return;
            }

            #endregion

            notifyIcon = (TaskbarIcon)FindResource("MyNotifyIcon");
            locator = (ViewModelLocator)FindResource("Locator");


            //Start HttpServer
            HttpServer = new SimpleHttpServer(AppDomain.CurrentDomain.BaseDirectory + "\\TodoWeb","127.0.0.1", 13554);
            HttpServer.Start();


            SimpleMessenger.Default.Subscribe<GeneralMessage>(this, HandleGeneralMessage);
            SimpleMessenger.Default.Subscribe<NotifyIconViewMessage>(this, HandleSimpleCommand);


            // 自启动任务
            SimpleMessenger.Default.Publish(new NotifyIconViewMessage()
            {
                Signal = NotifyIconViewMessage.Signals.SyncTime,
                Parameter = "Slient"
            });

            WindowManager.Single<TodoWindow>().Preload();

            //todoProcess = Process.Start(AppDomain.CurrentDomain.BaseDirectory + "TodoApp\\TodoApp.exe");


            //string command = "dotnet application.dll --urls=http://*:5123";

            //Process proc = new System.Diagnostics.Process();
            //proc.StartInfo.FileName = AppDomain.CurrentDomain.BaseDirectory + "TodoApp\\TodoApp.exe";
            ////proc.StartInfo.Arguments = AppDomain.CurrentDomain.BaseDirectory + "TodoApp\\TodoApp.dll";
            //proc.StartInfo.UseShellExecute = false;
            //proc.StartInfo.RedirectStandardOutput = true;
            //proc.StartInfo.Verb = "runas";
            //proc.Start();

            //todoProcess = proc;



            hook.KeyPressed += new EventHandler<KeyPressedEventArgs>(hook_KeyPressed);
            try
            {
                hook.RegisterHotKey(ModifierKeys.Control | ModifierKeys.Alt, System.Windows.Forms.Keys.T);
            }
            catch
            {
                notifyIcon?.ShowBalloonTip(DateTime.Now.ToShortTimeString(), "快捷键注册失败", BalloonIcon.Info);
            }

        }

        protected override void OnExit(ExitEventArgs e)
        {
            todoProcess?.Kill();

            SimpleMessenger.Default.Unsubscribe(this);
            notifyIcon?.Dispose();
            base.OnExit(e);


            var locator = (ViewModelLocator)FindResource("Locator");
            locator?.Dispose();
            HttpServer?.Stop();

            WindowManager.CloseAll();
        }

        private void HandleGeneralMessage(GeneralMessage msg)
        {
            switch (msg.Signal)
            {
                case GeneralMessage.Types.ShowBallon:
                    notifyIcon?.ShowBalloonTip(msg.Title, msg.Content, BalloonIcon.Info);
                    break;
                default:
                    break;
            }
        }
        private void HandleSimpleCommand(NotifyIconViewMessage msg)
        {
            switch (msg.Signal)
            {
                case NotifyIconViewMessage.Signals.OpenWindow:
                    {
                        var mainWindow = WindowManager.Cache<MainWindow>();
                        mainWindow.Show(); 

                        if (mainWindow.Visibility != Visibility.Visible)
                            mainWindow.Visibility = Visibility.Visible;

                        if (mainWindow.WindowState == WindowState.Minimized)
                            mainWindow.WindowState = WindowState.Normal;

                        if (!mainWindow.IsActive)
                            mainWindow.Activate();
                    }
                    break;
                case NotifyIconViewMessage.Signals.ShowTodo:
                    {
                        // 废弃
                        TodoWindow todoWindow = WindowManager.Single<TodoWindow>();
                        todoWindow.Show();
                    }
                    break;
                default:
                    break;
            }
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
                }
                Process.GetCurrentProcess().Kill();
            }
        }


        private static bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }


        public static Process GetProcInstance()
        {
            Process current = Process.GetCurrentProcess();
            Process[] processes = Process.GetProcessesByName(current.ProcessName);

            foreach (Process process in processes)
            {
                if (process.Id != current.Id)
                {
                    if (current.MainModule.FileName == Assembly.GetExecutingAssembly().Location.Replace("/", "\\"))
                    {
                        return process;
                    }
                }
            }
            return null;
        }


        static void hook_KeyPressed(object sender, KeyPressedEventArgs e)
        {
            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.Verb = "runas";
            p.StartInfo.WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            p.StartInfo.FileName = "cmd";
            p.StartInfo.Arguments = "";
            p.Start();

        }


    }
}
