using MVVMLib;
using System.Windows.Controls;
using System.Windows.Input;

namespace NINI.Views.Settings.Pages
{
    public partial class GeneralPage : Page
    {
        public GeneralPage()
        {
            InitializeComponent();
            DataContext = new GeneralViewModel();
        }
    }

    public class GeneralViewModel : ObservableObject
    {
        private readonly NINI.Helper.HotkeyService? _hotkeyService;

        public GeneralViewModel()
        {
            _hotkeyService = App.GetHotkeyService();
        }

        public bool AutoStart
        {
            get => _hotkeyService?.AutoStart ?? false;
            set
            {
                if (_hotkeyService != null)
                {
                    _hotkeyService.AutoStart = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool StartMinimized
        {
            get => _hotkeyService?.StartMinimized ?? false;
            set
            {
                if (_hotkeyService != null)
                {
                    _hotkeyService.StartMinimized = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool RememberWindowPosition
        {
            get => _hotkeyService?.RememberWindowPosition ?? true;
            set
            {
                if (_hotkeyService != null)
                {
                    _hotkeyService.RememberWindowPosition = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool MinimizeToTray
        {
            get => _hotkeyService?.MinimizeToTray ?? true;
            set
            {
                if (_hotkeyService != null)
                {
                    _hotkeyService.MinimizeToTray = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool ShowNotifications
        {
            get => _hotkeyService?.ShowNotifications ?? true;
            set
            {
                if (_hotkeyService != null)
                {
                    _hotkeyService.ShowNotifications = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool EnableConflictDetection
        {
            get => _hotkeyService?.EnableConflictDetection ?? true;
            set
            {
                if (_hotkeyService != null)
                {
                    _hotkeyService.EnableConflictDetection = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand SaveSettingsCommand => new RelayCommand(() =>
        {
            _hotkeyService?.SaveSettings();
        });
    }
}
