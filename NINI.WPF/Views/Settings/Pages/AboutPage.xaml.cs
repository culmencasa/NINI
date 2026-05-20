using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace NINI.Views.Settings.Pages
{
    public partial class AboutPage : Page
    {
        public AboutPage()
        {
            InitializeComponent();
        }

        private void GitHub_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com",
                UseShellExecute = true
            });
        }

        private void Issues_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com",
                UseShellExecute = true
            });
        }
    }
}
