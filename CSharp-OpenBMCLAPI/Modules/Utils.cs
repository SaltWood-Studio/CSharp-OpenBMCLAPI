using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;

namespace CSharpOpenBMCLAPI.Modules
{
    public static class Utils
    {
        static long ToDecimal(string hex)
        {
            Dictionary<char, int> pairs = new();
            int count = 0;
            foreach (char i in "0123456789abcdefghijklmnopqrstuvwxyz")
            {
                pairs[i] = count;
                count++;
            }
            long decimalValue = 0;
            for (int i = 0; i < hex.Length; i++)
            {
                char c = hex[i];
                int digit = pairs[c];
                decimalValue = decimalValue * 36 + digit;
            }
            return decimalValue;
        }

        public static string ToUrlSafeBase64String(string b) => b.Replace('/', '_').Replace('+', '-').Replace("=", "");
        public static string ToUrlSafeBase64String(byte[] b) => Convert.ToBase64String(b).Replace('/', '_').Replace('+', '-').Replace("=", "");

        public static bool CheckSign(string? hash, string? secret, string? s, string? e)
        {
            if (string.IsNullOrEmpty(e) || string.IsNullOrEmpty(s))
            {
                return false;
            }
            var sha1 = SHA1.Create();
            var sign = ToUrlSafeBase64String(sha1.ComputeHash(Encoding.UTF8.GetBytes($"{secret}{hash}{e}")));
            var timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
            var a = timestamp < (ToDecimal(e) / 100);
            return sign == s && timestamp < (ToDecimal(e) / 100);
        }

        public static bool IsAdministrator()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return true;
            WindowsIdentity current = WindowsIdentity.GetCurrent();
            WindowsPrincipal windowsPrincipal = new WindowsPrincipal(current);
            //WindowsBuiltInRole可以枚举出很多权限，例如系统用户、User、Guest等等
            return windowsPrincipal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public static void RunAsAdministrator()
        {
            //创建启动对象
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = Process.GetCurrentProcess().MainModule?.FileName,
                WorkingDirectory = Environment.CurrentDirectory,
                Verb = "runas",
                UseShellExecute = true,
                CreateNoWindow = false
            };
            try
            {
                Process.Start(startInfo);
            }
            catch
            {
                return;
            }
            Environment.Exit(0);
        }
    }
}
