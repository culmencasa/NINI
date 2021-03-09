using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace NINI.Helper
{
    public static class SyncTime
    {
        public static DateTime GetUtc()
        {

            var ntpData = new byte[48];
            ntpData[0] = 0x1B;

            string[] ntpServers = {
                                    "ntp1.aliyun.com",
                                    "ntp2.aliyun.com",
                                    "ntp3.aliyun.com",
                                    "ntp4.aliyun.com",
                                    "ntp5.aliyun.com",
                                    "ntp6.aliyun.com",
                                    "ntp7.aliyun.com"
            };


            var socket =
                new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp) { ReceiveTimeout = 3000 };

            foreach (var server in ntpServers)
            {
                try
                {
                    var addresses = Dns.GetHostEntry(server).AddressList;
                    var ipEndPoint = new IPEndPoint(addresses[0], 123);
                    socket.Connect(ipEndPoint);
                    if (socket.Connected)
                        break;
                }
                catch
                {
                    continue;
                }
            }
            socket.Send(ntpData);
            socket.Receive(ntpData);
            socket.Close();

            var intPart = ((ulong)ntpData[40] << 24) | ((ulong)ntpData[41] << 16) | ((ulong)ntpData[42] << 8) | ntpData[43];
            var fractPart = ((ulong)ntpData[44] << 24) | ((ulong)ntpData[45] << 16) | ((ulong)ntpData[46] << 8) | ntpData[47];

            var milliseconds = intPart * 1000 + fractPart * 1000 / 0x100000000L;
            var networkDateTime = new DateTime(1900, 1, 1).AddMilliseconds((long)milliseconds);

            return networkDateTime;
        }

        public static DateTime GetBJTime()
        {
            DateTime utcTime = GetUtc();

            return utcTime.AddHours(8);
        }


        public static bool SetSystemTime(DateTime dt)
        { 
            //转换System.DateTime到SystemTime   
            SystemTime st = new SystemTime();
            st.FromDateTime(dt);

            //调用Win32 API设置系统时间   
            SetLocalTime(ref st);

            return true;
        }



        [DllImport("Kernel32.dll")]
        public static extern bool SetLocalTime(ref SystemTime Time);

        [DllImport("Kernel32.dll")]
        public static extern void GetLocalTime(ref SystemTime Time);

        public struct SystemTime
        {
            public ushort wYear;
            public ushort wMonth;
            public ushort wDayOfWeek;
            public ushort wDay;
            public ushort wHour;
            public ushort wMinute;
            public ushort wSecond;
            public ushort wMilliseconds;

            /// <summary>   
            /// 从System.DateTime转换。   
            /// </summary>   
            /// <param name="time">System.DateTime类型的时间。</param>   
            public void FromDateTime(DateTime time)
            {
                wYear = (ushort)time.Year;
                wMonth = (ushort)time.Month;
                wDayOfWeek = (ushort)time.DayOfWeek;
                wDay = (ushort)time.Day;
                wHour = (ushort)time.Hour;
                wMinute = (ushort)time.Minute;
                wSecond = (ushort)time.Second;
                wMilliseconds = (ushort)time.Millisecond;
            }
            /// <summary>   
            /// 转换为System.DateTime类型。   
            /// </summary>   
            /// <returns></returns>   
            public DateTime ToDateTime()
            {
                return new DateTime(wYear, wMonth, wDay, wHour, wMinute, wSecond, wMilliseconds);
            }
            /// <summary>   
            /// 静态方法。转换为System.DateTime类型。   
            /// </summary>   
            /// <param name="time">SYSTEMTIME类型的时间。</param>   
            /// <returns></returns>   
            public static DateTime ToDateTime(SystemTime time)
            {
                return time.ToDateTime();
            }
        }


    }
}
