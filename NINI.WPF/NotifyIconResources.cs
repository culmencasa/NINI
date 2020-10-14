using NINI.Helper;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace NINI
{
    public partial class NotifyIconResources
    {
        /// <summary>
        /// 单击事件
        /// 发消息还是太慢了...
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void TrayLeftMouseDownHandler(object sender, RoutedEventArgs e)
        {

            TodoWindow todoWindow = WindowManager.Single<TodoWindow>();
            todoWindow.Show();
        }
    }
}
