using NINI.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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

namespace NINI.Controls
{
    /// <summary>
    /// ToDoList.xaml 的交互逻辑
    /// </summary>
    public partial class ToDoList : UserControl
    {
        public ToDoList()
        {
            InitializeComponent();
        }

        private void txtItemTitle_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtItemTitle.Text.Length == 0)
            {
                lblTip.Visibility = Visibility.Visible;
            }
            else
            {
                lblTip.Visibility = Visibility.Collapsed;
            }
        }

        private void txtItemTitle_KeyUp(object sender, KeyEventArgs e)
        {
            OnEnterPressed?.Invoke();
        }
    }
}
