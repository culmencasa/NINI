using MVVMLib;
using NINI.Helper;
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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NINI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : SingleWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            this.Hide();

            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
        }


        RunWindow runWindow;

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (runWindow == null)
            {
                runWindow = new RunWindow();
            }


            //pageTransition.ShowPage(runWindow);
        }


    }






}
