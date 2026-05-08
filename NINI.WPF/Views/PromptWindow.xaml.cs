using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Win32;

namespace NINI.Views
{
    public partial class PromptWindow : Window
    {
        private static PromptWindow _instance;

        private Action _onFinished;
        private bool _isClosing = false; // 防止关闭逻辑被重复触发或冲突

        // 新增：用于统一管理倒计时的变量
        private System.Windows.Threading.DispatcherTimer _activeTimer;


        private System.Windows.Threading.DispatcherTimer _autoCloseTimer;

        public enum PromptTheme
        {
            Dark,
            Light,
        }

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
        public static void ShowAction(string hotkey, string title, string sub, int ms, Action action)
        {
            // 如果实例存在且没有正在关闭，则复用
            if (_instance != null && _instance.IsLoaded && !_instance._isClosing)
            {
                _instance.Activate(); // 确保窗口在最前
                _instance.RunIslandTransform(hotkey, title, sub, ms, action);
            }
            else
            {
                // 如果窗口不存在或正在关闭，创建新窗口
                _instance = new PromptWindow(hotkey, title, sub, ms, action);
                _instance.Show();
            }
        }
        public PromptWindow(string hotkey, string main, string sub = null, int ms = 0, Action onDone = null)
        {
            InitializeComponent();
            _instance = this;

            ApplyTheme(GetSystemTheme());
            UpdateContent(hotkey, main, sub, ms, onDone);

            this.Loaded += (s, e) =>
            {
                UpdateLayoutAndPosition(false); // 初始定位

                // 初始滑入动画
                var tt = new TranslateTransform(0, 80);
                RootGrid.RenderTransform = tt;
                tt.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(400))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                });
            };
        }
        private void UpdateContent(string hotkey, string title, string sub, int ms, Action action)
        {
            _onFinished = action;
            TxtHotkey.Text = string.IsNullOrEmpty(hotkey) ? " " : hotkey.ToUpper();
            TxtMain.Text = title;
            TxtSub.Text = string.IsNullOrEmpty(sub) ? " " : sub;
            TxtSub.Visibility = string.IsNullOrEmpty(sub) ? Visibility.Hidden : Visibility.Visible;
            CountdownBar.Visibility = ms > 0 ? Visibility.Visible : Visibility.Hidden;
            CountdownBar.Value = 0; // 重置进度条
        }

        public void RunIslandTransform(string hotkey, string title, string sub, int ms, Action action)
        {
            _isClosing = false;
            StopCurrentTimer(); // 立即掐断旧的自动关闭倒计时

            // 阶段 A：旧文字淡出
            var fadeOut = new DoubleAnimation(0, TimeSpan.FromMilliseconds(150));
            fadeOut.Completed += (s, e) =>
            {
                // 阶段 B：文字消失后，更新数据，开始形状形变
                UpdateContent(hotkey, title, sub, ms, action);
                ApplyTheme(GetSystemTheme());

                PerformIslandShapeTransition(() =>
                {
                    // 阶段 C：形状变好后，新文字淡入
                    ContentPanel.BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(250)));

                    // 阶段 D：全部就绪，重新开始计时
                    if (ms > 0) StartTimer(ms);
                    else AutoClose(2000);
                });
            };
            ContentPanel.BeginAnimation(OpacityProperty, fadeOut);
        }

        private void PerformIslandShapeTransition(Action onCompleted)
        {
            // 1. 测量内容实际需要的尺寸
            // 注意：MaxWidth 限制了窗口不会无限水平扩张
            ContentPanel.Measure(new Size(this.MaxWidth, double.PositiveInfinity));

            // 2. 计算目标物理尺寸
            // 内容宽度 + ContentPanel左右Margin(32*2) + RootGrid左右Margin给阴影留的空间(20*2)
            double targetW = Math.Max(this.MinWidth, ContentPanel.DesiredSize.Width + 64 + 40);
            // 内容高度 + ContentPanel上下Margin(16+18) + RootGrid上下Margin(20*2)
            double targetH = ContentPanel.DesiredSize.Height + 34 + 40;

            // 3. 获取当前显示器的 DPI 缩放比例
            var source = PresentationSource.FromVisual(this);
            double dpiX = source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
            double dpiY = source?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;

            // 4. 调用 Win32 API 获取工作区（避开任务栏）
            RECT rc = new RECT();
            SystemParametersInfo(48, 0, ref rc, 0); // 48 = SPI_GETWORKAREA

            // 5. 将物理像素坐标转换为 WPF 逻辑坐标
            double logicalScreenWidth = (rc.Right - rc.Left) / dpiX;
            double logicalWorkAreaBottom = rc.Bottom / dpiY;

            // 6. 计算为了保持“底边中心对齐”的目标坐标
            double targetLeft = (logicalScreenWidth - targetW) / 2;
            // 减去窗口高度，再减去你想要的底部间距（如 10 像素）
            double targetTop = logicalWorkAreaBottom - targetH - 10;

            // 7. 设置动画参数
            var duration = TimeSpan.FromMilliseconds(400);
            var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

            // 8. 属性锁清理：在对 Width/Height 这种依赖属性重新执行动画前，必须卸载之前的动画层
            this.BeginAnimation(Window.WidthProperty, null);
            this.BeginAnimation(Window.HeightProperty, null);
            this.BeginAnimation(Window.LeftProperty, null);
            this.BeginAnimation(Window.TopProperty, null);

            // 9. 构建动画对象
            var widthAnim = new DoubleAnimation(targetW, duration) { EasingFunction = ease };
            var heightAnim = new DoubleAnimation(targetH, duration) { EasingFunction = ease };
            var leftAnim = new DoubleAnimation(targetLeft, duration) { EasingFunction = ease };
            var topAnim = new DoubleAnimation(targetTop, duration) { EasingFunction = ease };

            // 10. 监听动画完成，回调执行“内容淡入”
            widthAnim.Completed += (s, e) => onCompleted?.Invoke();

            // 11. 四轴联动，启动动画
            this.BeginAnimation(Window.WidthProperty, widthAnim);
            this.BeginAnimation(Window.HeightProperty, heightAnim);
            this.BeginAnimation(Window.LeftProperty, leftAnim);
            this.BeginAnimation(Window.TopProperty, topAnim);
        }
        private void UpdateLayoutAndPosition(bool animate)
        {
            // 1. 精确测量文字需要的尺寸
            ContentPanel.Measure(new Size(this.MaxWidth, double.PositiveInfinity));

            // 2. 计算目标宽度：文字宽度 + ContentPanel的左右Margin(32+32) + RootGrid留给阴影的Margin(20+20)
            double targetW = Math.Max(this.MinWidth, ContentPanel.DesiredSize.Width + 64 + 40);

            // 3. 计算目标高度：文字高度 + ContentPanel的上下Margin(16+18) + RootGrid留给阴影的Margin(20+20)
            double targetH = ContentPanel.DesiredSize.Height + 34 + 40;

            var source = PresentationSource.FromVisual(this);
            double dpiX = source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
            double dpiY = source?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;
            RECT rc = new RECT();
            SystemParametersInfo(48, 0, ref rc, 0);

            double screenW = (rc.Right - rc.Left) / dpiX;
            double workBottom = rc.Bottom / dpiY;

            // 位置计算保持不变
            double targetLeft = (screenW - targetW) / 2;
            double targetTop = workBottom - targetH - 10;

            if (animate)
            {
                var duration = TimeSpan.FromMilliseconds(400);
                var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

                this.BeginAnimation(Window.WidthProperty, null);
                this.BeginAnimation(Window.HeightProperty, null);
                this.BeginAnimation(Window.LeftProperty, null);
                this.BeginAnimation(Window.TopProperty, null);

                this.BeginAnimation(Window.WidthProperty, new DoubleAnimation(targetW, duration) { EasingFunction = ease });
                this.BeginAnimation(Window.HeightProperty, new DoubleAnimation(targetH, duration) { EasingFunction = ease });
                this.BeginAnimation(Window.LeftProperty, new DoubleAnimation(targetLeft, duration) { EasingFunction = ease });
                this.BeginAnimation(Window.TopProperty, new DoubleAnimation(targetTop, duration) { EasingFunction = ease });
            }
            else
            {
                this.Width = targetW;
                this.Height = targetH;
                this.Left = targetLeft;
                this.Top = targetTop;
            }
        }


        private void UpdateState(string hotkey, string main, string sub, int ms, Action onDone)
        {
            _onFinished = onDone;
            TxtHotkey.Text = string.IsNullOrEmpty(hotkey) ? " " : hotkey.ToUpper();
            TxtMain.Text = main;
            TxtSub.Text = string.IsNullOrEmpty(sub) ? " " : sub;
            TxtSub.Visibility = string.IsNullOrEmpty(sub) ? Visibility.Hidden : Visibility.Visible;
            CountdownBar.Visibility = ms > 0 ? Visibility.Visible : Visibility.Hidden;
        }


        // 提取公共的内容设置方法
        private void SetContent(string hotkey, string title, string sub, int ms, Action action)
        {
            _onFinished = action;
            TxtHotkey.Text = string.IsNullOrEmpty(hotkey) ? " " : hotkey.ToUpper();
            TxtMain.Text = title;
            TxtSub.Text = string.IsNullOrEmpty(sub) ? " " : sub;
            TxtSub.Visibility = string.IsNullOrEmpty(sub) ? Visibility.Hidden : Visibility.Visible;
            CountdownBar.Visibility = ms > 0 ? Visibility.Visible : Visibility.Hidden;
        }


        private void ApplyIslandTransform()
        {
            // 记录旧尺寸
            double oldWidth = this.ActualWidth;
            double oldHeight = this.ActualHeight;

            // 强制测量新尺寸
            this.Measure(new Size(MaxWidth, double.PositiveInfinity));
            double newWidth = Math.Max(MinWidth, Math.Min(MaxWidth, this.DesiredSize.Width));
            double newHeight = this.DesiredSize.Height;

            // 尺寸形变动画 (灵动岛效果核心)
            var widthAnim = new DoubleAnimation(oldWidth, newWidth, TimeSpan.FromMilliseconds(400))
            { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
            var heightAnim = new DoubleAnimation(oldHeight, newHeight, TimeSpan.FromMilliseconds(400))
            { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };

            this.BeginAnimation(Window.WidthProperty, widthAnim);
            this.BeginAnimation(Window.HeightProperty, heightAnim);

            // 位置修正动画：确保形变时底部中心对齐
            UpdatePosition(true, newWidth, newHeight);
        }

        private void UpdatePosition(bool animate, double targetW = 0, double targetH = 0)
        {
            var source = PresentationSource.FromVisual(this);
            double dpiX = source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
            double dpiY = source?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;

            RECT rc = new RECT();
            SystemParametersInfo(48, 0, ref rc, 0);

            double logicalScreenWidth = (rc.Right - rc.Left) / dpiX;
            double logicalWorkAreaBottom = rc.Bottom / dpiY;

            double winWidth = targetW > 0 ? targetW : this.ActualWidth;
            double winHeight = targetH > 0 ? targetH : this.ActualHeight;

            double finalLeft = (logicalScreenWidth - winWidth) / 2;
            double finalTop = logicalWorkAreaBottom - winHeight - 10;

            if (animate)
            {
                this.BeginAnimation(Window.LeftProperty, new DoubleAnimation(finalLeft, TimeSpan.FromMilliseconds(400)) { EasingFunction = new CubicEase() });
                this.BeginAnimation(Window.TopProperty, new DoubleAnimation(finalTop, TimeSpan.FromMilliseconds(400)) { EasingFunction = new CubicEase() });
            }
            else
            {
                this.Left = finalLeft;
                this.Top = finalTop;
            }
        }

        private void ResetTimer(int ms)
        {
            _autoCloseTimer?.Stop();
            if (ms > 0) StartTimer(ms);
            else AutoClose(2000);
        }
        

        /*
        public PromptWindow(
            string hotkey,
            string main,
            string sub = null,
            int ms = 0,
            Action onDone = null
        )
        {
            InitializeComponent();

            _onFinished = onDone;

            // 1. 设置内容
            TxtHotkey.Text = string.IsNullOrEmpty(hotkey) ? " " : hotkey.ToUpper();
            TxtMain.Text = main;

            // 2. 占位逻辑：使用 Hidden 保留空间，文本设为空格防止高度塌陷 
            if (!string.IsNullOrEmpty(sub))
            {
                TxtSub.Text = sub;
                TxtSub.Visibility = Visibility.Visible;
            }
            else
            {
                TxtSub.Text = " ";
                TxtSub.Visibility = Visibility.Hidden; // 绝对不能用 Collapsed
            }

            // 进度条
            CountdownBar.Visibility = ms > 0 ? Visibility.Visible : Visibility.Hidden;

            // 3. 应用主题
            var theme = GetSystemTheme();
            ApplyTheme(theme);

            this.Loaded += (s, e) =>
            {
                // 强制刷新布局，获取真实尺寸
                this.UpdateLayout();

                double winHeight = this.ActualHeight > 0 ? this.ActualHeight : 80;
                double winWidth = this.ActualWidth > 0 ? this.ActualWidth : 300;

                // 1. 获取 WPF 针对当前屏幕的 DPI 缩放矩阵
                var source = PresentationSource.FromVisual(this);
                double dpiX = source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
                double dpiY = source?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;

                // 2. 调用最底层的 Win32 API 获取真实的工作区边界（SPI_GETWORKAREA = 48）
                RECT rc = new RECT();
                SystemParametersInfo(48, 0, ref rc, 0);

                // 3. 将物理坐标 / DPI，转换为 WPF 永远不会算错的逻辑坐标 
                double logicalScreenWidth = (rc.Right - rc.Left) / dpiX;
                double logicalWorkAreaBottom = rc.Bottom / dpiY;

                // 4. 完美定位：直接将物理窗口固定在【最终位置】
                this.Left = (logicalScreenWidth - winWidth) / 2;
                double finalTop = logicalWorkAreaBottom - winHeight - 10;

                this.Top = finalTop; // <--- 关键修正：必须设定为 finalTop
                this.Visibility = Visibility.Visible;

                // 5. 利用 RenderTransform 移动内部的 RootGrid 元素
                // 此时物理窗体已经在屏幕内了，我们把内部的 Grid 向下推移 winHeight 的距离
                // 因为超出了物理窗体的边界，WPF 会自动把 Grid 裁剪掉（看不见）
                var transform = new TranslateTransform { Y = winHeight };
                RootGrid.RenderTransform = transform;

                // 6. 执行内部滑入动画：让 Grid 从底部慢慢“升”回 0 的位置
                var slideInAnim = new DoubleAnimation
                {
                    From = winHeight,
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(350),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
                };

                transform.BeginAnimation(TranslateTransform.YProperty, slideInAnim);

                if (ms > 0)
                    StartTimer(ms);
                else
                    AutoClose(2000);
            };


        }*/



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
                FadeOutAndClose();
                e.Handled = true;
            }
        }


        #region 计时关闭

        // 核心：每次重新计时前，必须先停掉旧的计时器
        private void StopCurrentTimer()
        {
            if (_activeTimer != null)
            {
                _activeTimer.Stop();
                _activeTimer = null;
            }
        }

        // 替换原来的 StartTimer
        private void StartTimer(int ms)
        {
            StopCurrentTimer(); // 掐断旧计时

            _activeTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(15)
            };

            int elapsed = 0;
            _activeTimer.Tick += (s, e) =>
            {
                elapsed += 15;
                CountdownBar.Value = (double)elapsed / ms * 100;

                if (elapsed >= ms)
                {
                    StopCurrentTimer(); // 计时结束，清理自身
                    _onFinished?.Invoke();
                    FadeOutAndClose();
                }
            };

            _activeTimer.Start();
        }

        // 替换原来的 AutoClose
        private void AutoClose(int ms)
        {
            StopCurrentTimer(); // 掐断旧计时

            _activeTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(ms)
            };

            _activeTimer.Tick += (s, e) =>
            {
                StopCurrentTimer(); // 计时结束，清理自身
                FadeOutAndClose();
            };

            _activeTimer.Start();
        }


        #endregion


        private void FadeOutAndClose()
        {
            if (_isClosing) return;
            _isClosing = true;

            var slideOutAnim = new DoubleAnimation
            {
                To = this.ActualHeight,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn },
            };

            slideOutAnim.Completed += (s, e) => {
                _instance = null; // 彻底清除引用
                this.Close();
            };

            if (RootGrid.RenderTransform is TranslateTransform tt)
                tt.BeginAnimation(TranslateTransform.YProperty, slideOutAnim);
            else
                this.Close();
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
                        // AppsUseLightTheme = 1 表示明亮模式，0 表示暗黑模式
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
                // 出现异常（如权限问题）时跳过
            }

            // 如果系统不支持（如旧版 Windows 7/8）或读取失败，默认返回明亮模式
            return PromptTheme.Light;
        }


        [StructLayout(LayoutKind.Sequential)]
        public struct RECT { public int Left, Top, Right, Bottom; }

        [DllImport("user32.dll")]
        public static extern bool SystemParametersInfo(int nAction, int nParam, ref RECT rc, int nUpdate);


    }
}
