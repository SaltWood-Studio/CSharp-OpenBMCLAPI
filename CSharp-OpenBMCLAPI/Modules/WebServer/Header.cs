using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpOpenBMCLAPI.Modules.WebServer
{
    public class Header
    {
        Dictionary<string, string> tables;

        public Header(Dictionary<string, string> tables)
        {
            this.tables = tables;
        }

        public Header()
        {
            this.tables = new Dictionary<string, string>();
        }

        public static Header FromBytes(byte[][] headers)
        {
            Dictionary<string, string> tables = new Dictionary<string, string>();
            foreach (var header in headers)
            {
                var h = WebUtils.SplitBytes(header, WebUtils.Encode(": ").ToArray(), 1).ToArray();
                if (h.Length < 1)
                {
                    continue;
                }
                tables.TryAdd(WebUtils.Decode(h[0]), WebUtils.Decode(h[1]));
            }
            return new Header(tables);
        }

        public object Get(string key, object defaultValue)
        {
            return tables.ContainsKey(key) ? tables[key] : defaultValue;
        }

        public void Set(string key, object value)
        {
            if (value == null)
            {
                if (tables.ContainsKey(key)) tables.Remove(key);
                return;
            }
            tables.TryAdd(key, value + "");
        }

        public bool ContainsKey(string key)
        {
            return tables.ContainsKey(key);
        }

        public override string ToString()
        {
            string header = "";
            foreach (var key in tables.Keys)
            {
                header += key + ": " + tables[key] + "\r\n";
            }
            return header;
        }
    }
}
