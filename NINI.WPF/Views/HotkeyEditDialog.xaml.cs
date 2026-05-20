using NINI.Helper;
using NINI.Models;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace NINI.Views
{
    public partial class HotkeyEditDialog : Wpf.Ui.Controls.FluentWindow
    {
        private readonly HotkeyItem _hotkeyItem;
        private readonly HotkeyService _hotkeyService;
        private Key _capturedKey = Key.None;
        private System.Windows.Input.ModifierKeys _capturedModifiers = System.Windows.Input.ModifierKeys.None;
        private bool _isCapturing;

        public (NINI.Helper.ModifierKeys modifiers, System.Windows.Forms.Keys key)? NewHotkey { get; private set; }

        public HotkeyEditDialog(HotkeyItem hotkeyItem)
        {
            InitializeComponent();
            _hotkeyItem = hotkeyItem;
            _hotkeyService = App.GetHotkeyService()!;

            HotkeyName.Text = hotkeyItem.Name;
            CurrentHotkeyText.Text = hotkeyItem.GetHotkeyText();
        }

        private void HotkeyInput_GotFocus(object sender, RoutedEventArgs e)
        {
            _isCapturing = true;
            HotkeyInputBorder.BorderBrush = (Brush)FindResource("AccentFillColorDefaultBrush");
            NewHotkeyText.Text = "请按下快捷键...";
            NewHotkeyText.Foreground = (Brush)FindResource("TextFillColorSecondaryBrush");
        }

        private void HotkeyInput_LostFocus(object sender, RoutedEventArgs e)
        {
            _isCapturing = false;
            HotkeyInputBorder.BorderBrush = (Brush)FindResource("ControlStrokeColorDefaultBrush");
            UpdateHotkeyText();
        }

        private void HotkeyInput_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!_isCapturing) return;

            e.Handled = true;

            var key = e.Key == Key.System ? e.SystemKey : e.Key;
            var modifiers = Keyboard.Modifiers;

            if (key == Key.LeftCtrl || key == Key.RightCtrl ||
                key == Key.LeftAlt || key == Key.RightAlt ||
                key == Key.LeftShift || key == Key.RightShift ||
                key == Key.LWin || key == Key.RWin)
            {
                return;
            }

            if (modifiers == System.Windows.Input.ModifierKeys.None)
            {
                NewHotkeyText.Text = "需要至少一个修饰键 (Ctrl/Alt/Shift)";
                NewHotkeyText.Foreground = Brushes.Orange;
                return;
            }

            _capturedKey = key;
            _capturedModifiers = modifiers;

            UpdateHotkeyText();
            CheckConflict();
        }

        private void UpdateHotkeyText()
        {
            if (_capturedKey == Key.None) return;

            var parts = new System.Collections.Generic.List<string>();

            if (_capturedModifiers.HasFlag(System.Windows.Input.ModifierKeys.Control))
                parts.Add("Ctrl");
            if (_capturedModifiers.HasFlag(System.Windows.Input.ModifierKeys.Alt))
                parts.Add("Alt");
            if (_capturedModifiers.HasFlag(System.Windows.Input.ModifierKeys.Shift))
                parts.Add("Shift");
            if (_capturedModifiers.HasFlag(System.Windows.Input.ModifierKeys.Windows))
                parts.Add("Win");

            parts.Add(_capturedKey.ToString());

            NewHotkeyText.Text = string.Join(" + ", parts);
            NewHotkeyText.Foreground = (Brush)FindResource("AccentTextFillColorPrimaryBrush");
        }

        private void CheckConflict()
        {
            if (_capturedKey == Key.None) return;

            var nativeModifiers = (NINI.Helper.ModifierKeys)_capturedModifiers;
            var keyCode = (System.Windows.Forms.Keys)KeyInterop.VirtualKeyFromKey(_capturedKey);

            var isConflicted = _hotkeyService.CheckHotkeyConflict(_hotkeyItem.Id, nativeModifiers, keyCode);

            ConflictWarning.Visibility = isConflicted ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            _capturedKey = Key.None;
            _capturedModifiers = System.Windows.Input.ModifierKeys.None;
            NewHotkeyText.Text = "点击此处并按下快捷键...";
            NewHotkeyText.Foreground = (Brush)FindResource("TextFillColorSecondaryBrush");
            ConflictWarning.Visibility = Visibility.Collapsed;
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (_capturedKey != Key.None)
            {
                var nativeModifiers = (NINI.Helper.ModifierKeys)_capturedModifiers;
                var keyCode = (System.Windows.Forms.Keys)KeyInterop.VirtualKeyFromKey(_capturedKey);
                NewHotkey = (nativeModifiers, keyCode);
                DialogResult = true;
            }
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
