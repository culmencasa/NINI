using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Win32;

namespace NINI.Views
{
    public partial class PromptWindow : Window
    {


        #region 枚举


        public enum PromptTheme
        {
            Dark,
            Light,
        }

        #endregion

        #region 静态成员

        private static PromptWindow singleton;

        /// <summary>
        /// 仅显示单行信息提示 (如: "终端")
        /// </summary>
        public static void ShowInfo(string hotkey, string title)
        {
            ShowAction(hotkey, title, null, 0, null);
        }

        /// <summary>
        /// 显示带倒计时的交互操作 (如: "关闭显示器")
        /// </summary>
        public static void ShowAction(
            string hotkey,
            string title,
            string sub,
            int ms,
            Action action
        )
        {
            // 如果实例存在且没有正在关闭，则复用
            if (singleton != null && singleton.IsLoaded && !singleton._isClosing)
            {
                singleton.Activate(); // 确保窗口在最前
                singleton.RunIslandTransform(hotkey, title, sub, ms, action);
            }
            else
            {
                // 如果窗口不存在或正在关闭，创建新窗口
                singleton = new PromptWindow(hotkey, title, sub, ms, action);
                singleton.Show();
            }
        }


        public static PromptTheme GetSystemTheme()
        {
            try
            {
                using (
                    var key = Registry.CurrentUser.OpenSubKey(
                        @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"
                    )
                )
                {
                    if (key != null)
                    {
                        // AppsUseLightTheme  1 表示明亮模式 0 表示暗黑模式
                        object registryValue = key.GetValue("AppsUseLightTheme");
                        if (registryValue is int i)
                        {
                            return i == 1 ? PromptTheme.Light : PromptTheme.Dark;
                        }
                    }
                }
            }
            catch
            {
            }

            return PromptTheme.Light;
        }

        #endregion

        #region 字段


        private Action _onFinished;
        private bool _isClosing = false; // 防止关闭逻辑被重复触发或冲突

        private System.Windows.Threading.DispatcherTimer _activeTimer;

        #endregion

        #region 构造

        public PromptWindow(
            string hotkey,
            string main,
            string sub = null,
            int ms = 0,
            Action onDone = null
        )
        {
            InitializeComponent();
            singleton = this;

            ApplyTheme(GetSystemTheme());
            UpdateContent(hotkey, main, sub, ms, onDone);

            this.Loaded += (s, e) =>
            {
                SetupStage();

                // 计算并直接设置初始尺寸，不带动画，防止第一次弹出时过小
                SyncIslandSizeStatic();

                // 执行底部滑入动画
                var tt = new TranslateTransform(0, 150);
                IslandContainer.RenderTransform = tt;
                tt.BeginAnimation(
                    TranslateTransform.YProperty,
                    new DoubleAnimation(0, TimeSpan.FromMilliseconds(500))
                    {
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
                    }
                );

                if (ms > 0)
                    StartTimer(ms);
                else
                    AutoClose(2000);
            };
        }

        #endregion


        /// <summary>
        /// 首次显示时计算尺寸
        /// </summary>
        private void SyncIslandSizeStatic()
        {
            MainContentGrid.Measure(new Size(800, double.PositiveInfinity));

            // 直接使用它测出的尺寸，Math.Max 确保最小宽度为 160
            double targetW = Math.Max(160, MainContentGrid.DesiredSize.Width);
            double targetH = MainContentGrid.DesiredSize.Height;

            // 直接赋值，不走 BeginAnimation
            IslandContainer.Width = targetW;
            IslandContainer.Height = targetH;
        }

        /// <summary>
        /// 根据DPI初始化窗体尺寸及位置（仅实现底部居中）
        /// </summary>
        private void SetupStage()
        {
            var source = PresentationSource.FromVisual(this);
            double dpiX = source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
            double dpiY = source?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;
            RECT rc = new RECT();
            SystemParametersInfo(48, 0, ref rc, 0);

            double logicalScreenWidth = (rc.Right - rc.Left) / dpiX;
            double logicalWorkAreaBottom = rc.Bottom / dpiY;

            this.Width = 800;
            this.Height = 300;
            this.Left = (logicalScreenWidth - Width) / 2;
            this.Top = logicalWorkAreaBottom - Height;
        }

        private void UpdateContent(string hotkey, string title, string sub, int ms, Action action)
        {
            _onFinished = action;
            TxtHotkey.Text = string.IsNullOrEmpty(hotkey) ? " " : hotkey.ToUpper();
            TxtMain.Text = title;
            TxtSub.Text = string.IsNullOrEmpty(sub) ? " " : sub;
            TxtSub.Visibility = string.IsNullOrEmpty(sub) ? Visibility.Hidden : Visibility.Visible;

            if (sub == "本地IPv4地址")
            {
                ContentPanel.Visibility = Visibility.Collapsed;
                HorizontalPanel.Visibility = Visibility.Visible;

                CountdownBar.Visibility = Visibility.Hidden;
                CountdownBar.Value = 0;
            }
            else
            {
                ContentPanel.Visibility = Visibility.Visible;
                HorizontalPanel.Visibility = Visibility.Collapsed;

                CountdownBar.Visibility = ms > 0 ? Visibility.Visible : Visibility.Hidden;
                CountdownBar.Value = 0; // 重置进度条
            }
        }

