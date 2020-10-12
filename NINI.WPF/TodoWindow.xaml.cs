using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace NINI
{
    /// <summary>
    /// TodoWindow.xaml 的交互逻辑
    /// </summary>
    public partial class TodoWindow : Window
    {
        public TodoWindow()
        {
            InitializeComponent();

            Loaded += TodoWindow_Loaded;
        }

        private void TodoWindow_Loaded(object sender, RoutedEventArgs e)
        {
            string url = "https://to-do.live.com/tasks/";
            url = "http://127.0.0.1:13554";
            browser.Navigate(url);
        }

        public void SuppressScriptErrors(WebBrowser webBrowser, bool Hide)
        {
            FieldInfo fiComWebBrowser = typeof(WebBrowser).GetField("_axIWebBrowser2", BindingFlags.Instance | BindingFlags.NonPublic);
            if (fiComWebBrowser == null) return;

            object objComWebBrowser = fiComWebBrowser.GetValue(webBrowser);
            if (objComWebBrowser == null) return;

            objComWebBrowser.GetType().InvokeMember("Silent", BindingFlags.SetProperty, null, objComWebBrowser, new object[] { Hide });
        }

        private void browser_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            SuppressScriptErrors((WebBrowser)sender, true);
        }
    }
}
