using CSharpOpenBMCLAPI.Modules.Storage;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using SocketIOClient;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using TeraIO.Runnable;

namespace CSharpOpenBMCLAPI.Modules
{
    public static class Utils
    {
        /// <summary>
        /// 打印 <seealso cref="SocketIOClient.SocketIOResponse"/> 包含的数据
        /// </summary>
        /// <param name="resp"></param>
        public static void PrintResponseMessage(SocketIOResponse resp)
        {
            JsonElement element = resp.GetValue<JsonElement>();
            PrintJsonElement(element);
        }

        /// <summary>
        /// 打印 <seealso cref="JsonElement"/> 中的内容，用于 <seealso cref="PrintResponseMessage(SocketIOResponse)"/> 遍历打印数据
        /// </summary>
        /// <param name="element"></param>
        public static void PrintJsonElement(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Null) return;
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                case JsonValueKind.Number:
                case JsonValueKind.Object:
                    Logger.Instance.LogInfo(element);
                    break;
                case JsonValueKind.Array:
                    foreach (var i in element.EnumerateArray())
                    {
                        try
                        {
                            var message = JsonConvert.DeserializeAnonymousType(i.ToString(), new { message = "" });
                            if (message != null)
                            {
                                Logger.Instance.LogInfo(message.message);
                                continue;
                            }
                            PrintJsonElement(i);
                        }
                        catch { }
                    }
                    break;
                default:
                    break;

            }
        }

        /// <summary>
        /// 关闭节点
        /// </summary>
        /// <param name="cluster"></param>
        /// <returns></returns>
        public static async Task ExitCluster(Cluster cluster)
        {
            cluster.WantEnable = false;
            cluster.cancellationSrc.Cancel();
            await cluster.KeepAlive();
            Thread.Sleep(1000);
            await cluster.Disable();
            cluster.IsEnabled = false;
        }

        /// <summary>
        /// 获取运行时版本
        /// </summary>
        /// <returns></returns>
        public static string GetRuntime()
        {
            var version = Environment.Version;
            return $"Dotnet-CSharp/v{version}";
        }

        /// <summary>
        /// 获取文件存储类型
        /// </summary>
        /// <param name="storage"></param>
        /// <returns></returns>
        public static string GetStorageType(IStorage storage) => "file";

        /// <summary>
        /// 将以 <seealso cref="long"/> 值存储的 bytes 数据输出为可读的字符串
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static string GetLength(long bytes)
        {

            if (bytes < 1024)
                return $"{string.Format("{0:F2}", (bytes))} B";
            else if (bytes > 1024 && bytes <= Math.Pow(1024, 2))
                return $"{string.Format("{0:F2}", (bytes / 1024.0))} KB";
            else if (bytes > Math.Pow(1024, 2) && bytes <= Math.Pow(1024, 3))
                return $"{string.Format("{0:F2}", (bytes / 1024.0 / 1024.0))} MB";
            else if (bytes > Math.Pow(1024, 3) && bytes <= Math.Pow(1024, 4))
                return $"{string.Format("{0:F2}", (bytes / 1024.0 / 1024.0 / 1024.0))} GB";
            else if (bytes > Math.Pow(1024, 4) && bytes <= Math.Pow(1024, 5))
                return $"{string.Format("{0:F2}", (bytes / 1024.0 / 1024.0 / 1024.0 / 1024.0))} TB";
            else
                return bytes.ToString();
        }

        /// <summary>
        /// 从哈希值获取文件名
        /// </summary>
        /// <param name="hash"></param>
        /// <returns></returns>
        public static string HashToFileName(string hash) => $"{hash[0..2]}/{hash}";

        /// <summary>
        /// 检验文件
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="hash"></param>
        /// <returns></returns>
        public static bool ValidateFile(Stream stream, string hash)
        {
            string checkSum;

            if (hash.Length == 32)
            {
                checkSum = Convert.ToHexString(MD5.HashData(stream)).ToLower();
            }
            else
            {
                checkSum = Convert.ToHexString(SHA1.HashData(stream)).ToLower();
            }
            return checkSum == hash;
        }

        /// <summary>
        /// 检验文件并输出哈希值
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="hash"></param>
        /// <param name="realHash"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 将 36 进制的数据转换成 10 进制的数据
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
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
            query?.Split('&').ForEach(s =>
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

        public static string ExceptionToDetail(this Exception ex)
        {
            return $"""
                {ex.GetType().FullName}: {ex.Message}
                {ex.StackTrace}
                """;
        }

        public static IEnumerator<T> GetEnumerator<T>(this IEnumerator<T> enumerator) => enumerator;

        public static byte[] BsonSerializeObject(object obj)
        {
            MemoryStream stream = new MemoryStream();
            using (BsonDataWriter writer = new BsonDataWriter(stream))
            {
                Newtonsoft.Json.JsonSerializer serializer = new();
                serializer.Serialize(writer, obj);
                return stream.ToArray();
            }
        }

        public static T? BsonDeserializeObject<T>(byte[] bytes)
        {
            MemoryStream stream = new MemoryStream(bytes);
            using (BsonDataReader reader = new BsonDataReader(stream))
            {
                Newtonsoft.Json.JsonSerializer serializer = new();
                T? e = serializer.Deserialize<T>(reader);
                return e;
            }
        }

        public static Stream? GetEmbeddedFileStream(string file)
        {
            Type? type = MethodBase.GetCurrentMethod()?.DeclaringType;
            string? _namespace = type?.Namespace;
            Assembly _assembly = Assembly.GetExecutingAssembly();
            string resourceName = $"{_namespace}.{file.Replace('/', '.')}";
            Stream? stream = _assembly.GetManifestResourceStream(resourceName);
            return stream;
        }
    }
}
