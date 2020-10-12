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

            this.Loaded += MainWindow_Loaded;
            this.Closed += MainWindow_Closed;
        }

        public bool IsClosed { get; set; }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            if (IsClosed)
            {
            }
        }


        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            if (!e.Cancel)
            {
                IsClosed = true;
            }
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {            
            //仅支持文件的拖放            
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                return;
            }
            //获取拖拽的文件            
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            string url = LnkHelper.ResolveShortcut(files[0]);

            //这里需要注意，由于程序既支持拖过来也支持拖过去，那么ListBox就也能接收自身拖拽过来的文件         
            //为了防止鼠标点击和拖拽的冲突，需要屏蔽从程序自身拖拽过来的文件        
            //这里判断文件是否从程序外部拖拽进来，也就是判断图片是否在工作目录下         
            //if (files.Length > 0 && !files[0].StartsWith(path) &&
            //    (e.AllowedEffects & DragDropEffects.Copy) == DragDropEffects.Copy)
            //{ 
            //    e.Effects = DragDropEffects.Copy;
            //}
            //else { 
            //    e.Effects = DragDropEffects.None;
            //}

            //foreach (string file in files)
            //{
            //    try
            //    {   
            //        string destFile = path + System.IO.Path.GetFileName(file);
            //        switch (e.Effects)
            //        {
            //            case DragDropEffects.Copy:
            //                File.Copy(file, destFile, false);
            //                bmi = new BitmapImage(new Uri(destFile));
            //                imgShow.Source = bmi;
            //                lstImage.Items.Add(destFile);
            //                break;
            //            default:
            //                break;
            //        }
            //    }
            //    catch
            //    {
            //        MessageBox.Show("已存在此文件或导入了非图像文件！");
            //    }
            //}
        }

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Copy;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //RunWindow runWindow = new RunWindow();
            //runWindow.Show();

            //Button btn = sender as Button;
            //btn.Background = Brushes.Green;
            //btn.IsEnabled = false;
        }
    }


    // Create your POCO class entity
    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<string> Phones { get; set; }
        public bool IsActive { get; set; }
    }


}
