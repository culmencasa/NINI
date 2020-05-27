using MVVMLib;
using NINI.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NINI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            this.Hide();
            SimpleMessenger.Default.Subscribe<MainViewMessage>(this, HandleSimpleCommand);
        }

        private void HandleSimpleCommand(MainViewMessage msg)
        {
            switch (msg.Signal)
            {
                case MainViewMessage.Signals.OpenWindow:
                    {
                        this.Show();
                        if (this.Visibility != Visibility.Visible)
                            this.Visibility = Visibility.Visible;

                        if (this.WindowState == WindowState.Minimized)
                            this.WindowState = WindowState.Normal;

                        if (!this.IsActive)
                            this.Activate();
                    }
                    break;
                default:
                    break;
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
            base.OnClosing(e);
        }
    }
}
