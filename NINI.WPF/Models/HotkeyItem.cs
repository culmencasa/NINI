using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Windows.Forms;

namespace NINI.Models
{
    /// <summary>
    /// 热键配置项
    /// </summary>
    public class HotkeyItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string _id = string.Empty;
        private string _name = string.Empty;
        private string _description = string.Empty;
        private bool _isEnabled = true;
        private int _modifier;
        private int _keyCode;
        private bool _isConflicted;

        /// <summary>
        /// 热键唯一标识
        /// </summary>
        public string Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(nameof(Id)); }
        }

        /// <summary>
        /// 热键名称（显示用）
        /// </summary>
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }

        /// <summary>
        /// 功能描述
        /// </summary>
        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(nameof(Description)); }
        }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set { _isEnabled = value; OnPropertyChanged(nameof(IsEnabled)); }
        }

        /// <summary>
        /// 修饰键（Ctrl=2, Alt=1, Shift=4, Win=8）
        /// </summary>
        public int Modifier
        {
            get => _modifier;
            set { _modifier = value; OnPropertyChanged(nameof(Modifier)); }
        }

        /// <summary>
        /// 主键代码
        /// </summary>
        public int KeyCode
        {
            get => _keyCode;
            set { _keyCode = value; OnPropertyChanged(nameof(KeyCode)); }
        }

        /// <summary>
        /// 是否冲突
        /// </summary>
        [JsonIgnore]
        public bool IsConflicted
        {
            get => _isConflicted;
            set { _isConflicted = value; OnPropertyChanged(nameof(IsConflicted)); }
        }

        /// <summary>
        /// 获取修饰键文本
        /// </summary>
        public string GetModifierText()
        {
            var parts = new System.Collections.Generic.List<string>();
            if ((Modifier & 2) != 0) parts.Add("Ctrl");
            if ((Modifier & 1) != 0) parts.Add("Alt");
            if ((Modifier & 4) != 0) parts.Add("Shift");
            if ((Modifier & 8) != 0) parts.Add("Win");
            return string.Join("+", parts);
        }

        /// <summary>
        /// 获取热键文本
        /// </summary>
        public string GetHotkeyText()
        {
            var modifier = GetModifierText();
            var key = (Keys)KeyCode;
            return string.IsNullOrEmpty(modifier) ? key.ToString() : $"{modifier}+{key}";
        }

        /// <summary>
        /// 从 ModifierKeys 和 Keys 创建
        /// </summary>
        public static HotkeyItem FromKeys(Helper.ModifierKeys modifier, Keys key)
        {
            return new HotkeyItem
            {
                Modifier = (int)modifier,
                KeyCode = (int)key
            };
        }
    }
}
