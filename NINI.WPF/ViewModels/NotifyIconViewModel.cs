﻿using MVVMLib;
using NINI.Helper;
using NINI.Models;
using System;
using System.Collections.Generic;
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

            ToolTipText = GetLocalIPString();
            NetworkChange.NetworkAddressChanged += new NetworkAddressChangedEventHandler(NetworkChange_NetworkAddressChanged);

            SyncTimeCommand = new RelayCommand(SyncTimeCommandAction);
            ExitCommand = new RelayCommand(ExitCommandAction);
            OpenMainWindowCommand = new RelayCommand(OpenMainWindowAcion);

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
                    ToolTipText = GetLocalIPString();
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


        #endregion

        #region Public Methods

        /// <summary>
        /// 获取IP地址
        /// </summary>
        /// <returns></returns>
        public static string GetLocalIPString()
        {
            string ipString = "";
            try
            {
                using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                {
                    socket.Connect("8.8.8.8", 65530);
                    IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;

                    ipString = endPoint.Address.ToString();
                }
            }
            catch (Exception ex)
            {
                ipString = "No IP Address";
                EZLogger.Default.Error(ex.StackTrace);
            }

            return ipString;
        }

        #endregion
    }
}