        /// <summary>
        /// 播放滑入动画
        /// </summary>
        /// <param name="hotkey"></param>
        /// <param name="title"></param>
        /// <param name="sub"></param>
        /// <param name="ms"></param>
        /// <param name="action"></param>
        public void RunIslandTransform(
            string hotkey,
            string title,
            string sub,
            int ms,
            Action action
        )
        {
            _isClosing = false;
            StopCurrentTimer();

            var fadeOut = new DoubleAnimation(0, TimeSpan.FromMilliseconds(80));
            fadeOut.Completed += (s, e) =>
            {
                UpdateContent(hotkey, title, sub, ms, action);
                ApplyTheme(GetSystemTheme());

                PerformIslandShapeTransition(() =>
                {
                    MainContentGrid.BeginAnimation(
                        OpacityProperty,
                        new DoubleAnimation(1, TimeSpan.FromMilliseconds(150))
                    );

                    if (ms > 0)
                        StartTimer(ms);
                    else
                        AutoClose(2000);
                });
            };
            MainContentGrid.BeginAnimation(OpacityProperty, fadeOut);
        }

        /// <summary>
        /// 播放形变动画
        /// </summary>
        /// <param name="onCompleted"></param>
        private void PerformIslandShapeTransition(Action onCompleted)
        {
            MainContentGrid.Measure(new Size(800, double.PositiveInfinity));
            double targetW = Math.Max(160, MainContentGrid.DesiredSize.Width);
            double targetH = MainContentGrid.DesiredSize.Height;

            var duration = TimeSpan.FromMilliseconds(150);
            var ease = new QuarticEase { EasingMode = EasingMode.EaseOut };

            // 停止旧动画
            IslandContainer.BeginAnimation(WidthProperty, null);
            IslandContainer.BeginAnimation(HeightProperty, null);

            // 显式同步：将当前的 Actual 值赋给 Width 属性，作为动画起点
            IslandContainer.Width = IslandContainer.ActualWidth;
            IslandContainer.Height = IslandContainer.ActualHeight;

            var widthAnim = new DoubleAnimation(targetW, duration) { EasingFunction = ease };
            var heightAnim = new DoubleAnimation(targetH, duration) { EasingFunction = ease };

            widthAnim.Completed += (s, e) => onCompleted?.Invoke();

            IslandContainer.BeginAnimation(WidthProperty, widthAnim);
            IslandContainer.BeginAnimation(HeightProperty, heightAnim);
        }

        private void ApplyTheme(PromptTheme theme)
        {
            if (theme == PromptTheme.Light)
            {
                BackBorder.Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#F2FDFDFD")
                );
                TxtMain.Foreground = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#E6000000")
                );
                TxtSub.Foreground = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#99000000")
                );
                TxtHotkey.Foreground = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#66000000")
                );
                CountdownBar.Foreground = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#0078D4")
                );
                CountdownBar.Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#10000000")
                );
                if (WindowShadow != null)
                    WindowShadow.Opacity = 0.2;
            }
            else
            {
                BackBorder.Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#F21A1A1A")
                );
                TxtMain.Foreground = Brushes.White;
                TxtSub.Foreground = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#99FFFFFF")
                );
                TxtHotkey.Foreground = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#11960D")
                );
                CountdownBar.Foreground = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#0078D4")
                );
                CountdownBar.Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#10FFFFFF")
                );
                if (WindowShadow != null)
                    WindowShadow.Opacity = 0.5;
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                // 1. 立刻掐断计时器，这是最关键的！防止后台继续读秒并触发关屏
                StopCurrentTimer();

                // 2. 清空即将执行的回调（上个双保险）
                _onFinished = null;

                // 3. 执行丝滑退场并销毁窗体
                FadeOutAndClose();

                e.Handled = true;
            }
        }

        #region 计时关闭

        private void StopCurrentTimer()
        {
            if (_activeTimer != null)
            {
                _activeTimer.Stop();
                _activeTimer = null;
            }
        }

        private void StartTimer(int ms)
        {
            StopCurrentTimer();

            _activeTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(15),
            };

            int elapsed = 0;
            _activeTimer.Tick += (s, e) =>
            {
                elapsed += 15;
                CountdownBar.Value = (double)elapsed / ms * 100;

                if (elapsed >= ms)
                {
                    StopCurrentTimer();
                    _onFinished?.Invoke();
                    FadeOutAndClose();
                }
            };

            _activeTimer.Start();
        }


        private void AutoClose(int ms)
        {
            StopCurrentTimer(); 

            _activeTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(ms),
            };

            _activeTimer.Tick += (s, e) =>
            {
                StopCurrentTimer(); 
                FadeOutAndClose();
            };

            _activeTimer.Start();
        }

        #endregion



        /// <summary>
        /// 播放关闭动画
        /// </summary>
        private void FadeOutAndClose()
        {
            if (_isClosing)
                return;
            _isClosing = true;

            var fadeOutAnim = new DoubleAnimation(0, TimeSpan.FromMilliseconds(200));
            var slideDownAnim = new DoubleAnimation(150, TimeSpan.FromMilliseconds(250))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn },
            };

            slideDownAnim.Completed += (s, e) =>
            {
                singleton = null; // 清理唯一实例
                this.Close();
            };

            // 执行退出动画
            IslandContainer.BeginAnimation(OpacityProperty, fadeOutAnim);

            if (IslandContainer.RenderTransform is TranslateTransform tt)
            {
                tt.BeginAnimation(TranslateTransform.YProperty, slideDownAnim);
            }
            else
            {
                singleton = null;
                this.Close();
            }
        }


        #region 使用API获取屏幕尺寸

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left,
                Top,
                Right,
                Bottom;
        }

        [DllImport("user32.dll")]
        public static extern bool SystemParametersInfo(
            int nAction,
            int nParam,
            ref RECT rc,
            int nUpdate
        );


        #endregion
    }
}
