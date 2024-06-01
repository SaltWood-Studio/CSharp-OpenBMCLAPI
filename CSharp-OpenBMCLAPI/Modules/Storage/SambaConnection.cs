using System.Runtime.InteropServices;

namespace CSharpOpenBMCLAPI.Modules.Storage
{
    public class SambaConnection : IDisposable
    {
        const int logonProvider = 0; // Default
        const int logonType = 9; // New credentials
        private bool disposed;

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern bool LogonUser(string pszUsername, string pszDomain, string pszPassword, int dwLogonType, int dwLogonProvider, ref nint phToken);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        extern static bool CloseHandle(nint handle);

        [DllImport("advapi32.DLL")]
        static extern bool ImpersonateLoggedOnUser(nint hToken);

        [DllImport("advapi32.DLL")]
        static extern bool RevertToSelf();

        public SambaConnection(string username, string password, string address)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) throw new PlatformNotSupportedException($"{nameof(SambaConnection)} 在你的系统环境（当前为 {Environment.OSVersion.Platform}, {RuntimeInformation.OSArchitecture}）不受支持。");

            nint existingTokenHandle = new nint(0);
            nint duplicateTokenHandle = new nint(0);

            try
            {
                bool impersonated = LogonUser(username, address, password, logonType, logonProvider, ref existingTokenHandle);

                if (impersonated)
                {
                    if (!ImpersonateLoggedOnUser(existingTokenHandle))
                    {
                        int errorCode = Marshal.GetLastWin32Error();
                        throw new Exception($"线程无法以 {username} 的身份登录并获取权限！错误代码：{errorCode}");
                    }
                }
                else
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    throw new Exception($"登录失败！错误代码：{errorCode}");
                }
            }
            finally
            {
                if (existingTokenHandle != nint.Zero)
                {
                    CloseHandle(existingTokenHandle);
                }

                if (duplicateTokenHandle != nint.Zero)
                {
                    CloseHandle(duplicateTokenHandle);
                }
            }
        }

        ~SambaConnection()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // 释放大型托管资源
                }
                // 释放非托管资源
                RevertToSelf();
                this.disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void CheckIfDisposed()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException($"对象 {this} 已被清理。");
            }
        }

        public override string ToString()
        {
            return $"<{this.GetType().FullName} object, HashCode = {this.GetHashCode()}>";
        }
    }
}
