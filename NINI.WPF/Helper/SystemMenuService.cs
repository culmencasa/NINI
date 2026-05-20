using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace NINI.Helper
{
    internal class SystemMenuService : IDisposable
    {
        private bool _isInstalled = false;
        private bool _disposed = false;

        [DllImport("TopMostHook.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern bool InstallMenuToolsHook();

        [DllImport("TopMostHook.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern void UninstallMenuToolsHook();

        /// <summary>
        /// 安装全局窗口置顶钩子
        /// </summary>
        internal void Install()
        {
            if (_isInstalled)
                return;

            bool result = InstallMenuToolsHook();
            if (result)
            {
                _isInstalled = true;
            }
            else
            {
                throw new InvalidOperationException("Failed to install system menu hook");
            }
        }

        /// <summary>
        /// 卸载全局窗口置顶钩子
        /// </summary>
        internal void Uninstall()
        {
            if (!_isInstalled)
                return;

            UninstallMenuToolsHook();
            _isInstalled = false;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                // 卸载钩子
                Uninstall();
            }

            _disposed = true;
        }

        ~SystemMenuService()
        {
            Dispose(false);
        }
    }
}
