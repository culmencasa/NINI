using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace NINI
{
    /// <summary>
    /// TodoWindow.xaml 的交互逻辑
    /// </summary>
    public partial class TodoWindow : SingleWindow
    {
        #region 构造

        public TodoWindow()
        {
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.Manual;


            Loaded += TodoWindow_Loaded;
            this.Activated += TodoWindow_Activated;
            this.Deactivated += TodoWindow_Deactivated;

        }

        #endregion

        #region 关闭系统动画(未用)

        const int DWMWA_TRANSITIONS_FORCEDISABLED = 3;

        [DllImport("dwmapi", PreserveSig = true)]
        static extern int DwmSetWindowAttribute(IntPtr hWnd, int attr, ref int value, int attrLen);

        /*
         
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            // in the form's constructor:
            // (Note: in addition to checking the OS version for DWM support, you should also check
            // that DWM composition is enabled---or at least gracefully handle the function's
            // failure when it is not. Instead of S_OK, it will return DWM_E_COMPOSITIONDISABLED.)
            if (Environment.OSVersion.Version.Major >= 6)
            {
                int value = 1;  // TRUE to disable
                DwmSetWindowAttribute(hwnd,
                                      DWMWA_TRANSITIONS_FORCEDISABLED,
                                      ref value,
                                      Marshal.SizeOf(value));
            }
         */


        #endregion
          
        #region 动画


        DispatcherTimer preloadWaiter;        
        DoubleAnimation showAnimation;
        ManualResetEvent animationDone = new ManualResetEvent(true);
        bool webcontentLoaded = false;

        /// <summary>
        /// 窗体状态
        /// 0 close 
        /// 1 open 
        /// 2 closing 
        /// 3 opening
        /// </summary>
        int windowState = 0;  


        /// <summary>
        /// 窗体激活时显示动画
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TodoWindow_Activated(object sender, EventArgs e)
        {
            int taskbarHeight = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height - System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Height;
            this.Left = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width - this.ActualWidth;
            this.Top = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height - taskbarHeight;

            // 初始化动画
            if (showAnimation == null)
            {
                showAnimation = new DoubleAnimation(
                    fromValue: System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height - taskbarHeight,
                    toValue: System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height - this.ActualHeight - taskbarHeight,
                    new Duration(TimeSpan.FromMilliseconds(200)));
                //showAnimation.AccelerationRatio = 0.5; // 加速度
                showAnimation.Completed += (a, b) =>
                {
                    animationDone.Set();
                    windowState = 1;
                };
            }

            windowState = 3;
            this.BeginAnimation(TopProperty, showAnimation);
            animationDone.WaitOne(TimeSpan.FromSeconds(1));
        }

        /// <summary>
        /// 窗体失去焦点时自动隐藏
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void TodoWindow_Deactivated(object sender, EventArgs e)
        {
            windowState = 2;

            Debug.WriteLine("Hide");
            Close();

            // 等待窗体关闭完成
            await Task.Delay(TimeSpan.FromMilliseconds(200));
            windowState = 0;
        }




        #endregion


        #region 公开方法

        /// <summary>
        /// 切换显示隐藏
        /// </summary>
        public void SwitchShowHide()
        {
            if (windowState == 0)
            {
                Debug.WriteLine("Show");
                base.Show();
                Activate();

            }
            else if (windowState == 1)
            {
                Debug.WriteLine("Hide on clicking tray icon");
                // 一般不会进来，因为Deactived事件触发在先
                Close();
            }
            else
            {
                Debug.WriteLine("Ignored window state:" + windowState);
            }
        }

        /// <summary>
        /// 预加载网页
        /// </summary>
        public void Preload()
        {
            // 让窗体不显示在屏幕内
            this.WindowStartupLocation = WindowStartupLocation.Manual;
            this.Top = Int32.MinValue;
            this.Left = Int32.MinValue;
            this.ShowActivated = false; 
            this.ShowInTaskbar = false;


            // 触发Load事件，开始加载网页
            base.Show();
            


            int t1 = Environment.TickCount;

            // 3秒后恢复
            preloadWaiter = new DispatcherTimer(DispatcherPriority.Background);
            preloadWaiter.Interval = TimeSpan.FromMilliseconds(100);
            preloadWaiter.Tick += (t, e) =>
            {
                int t2 = Environment.TickCount;
                if (webcontentLoaded)
                {
                    preloadWaiter.Stop();
                    Debug.WriteLine(t2 - t1);
                    this.Hide();
                    ShowActivated = true;
                }
                else if (t2 - t1 > 1500) // 超过1秒关闭
                {
                    preloadWaiter.Stop();
                    this.Hide();
                    ShowActivated = true;
                }
            };
            preloadWaiter.Start();


        }



        #endregion


        #region Window事件

        private void TodoWindow_Loaded(object sender, RoutedEventArgs e)
        {
            webcontentLoaded = false;

            string url = "https://to-do.live.com/tasks/";
            //url = "http://127.0.0.1:13554";
            //url = "https://tasks.google.com/embed/?origin=https://calendar.google.com&fullWidth=1"; // 不支持IE, 将来可换成WebView2控件
            browser.Navigate(url);
        }

        #endregion


        #region IE Browser

        public void SuppressScriptErrors(WebBrowser webBrowser, bool Hide)
        {
            FieldInfo fiComWebBrowser = typeof(WebBrowser).GetField("_axIWebBrowser2", BindingFlags.Instance | BindingFlags.NonPublic);
            if (fiComWebBrowser == null) return;

            object objComWebBrowser = fiComWebBrowser.GetValue(webBrowser);
            if (objComWebBrowser == null) return;

            objComWebBrowser.GetType().InvokeMember("Silent", BindingFlags.SetProperty, null, objComWebBrowser, new object[] { Hide });
        }

        private void browser_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            SuppressScriptErrors((WebBrowser)sender, true);

            webcontentLoaded = true;

        }


        //todo : 加载失败的界面

        #endregion

    }
}
