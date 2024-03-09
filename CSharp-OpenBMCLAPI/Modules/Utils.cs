using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

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

    }
}
