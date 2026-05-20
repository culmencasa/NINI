using NINI.Views.Settings.Pages;
using System.Windows;

namespace NINI.Views.Settings
{
    public partial class SettingsWindow2 : Wpf.Ui.Controls.FluentWindow
    {
        public SettingsWindow2()
        {
            InitializeComponent();
            
            // 默认导航到常规页面
            NavigationView.Navigate(typeof(GeneralPage));
        }
    }
}
