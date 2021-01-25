using NINI.Controls;
using NINI.Models;
using NINI.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace NINI.Views
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

        private void CrossButton_Click(object sender, RoutedEventArgs e)
        {
            var item = ((CrossButton)sender).Tag as ToDo;
            var vm = this.DataContext as ToDoListViewModel;
            vm.DeleteCommand.Execute(item);
        }

        private void txtItemTitle_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                //todo: 如果光标在首位, 则在之前插入


                var item = ((TextBox)sender).Tag as ToDo;
                var vm = this.DataContext as ToDoListViewModel;
                vm.AppendAfter(item);


                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    UIElement elementWithFocus = Keyboard.FocusedElement as UIElement;
                    TraversalRequest request = new TraversalRequest(FocusNavigationDirection.Down);
                    if (elementWithFocus != null)
                    {
                        elementWithFocus.MoveFocus(request);
                    }
                }), DispatcherPriority.ApplicationIdle);
            }
            else if (e.Key == Key.Up)
            {
                UIElement elementWithFocus = Keyboard.FocusedElement as UIElement;
                TraversalRequest request = new TraversalRequest(FocusNavigationDirection.Up);
                if (elementWithFocus != null)
                {
                    elementWithFocus.MoveFocus(request);
                }
            }
            else if (e.Key == Key.Down)
            {
                UIElement elementWithFocus = Keyboard.FocusedElement as UIElement;
                TraversalRequest request = new TraversalRequest(FocusNavigationDirection.Down);
                if (elementWithFocus != null)
                {
                    elementWithFocus.MoveFocus(request);
                }
            }
        }

        private void txtItemTitle_LostFocus(object sender, RoutedEventArgs e)
        {
            var control = (TextBox)sender;
            var text = control.Text;
            var item = control.Tag as ToDo;

            if (string.IsNullOrEmpty(text))
            {
                var vm = this.DataContext as ToDoListViewModel;
                vm.Giveup(item);
            }
        }
    }
}
