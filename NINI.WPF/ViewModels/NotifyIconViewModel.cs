using MVVMLib;
using NINI.Helper;
using NINI.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Utils.Misc;

namespace NINI.ViewModels
{
    public class NotifyIconViewModel : ViewModelBase
    {
        public NotifyIconViewModel()
        {
            //todo:加上外网IP


            var netInfo = GetLocalIPString();
            ToolTipText = $"IPv4地址: {netInfo.IP} \r\n 网关地址: {netInfo.Gateway}";
            NetworkChange.NetworkAddressChanged += new NetworkAddressChangedEventHandler(NetworkChange_NetworkAddressChanged);

            SyncTimeCommand = new RelayCommand(SyncTimeCommandAction);
            ExitCommand = new RelayCommand(ExitCommandAction);
            OpenMainWindowCommand = new RelayCommand(OpenMainWindowAcion);
            ThisComputerCommand = new RelayCommand(ThisComputerAction);
            RunDialogCommand = new RelayCommand(RunDialogAction);
            ShutDownPCCommand = new RelayCommand(ShutDownPCAction);
            //SimpleMessenger.Default.Subscribe<NotifyIconViewMessage>(this, HandleMainViewMessage);
        }



        #region Actions for Commands, Messages

        private async void SyncTimeCommandAction()
        {
            await Task.Run(() =>
            {
                var dt = SyncTime.GetBJTime();

                if (DateTime.Now.ToString("yyyy-MM-dd HH:mm") == dt.ToString("yyyy-MM-dd HH:mm"))
                {
                    return;
                }

                SyncTime.SetSystemTime(dt);

                SimpleMessenger.Default.Publish(new GeneralMessage()
                {
                    Signal = GeneralMessage.Types.ShowBallon,
                    Title = "Time Synced.",
                    Content = DateTime.Now.ToString()
                });
            });            
        }

        private void ExitCommandAction()
        {
            App.Instance.KillProcesses(AppConst.CONSOLE_PROCESS_NAME);
            Application.Current.Shutdown();
        }


        // 废弃
        private void OpenMainWindowAcion()
        {
            SimpleMessenger.Default.Publish(new NotifyIconViewMessage()
            {
                Signal = NotifyIconViewMessage.Signals.OpenWindow,
                Parameter = null
            });
        }

        [STAThread]
        private void RunDialogAction()
        {
            // 注释: 原代码使用 Shell32 COM 组件，因.NET 6 不支持 COM 引用而修改
            // Microsoft Shell Controls and Automation
            // Shell32.Shell shell = new Shell32.Shell();
            // shell.FileRun();
            
            // 使用 Win32 API 打开运行对话框
            try
            {
                System.Diagnostics.Process.Start("rundll32.exe", "shell32.dll,#61");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"打开运行对话框失败：{ex.Message}");
            }
        }

        private void ThisComputerAction()
        {
            System.Diagnostics.Process.Start("explorer.exe", "");
        }


        private void ShutDownPCAction()
        {
            string fileName = System.Environment.GetFolderPath(Environment.SpecialFolder.System) + "\\wscript.exe";

            string argument = AppDomain.CurrentDomain.BaseDirectory + "Resources\\shutdown.vbs";

            if (File.Exists(fileName) && File.Exists(argument))
            {
                System.Diagnostics.Process.Start(fileName, argument);
            }
        }

        #endregion

        private async void HandleMainViewMessage(NotifyIconViewMessage msg)
        {
            switch (msg.Signal)
            {
                case NotifyIconViewMessage.Signals.SyncTime:
                    await Task.Run(()=>
                    {
                        bool synced = false;
                        try
                        {
                            var dt = SyncTime.GetBJTime();
                            synced = SyncTime.SetSystemTime(dt);
                        }
                        catch
                        {

                        }
                        finally
                        {
                            if (synced)
                            {
                                if (msg.Parameter?.ToString() != "Slient")
                                {
                                    SimpleMessenger.Default.Publish(new GeneralMessage()
                                    {
                                        Signal = GeneralMessage.Types.ShowBallon,
                                        Title = "Time Synced.",
                                        Content = DateTime.Now.ToString()
                                    });
                                }
                            }
                        } 
                    });
                    
                    break;
                default:
                    break;
            }
        }


        private void NetworkChange_NetworkAddressChanged(object sender, EventArgs e)
        { 
                var state = NetworkChecker.IsNetworkAvailable();
                if (state)
                {
                    var netInfo = GetLocalIPString();
                    ToolTipText = $"IPv4地址: {netInfo.IP} \r\n 网关地址: {netInfo.Gateway}";
            }
                else
                {
                    ToolTipText = "网络状态丢失";
                }
        }


        #region Fields

        private string _toolTipText;


        #endregion

        #region ViewProperties

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

        #endregion

        #region Commands

        public ICommand SyncTimeCommand { get; set; }
        public ICommand ExitCommand { get; set; }
        public ICommand OpenMainWindowCommand { get; private set; }

        public ICommand ThisComputerCommand { get; set; }

        public ICommand RunDialogCommand { get; set; }

        public ICommand ShutDownPCCommand { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// 获取本地IP和网关信息
        /// </summary>
        /// <returns></returns>
        public static (string IP, string Gateway) GetLocalIPString()
        {
            string ipString = "";
            string gatewayString = "";
            try
            {
                using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                {
                    socket.Connect("8.8.8.8", 65530);
                    IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;

                    ipString = endPoint.Address.ToString();
                }

                // 获取IPv4网关地址
                foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus == OperationalStatus.Up &&
                        ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    {
                        var ipProps = ni.GetIPProperties();
                        var gatewayAddresses = ipProps.GatewayAddresses;
                        foreach (var gateway in gatewayAddresses)
                        {
                            // 只获取IPv4网关
                            if (gateway.Address.AddressFamily == AddressFamily.InterNetwork)
                            {
                                gatewayString = gateway.Address.ToString();
                                break;
                            }
                        }
                        if (!string.IsNullOrEmpty(gatewayString))
                            break;
                    }
                }

                if (string.IsNullOrEmpty(gatewayString))
                {
                    gatewayString = "未知";
                }
            }
            catch (Exception ex)
            {
                ipString = "No IP Address";
                gatewayString = "未知";
                EZLogger.Default.Error(ex.StackTrace);
            }

            return (ipString, gatewayString);
        }

        #endregion
    }
}
