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
        private Action _onFinished;

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
            var win = new PromptWindow(hotkey, title, null, 0, null);
            win.Show();
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
            var win = new PromptWindow(hotkey, title, sub, ms, action);
            win.Show();
        }

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
                FadeOutAndClose();
                e.Handled = true;
            }
        }

        private void StartTimer(int ms)
        {
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(15),
            };
            int elapsed = 0;
            timer.Tick += (s, e) =>
            {
                elapsed += 15;
                CountdownBar.Value = (double)elapsed / ms * 100;
                if (elapsed >= ms)
                {
                    timer.Stop();
                    _onFinished?.Invoke();
                    FadeOutAndClose();
                }
            };
            timer.Start();
        }

        private async void AutoClose(int ms)
        {
            await Task.Delay(ms);
            FadeOutAndClose();
        }

        private void FadeOutAndClose()
        {
            double winHeight = this.ActualHeight > 0 ? this.ActualHeight : 80;

            // 1. 内部元素向下滑出的动画
            var slideOutAnim = new DoubleAnimation
            {
                To = winHeight, // 向下滑动窗口自身高度的距离，彻底离开可视区
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn },
            };

            // 动画结束后关闭真实窗口
            slideOutAnim.Completed += (s, e) => this.Close();

            // 2. 获取刚才绑定的 Transform 并执行动画
            if (RootGrid.RenderTransform is TranslateTransform transform)
            {
                transform.BeginAnimation(TranslateTransform.YProperty, slideOutAnim);
            }
            else
            {
                this.Close(); // 容错处理
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
