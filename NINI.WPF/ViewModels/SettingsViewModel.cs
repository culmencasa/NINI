using MVVMLib;
using NINI.Helper;
using NINI.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace NINI.ViewModels
{
    /// <summary>
    /// 热键项视图模型
    /// </summary>
    public class HotkeyItemViewModel : ObservableObject
    {
        private readonly HotkeyService _hotkeyService;
        private HotkeyItem _model;

        public HotkeyItemViewModel(HotkeyItem model, HotkeyService hotkeyService)
        {
            _model = model;
            _hotkeyService = hotkeyService;

            // 监听热键冲突状态变化
            _model.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(HotkeyItem.IsConflicted))
                {
                    OnPropertyChanged(nameof(StatusIcon));
                    OnPropertyChanged(nameof(StatusText));
                    OnPropertyChanged(nameof(IsConflicted));
                }
                else if (e.PropertyName == nameof(HotkeyItem.IsEnabled))
                {
                    OnPropertyChanged(nameof(IsEnabled));
                }
            };
        }

        public string Id => _model.Id;
        public string Name => _model.Name;
        public string Description => _model.Description;

        public string HotkeyText => _model.GetHotkeyText();

        public bool IsEnabled
        {
            get => _model.IsEnabled;
            set
            {
                if (_model.IsEnabled != value)
                {
                    _hotkeyService.SetEnabled(_model.Id, value);
                    OnPropertyChanged();
                }
            }
        }

        public bool IsConflicted => _model.IsConflicted;

        public string StatusIcon => _model.IsConflicted ? "⚠️" : (_model.IsEnabled ? "✅" : "❌");

        public string StatusText => _model.IsConflicted ? "冲突" : (_model.IsEnabled ? "正常" : "已禁用");

        public HotkeyItem Model => _model;

        public ICommand ResetCommand => new RelayCommand(() =>
        {
            var defaultHotkey = new HotkeyItem
            {
                Id = _model.Id,
                Name = _model.Name,
                Description = _model.Description,
                IsEnabled = true
            };

            // 根据ID设置默认值
            switch (_model.Id)
            {
                case "open_terminal":
                    defaultHotkey.Modifier = (int)(NINI.Helper.ModifierKeys.Control | NINI.Helper.ModifierKeys.Alt);
                    defaultHotkey.KeyCode = (int)System.Windows.Forms.Keys.T;
                    break;
                case "screen_saver":
                    defaultHotkey.Modifier = (int)(NINI.Helper.ModifierKeys.Control | NINI.Helper.ModifierKeys.Alt);
                    defaultHotkey.KeyCode = (int)System.Windows.Forms.Keys.R;
                    break;
                case "show_ip":
                    defaultHotkey.Modifier = (int)(NINI.Helper.ModifierKeys.Control | NINI.Helper.ModifierKeys.Alt);
                    defaultHotkey.KeyCode = (int)System.Windows.Forms.Keys.P;
                    break;
                case "turn_off_monitor":
                    defaultHotkey.Modifier = (int)NINI.Helper.ModifierKeys.Control;
                    defaultHotkey.KeyCode = (int)System.Windows.Forms.Keys.F12;
                    break;
            }

            _hotkeyService.UpdateHotkey(
                _model.Id,
                (NINI.Helper.ModifierKeys)defaultHotkey.Modifier,
                (System.Windows.Forms.Keys)defaultHotkey.KeyCode
            );

            OnPropertyChanged(nameof(HotkeyText));
        });
    }

    /// <summary>
    /// 设置窗口 ViewModel
    /// </summary>
    public class SettingsViewModel : ObservableObject
    {
        private readonly HotkeyService _hotkeyService;
        private ObservableCollection<HotkeyItemViewModel> _hotkeyItems;
        private HotkeyItemViewModel? _selectedHotkey;

        public SettingsViewModel(HotkeyService hotkeyService)
        {
            _hotkeyService = hotkeyService;
            _hotkeyItems = new ObservableCollection<HotkeyItemViewModel>();

            // 加载热键列表
            RefreshHotkeyList();

            // 监听设置变更
            _hotkeyService.SettingsChanged += (s, e) => RefreshHotkeyList();
        }

        public ObservableCollection<HotkeyItemViewModel> HotkeyItems => _hotkeyItems;

        public HotkeyItemViewModel? SelectedHotkey
        {
            get => _selectedHotkey;
            set
            {
                if (_selectedHotkey != value)
                {
                    _selectedHotkey = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasSelection));
                }
            }
        }

        public bool HasSelection => _selectedHotkey != null;

        public int ConflictedCount => _hotkeyItems.Count(x => x.IsConflicted);

        private void RefreshHotkeyList()
        {
            _hotkeyItems.Clear();
            foreach (var hotkey in _hotkeyService.Hotkeys)
            {
                _hotkeyItems.Add(new HotkeyItemViewModel(hotkey, _hotkeyService));
            }
            OnPropertyChanged(nameof(ConflictedCount));
        }

        public ICommand ResetAllCommand => new RelayCommand(() =>
        {
            _hotkeyService.ResetToDefault();
            RefreshHotkeyList();
        });

        public ICommand RefreshCommand => new RelayCommand(() =>
        {
            RefreshHotkeyList();
        });
    }
}
