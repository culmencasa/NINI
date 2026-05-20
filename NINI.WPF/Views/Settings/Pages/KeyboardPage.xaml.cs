using NINI.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;

namespace NINI.Views.Settings.Pages
{
    public partial class KeyboardPage : Page
    {
        private readonly SettingsViewModel _viewModel;

        public KeyboardPage()
        {
            InitializeComponent();
            var hotkeyService = App.GetHotkeyService()!;
            _viewModel = new SettingsViewModel(hotkeyService);
            DataContext = _viewModel;
        }

        private void HotkeyItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is System.Windows.Controls.Border border && border.DataContext is HotkeyItemViewModel item)
            {
                var dialog = new Views.HotkeyEditDialog(item.Model)
                {
                    Owner = System.Windows.Window.GetWindow(this)
                };
                if (dialog.ShowDialog() == true)
                {
                    _viewModel.RefreshCommand.Execute(null);
                }
            }
        }
    }
}
