using NINI.ViewModels;
using NINI.Helper;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace NINI.Views
{
    public partial class SettingsWindow : Window
    {
        private readonly SettingsViewModel _viewModel;

        public SettingsWindow(SettingsViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;
        }

        private void HotkeyItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is HotkeyItemViewModel item)
            {
                _viewModel.SelectedHotkey = item;

                // 打开热键录制对话框
                var dialog = new HotkeyCaptureDialog(item.Id, item.Name)
                {
                    Owner = this
                };

                if (dialog.ShowDialog() == true)
                {
                    // 更新热键（由 HotkeyCaptureDialog 在内部处理）
                    // 刷新显示
                    _viewModel.RefreshCommand.Execute(null);
                }
            }
        }

        private void ResetAll_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "确定要重置所有快捷键为默认值吗？",
                "确认重置",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                _viewModel.ResetAllCommand.Execute(null);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    /// <summary>
    /// 热键录制对话框
    /// </summary>
    public class HotkeyCaptureDialog : Window
    {
        private readonly string _hotkeyId;
        private System.Windows.Forms.Keys _key;
        private NINI.Helper.ModifierKeys _modifier;

        public System.Windows.Forms.Keys CapturedKey => _key;
        public NINI.Helper.ModifierKeys CapturedModifier => _modifier;
        public bool UpdateSuccess { get; private set; }

        public HotkeyCaptureDialog(string hotkeyId, string hotkeyName)
        {
            _hotkeyId = hotkeyId;
            Title = $"设置快捷键 - {hotkeyName}";
            Width = 400;
            Height = 200;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F21A1A1A"));

            var grid = new Grid { Margin = new Thickness(20) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // 标题
            var title = new TextBlock
            {
                Text = "请按下新的快捷键",
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Foreground = System.Windows.Media.Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Grid.SetRow(title, 0);
            grid.Children.Add(title);

            // 热键显示区
            var hotkeyPanel = new Border
            {
                Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#2A2A2A")),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(20),
                Margin = new Thickness(0, 20, 0, 20),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center
            };

            var hotkeyText = new TextBlock
            {
                Text = "请按组合键...",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#4DA6FF")),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            hotkeyPanel.Child = hotkeyText;
            Grid.SetRow(hotkeyPanel, 1);
            grid.Children.Add(hotkeyPanel);

            // 按钮
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var cancelButton = new Button { Content = "取消", Padding = new Thickness(16, 8, 16, 8), Margin = new Thickness(0, 0, 12, 0) };
            cancelButton.Click += (s, e) => { DialogResult = false; Close(); };

            var okButton = new Button { Content = "确定", Padding = new Thickness(16, 8, 16, 8), IsEnabled = false };
            okButton.Click += (s, e) =>
            {
                var service = App.GetHotkeyService();
                if (service != null)
                {
                    UpdateSuccess = service.UpdateHotkey(_hotkeyId, _modifier, _key);
                    if (UpdateSuccess)
                    {
                        DialogResult = true;
                    }
                    else
                    {
                        MessageBox.Show("快捷键冲突或注册失败！", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                Close();
            };

            buttonPanel.Children.Add(cancelButton);
            buttonPanel.Children.Add(okButton);
            Grid.SetRow(buttonPanel, 2);
            grid.Children.Add(buttonPanel);

            Content = grid;

            // 键盘事件
            PreviewKeyDown += (s, e) =>
            {
                e.Handled = true;

                // ESC 取消
                if (e.Key == Key.Escape)
                {
                    DialogResult = false;
                    Close();
                    return;
                }

                // 获取修饰键
                _modifier = 0;
                if (Keyboard.Modifiers.HasFlag(System.Windows.Input.ModifierKeys.Control))
                    _modifier |= NINI.Helper.ModifierKeys.Control;
                if (Keyboard.Modifiers.HasFlag(System.Windows.Input.ModifierKeys.Alt))
                    _modifier |= NINI.Helper.ModifierKeys.Alt;
                if (Keyboard.Modifiers.HasFlag(System.Windows.Input.ModifierKeys.Shift))
                    _modifier |= NINI.Helper.ModifierKeys.Shift;
                if (Keyboard.Modifiers.HasFlag(System.Windows.Input.ModifierKeys.Windows))
                    _modifier |= NINI.Helper.ModifierKeys.Win;

                // 获取主键
                var key = KeyInterop.VirtualKeyFromKey(e.Key);

                // 忽略纯修饰键
                if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl ||
                    e.Key == Key.LeftAlt || e.Key == Key.RightAlt ||
                    e.Key == Key.LeftShift || e.Key == Key.RightShift ||
                    e.Key == Key.LWin || e.Key == Key.RWin)
                {
                    return;
                }

                _key = (System.Windows.Forms.Keys)key;

                // 构建显示文本
                var parts = new System.Collections.Generic.List<string>();
                if (_modifier.HasFlag(NINI.Helper.ModifierKeys.Control)) parts.Add("Ctrl");
                if (_modifier.HasFlag(NINI.Helper.ModifierKeys.Alt)) parts.Add("Alt");
                if (_modifier.HasFlag(NINI.Helper.ModifierKeys.Shift)) parts.Add("Shift");
                if (_modifier.HasFlag(NINI.Helper.ModifierKeys.Win)) parts.Add("Win");
                parts.Add(_key.ToString());

                hotkeyText.Text = string.Join("+", parts);
                okButton.IsEnabled = true;
            };

            // 聚焦窗口以接收键盘事件
            Loaded += (s, e) => Focus();
        }
    }

    /// <summary>
    /// 大于0转Visibility转换器
    /// </summary>
    public class GreaterThanZeroToVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                return count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
