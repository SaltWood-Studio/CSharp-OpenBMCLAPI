using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using TeraIO.Runnable;
using WindowsFirewallHelper;
using static System.Net.Mime.MediaTypeNames;

namespace CSharpOpenBMCLAPI.Modules
{
    public static class Utils
    {
        public static string HashToFileName(string hash) => $"{hash[0..2]}/{hash}";

        public static List<Task> tasks = new List<Task>();

        public static bool ValidateFile(byte[] buffer, string hash)
        {
            string checkSum;

            if (hash.Length == 32)
            {
                checkSum = Convert.ToHexString(MD5.HashData(buffer)).ToLower();
            }
            else
            {
                checkSum = Convert.ToHexString(SHA1.HashData(buffer)).ToLower();
            }
            return checkSum == hash;
        }

        public static bool ValidateFile(byte[] buffer, string hash, out string realHash)
        {
            string checkSum;

            if (hash.Length == 32)
            {
                checkSum = Convert.ToHexString(MD5.HashData(buffer)).ToLower();
            }
            else
            {
                checkSum = Convert.ToHexString(SHA1.HashData(buffer)).ToLower();
            }
            realHash = checkSum;
            return checkSum == hash;
        }

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

        public static Dictionary<string, string> GetQueryStrings(string? query)
        {
            Dictionary<string, string> pairs = new();
            query?[1..].Split('&').ForEach(s =>
            {
                var pair = s.Split('=');
                pairs[pair[0]] = pair[1];
            });
            return pairs;
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

        public static bool RunAsAdministrator()
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
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static string ExceptionToDetail(Exception ex)
        {
            return $"""
                {ex.GetType().FullName}: {ex.Message}
                {ex.StackTrace}
                """;
        }

        public static void CreatePortRule(string newPortRuleName, ushort portNumber, FirewallAction firewallAction, FirewallDirection firewallDirection)
        {
            //搜索规则
            var rule = FirewallManager.Instance.Rules.Where(r =>
            r.Direction == firewallDirection &&
            r.Name.Equals(newPortRuleName)
            ).FirstOrDefault();

            if (rule == null) // 指定的规则不存在
            {
                try
                {
                    rule = FirewallManager.Instance.CreatePortRule(
                        FirewallProfiles.Domain | FirewallProfiles.Private | FirewallProfiles.Public, // 生效的配置文件
                        newPortRuleName,
                        firewallAction, // 运作：允许或阻止
                        portNumber,
                        FirewallProtocol.TCP //协议

                    );

                    rule.Direction = firewallDirection; //方向

                    FirewallManager.Instance.Rules.Add(rule);

                    SharedData.Logger.LogInfo($"添加防火墙规则成功：<IFirewallRule {rule.Name} {string.Join(',', rule.LocalPorts)} => {string.Join(',', rule.RemotePorts)} {rule.Protocol} {rule.Action}>");
                }
                catch (Exception ex)
                {
                    SharedData.Logger.LogWarn($"添加防火墙规则失败：{ExceptionToDetail(ex)}");
                }
            }
            else
            {
                // FirewallManager.Instance.Rules.Remove(rule);
                SharedData.Logger.LogInfo($"防火墙规则已存在：<IFirewallRule {rule.Name} {string.Join(',', rule.LocalPorts)} => {string.Join(',', rule.RemotePorts)} {rule.Protocol} {rule.Action}>");
            }
        }
    }
}
