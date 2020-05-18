using MVVMLib;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Input;
using Utils.Misc;

namespace NINI.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        public MainViewModel()
        {
            ToolTipText = GetIPString();
            NetworkChange.NetworkAddressChanged += new NetworkAddressChangedEventHandler(NetworkChange_NetworkAddressChanged);

            SyncTimeCommand = new RelayCommand(SyncTimeCommandAction);
            ExitCommand = new RelayCommand(ExitCommandAction);
        }

        private void SyncTimeCommandAction()
        {
            //var remoteTime = RemoteTOD.GetNow("time.windows.com", true);

            var dt = SyncTime.GetBJTime();

            SyncTime.SetSystemTime(dt);
        }

        private void ExitCommandAction()
        {
            Application.Current.Shutdown();
        }

        private void NetworkChange_NetworkAddressChanged(object sender, EventArgs e)
        { 
                var state = NetworkChecker.IsNetworkAvailable();
                if (state)
                {
                    Console.WriteLine("AvaiableChanged:" + state);


                    ToolTipText = GetIPString();
                }
                else
                {
                    ToolTipText = "网络连接已断开";
                }
        }

        private string _toolTipText;
        public string ToolTipText
        {
            get
            {
                return _toolTipText;
            }
            set
            {
                Set(ref _toolTipText, value);
            }
        }



        public ICommand SyncTimeCommand { get; set; }
        public ICommand ExitCommand { get; set; }

        /// <summary>
        /// 获取IP地址
        /// </summary>
        /// <returns></returns>
        public static string GetIPString()
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;

                return endPoint.Address.ToString();
            }
        }

    }
}
