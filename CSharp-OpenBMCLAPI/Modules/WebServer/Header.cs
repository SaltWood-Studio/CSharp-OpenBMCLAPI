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
        public Header(Dictionary<string, string> tables) {
            this.tables = tables;
        }
        public Header()
        {
            this.tables = new Dictionary<string, string>();
        }
        public static Header fromBytes(byte[][] headers)
        {
            Dictionary<string, string> tables = new Dictionary<string, string>();
            foreach (var header in headers)
            {
                var h = WebUtils.SplitBytes(header, WebUtils.encode(": ").ToArray(), 1).ToArray();
                if (h.Length < 1)
                {
                    continue;
                }
                tables.TryAdd(WebUtils.decode(h[0]), WebUtils.decode(h[1]));
            }
            return new Header(tables);
        }
        public Object get(string key, Object def)
        {
            return tables.ContainsKey(key) ? tables[key] : def;
        }
        public void set(string key, Object value)
        {
            if (value == null)
            {
                if (tables.ContainsKey(key)) tables.Remove(key);
                return;
            } 
            tables.TryAdd(key, value + "");
        }
        public bool ContainsKey(string key) {
            return tables.ContainsKey(key);
        }
        public string ToString()
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
