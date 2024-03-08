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
        public static bool CheckSign(string hash, string secret, string s, string e)
        {
            if (string.IsNullOrEmpty(e) || string.IsNullOrEmpty(s))
            {
                return false;
            }
            var sha1 = SHA1.Create();
            var sign = Convert.ToBase64String(sha1.ComputeHash(Encoding.UTF8.GetBytes($"{secret}{hash}{e}")));
            return sign == s && DateTime.Now.Ticks < (Convert.ToInt32(e, 36) / 100);
        }

    }
}
