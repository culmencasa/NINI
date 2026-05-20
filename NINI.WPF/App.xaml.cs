using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Win32;
using MVVMLib;
using NINI.Helper;
using NINI.Models;
using NINI.ViewModels;
using NINI.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace NINI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        #region Application成员

        public App()
        { 
        }

        /// <summary>
        /// 程序启动
        /// </summary>
        /// <param name="e"></param>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            #region 唯一实例判断

            bool isFirstSyncElement;
            _ = new Mutex(false, AppConst.APP_NAME, out isFirstSyncElement);
            if (isFirstSyncElement == false)
            {
                App.Current.Shutdown();
                Environment.Exit(0);
                return;
            }
            else
            {
                Process SameAppProc = GetAppProcess();
                if (SameAppProc != null)
                {
                    // 如果找到已有的实例则前置
                    //Win32.SetForegroundWindow(SameAppProc.MainWindowHandle);

                    // MessageBox.Show("程序已启动.");

                    App.Current.Shutdown();
                    Environment.Exit(0);
                    return;
                }

            }


            #endregion

            #region 如果守护进程未启动, 则以管理员启动

            StartConsoleService();
            //if (!IsAdministrator())
            //{
            //    RunThisAsAdmin();
            //    return;
            //}

            #endregion

            _notifyIcon = (TaskbarIcon)FindResource("MyNotifyIcon");


            //Start HttpServer - 注释：TodoWeb 目录不存在，暂时禁用
            // HttpServer = new SimpleHttpServer(AppDomain.CurrentDomain.BaseDirectory + "\\TodoWeb","127.0.0.1", 13554);
            // HttpServer.Start();


            SimpleMessenger.Default.Subscribe<GeneralMessage>(this, HandleGeneralMessage);
            SimpleMessenger.Default.Subscribe<NotifyIconViewMessage>(this, HandleSimpleCommand);


            // 自启动任务
            SimpleMessenger.Default.Publish(new NotifyIconViewMessage()
            {
                Signal = NotifyIconViewMessage.Signals.SyncTime,
                Parameter = "Slient"
            });

            //PreloadTodoWindow();

            //WindowManager.Single<ToDoComment>().Show();

            

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



            // 初始化热键服务
            _hotkeyService = new HotkeyService();
            _hotkeyService.LoadSettings();
            _hotkeyService.KeyPressed += new EventHandler<KeyPressedEventArgs>(hook_KeyPressed);

            // 注册所有热键
            var conflicted = _hotkeyService.RegisterAll();
            if (conflicted.Count > 0)
            {
                var names = string.Join(", ", conflicted.ConvertAll(h => h.Name));
                _notifyIcon?.ShowBalloonTip(
                    "快捷键冲突",
                    $"以下快捷键可能与其他程序冲突: {names}",
                    BalloonIcon.Warning
                );
            }

            // 安装全局窗口置顶钩子
            try
            {
                _hookManager = new SystemMenuService();
                _hookManager.Install();
                Debug.WriteLine("[App] Global hook installed successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[App] Failed to install global hook: {ex.Message}");
                // 可选：显示托盘通知
                _notifyIcon?.ShowBalloonTip(
                    "钩子安装失败",
                    $"窗口置顶功能不可用: {ex.Message}",
                    BalloonIcon.Error
                );
            }
        }

        /// <summary>
        /// 获取热键服务实例
        /// </summary>
        public static HotkeyService? GetHotkeyService()
        {
            return Instance?._hotkeyService;
        }

        /// <summary>
        /// 程序退出
        /// </summary>
        /// <param name="e"></param>
        protected override void OnExit(ExitEventArgs e)
        {
            _todoProcess?.Kill();

            SimpleMessenger.Default.Unsubscribe(this);
            _notifyIcon?.Dispose();
            _hotkeyService?.Dispose();

            // 卸载全局钩子
            _hookManager?.Uninstall();
            _hookManager?.Dispose();

            base.OnExit(e);


            var locator = (ViewModelLocator)FindResource("Locator");
            locator?.Dispose();
            HttpServer?.Stop();

            WindowManager.CloseAll();
        }

        #endregion

        #region 静态

        public static App Instance
        {
            get
            {
                return (App)Current;
            }
        }
        #endregion

        #region 私有字段

        private TaskbarIcon _notifyIcon;
        private Process _todoProcess;
        private HotkeyService _hotkeyService;
        private SystemMenuService _hookManager;

        #endregion

        #region 属性

        /// <summary>
        /// Http服务
        /// </summary>
        public SimpleHttpServer HttpServer { get; private set; }

        #endregion

        #region 事件处理

        private async void hook_KeyPressed(object sender, KeyPressedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"热键按下: {e.Modifier}+{e.Key}");
            if (e.Key == System.Windows.Forms.Keys.T)
            {
                PromptWindow.ShowInfo("CTRL+ALT+T", "终端"); // 默认无副标题，无滚动条



                Process p = new Process();
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.Verb = "runas";
                p.StartInfo.WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                p.StartInfo.FileName = "cmd";
                p.StartInfo.Arguments = "";
                p.Start();
            }
            else if (e.Key == System.Windows.Forms.Keys.R)
            {
                if (Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop") != null)
                {
                    string file = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop\").GetValue("SCRNSAVE.EXE").ToString();
                    Process.Start(file, "/r");
                }

            }
            else if (e.Key == System.Windows.Forms.Keys.P)
            {
                var netInfo = NotifyIconViewModel.GetLocalIPString();

                //PromptWindow.ShowInfo("CTRL+ALT+P", $"{netInfo.IP}{Environment.NewLine}{netInfo.Gateway}");

                PromptWindow.ShowAction("CTRL+ALT+P", $"{netInfo.IP}", $"本地IPv4地址", 5000, () =>
                {
                });
            }
            else if (e.Key == System.Windows.Forms.Keys.F12)
            {
                // 必须在 UI 线程打开窗口
                Application.Current.Dispatcher.Invoke(() =>
                {

                    PromptWindow.ShowAction("CTRL+F12", "关闭显示器", "按 ESC 取消", 3000, () =>
                    {
                        // 执行 P/Invoke 关闭
                        MonitorController.TurnOff();
                    });
                });
            } 
        }

        private void HandleGeneralMessage(GeneralMessage msg)
        {
            switch (msg.Signal)
            {
                case GeneralMessage.Types.ShowBallon:
                    _notifyIcon?.ShowBalloonTip(msg.Title, msg.Content, BalloonIcon.Info);
                    break;
                default:
                    break;
            }
        }


        /// <summary>
        /// 处理任务栏图标命令
        /// </summary>
        /// <param name="msg"></param>
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

        #endregion

        #region 私有方法

        /// <summary>
        /// 重新以管理员身份运行
        /// </summary>
        private void RunThisAsAdmin()
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

        /// <summary>
        /// 判断是否管理员身份运行
        /// </summary>
        /// <returns></returns>
        private bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        /// <summary>
        /// 获取程序的进程
        /// </summary>
        /// <returns></returns>
        private Process GetAppProcess()
        {
            Process current = Process.GetCurrentProcess();
            Process[] processes = Process.GetProcessesByName(current.ProcessName);

            foreach (Process process in processes)
            {
                if (process.Id != current.Id)
                {
                    //注释: core程序不能这样判断相同路径(GetExecutingAssembly()有可能是dll而不是exe)
                    //if (current.MainModule.FileName == Assembly.GetExecutingAssembly().Location.Replace("/", "\\"))
                    //{
                    //    return process;
                    //}

                    return process;
                }
            }
            return null;
        }


        /// <summary>
        /// 预加载Todo窗体
        /// </summary>
        private void PreloadTodoWindow()
        {
            var todoWindow = WindowManager.Single<TodoWindow>();
            // 让窗体不显示在屏幕内
            todoWindow.WindowStartupLocation = WindowStartupLocation.Manual;
            todoWindow.Top = Int32.MinValue;
            todoWindow.Left = Int32.MinValue;
            todoWindow.ShowActivated = false;
            todoWindow.ShowInTaskbar = false;

            // 触发Load事件，开始加载网页
            todoWindow.Show();

            int t1 = Environment.TickCount;

            // 3秒后恢复
            var preloadWaiter = new DispatcherTimer(DispatcherPriority.Background);
            preloadWaiter.Interval = TimeSpan.FromMilliseconds(100);
            preloadWaiter.Tick += (t, e) =>
            {
                int t2 = Environment.TickCount;
                if (todoWindow.WebContentLoaded)
                {
                    preloadWaiter.Stop();
                    Debug.WriteLine(t2 - t1);
                    todoWindow.Hide();
                    todoWindow.ShowActivated = true;
                }
                else if (t2 - t1 > 1500) // 超过1秒关闭
                {
                    preloadWaiter.Stop();
                    todoWindow.Hide();
                    todoWindow.ShowActivated = true;
                }
            };
            preloadWaiter.Start();
        }

        #endregion

        #region 公开方法


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

        public void StartConsoleService()
        {
            string consoleProcessName = AppConst.CONSOLE_PROCESS_NAME;
            string consoleService = AppDomain.CurrentDomain.BaseDirectory + $"{consoleProcessName}.exe";
            if (File.Exists(consoleService))
            {
                if (Process.GetProcessesByName(consoleProcessName).Length == 0)
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.FileName = consoleService;
                    startInfo.Arguments = " start";
                    startInfo.Verb = "runas"; // 以管理员身份运行
                    startInfo.UseShellExecute = true;
                    startInfo.CreateNoWindow = true;

                    Process processTemp = new Process();
                    processTemp.StartInfo = startInfo;
                    processTemp.EnableRaisingEvents = true;
                    try
                    {
                        processTemp.Start();
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
            }
        }

        public void StopConsoleService()
        {
            string consoleProcessName = AppConst.CONSOLE_PROCESS_NAME;
            string consoleService = AppDomain.CurrentDomain.BaseDirectory + $"{consoleProcessName}.exe";
            if (File.Exists(consoleService))
            {
                if (Process.GetProcessesByName(consoleProcessName).Length > 0)
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.FileName = consoleService;
                    startInfo.Arguments = " stop";
                    startInfo.Verb = "runas"; // 以管理员身份运行
                    startInfo.UseShellExecute = true;
                    startInfo.CreateNoWindow = true;

                    Process processTemp = new Process();
                    processTemp.StartInfo = startInfo;
                    processTemp.EnableRaisingEvents = true;
                    try
                    {
                        processTemp.Start();
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
            }
        }

        #endregion
    }
}
