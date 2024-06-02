using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace CSharpOpenBMCLAPI.Modules.WebServer
{
    public class Headers : IDictionary<string, string>
    {
        Dictionary<string, string> tables;

        public Headers(Dictionary<string, string> tables)
        {
            this.tables = tables;
        }

        public Headers()
        {
            this.tables = new Dictionary<string, string>();
        }

        public static Headers FromBytes(byte[][] headers)
        {
            Dictionary<string, string> tables = new Dictionary<string, string>();
            foreach (var header in headers)
            {
                var h = WebUtils.SplitBytes(header, WebUtils.Encode(": "), 1).ToArray();
                if (h.Length < 1)
                {
                    continue;
                }
                tables.TryAdd(WebUtils.Decode(h[0]).ToLower(), WebUtils.Decode(h[1]));
            }
            return new Headers(tables);
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

        public bool ContainsKey(string key) => tables.ContainsKey(key);

        public override string ToString() => string.Join("\r\n", from kvp in this select $"{kvp.Key}: {kvp.Value}");

        // Auto generated.

        public string this[string key] { get => tables[key]; set => tables[key] = value; }

        public ICollection<string> Keys => tables.Keys;

        public ICollection<string> Values => tables.Values;

        public int Count => tables.Count;

        public bool IsReadOnly => ((ICollection<KeyValuePair<string, string>>)tables).IsReadOnly;

        public void Add(string key, string value)
        {
            tables.Add(key, value);
        }

        public void Add(KeyValuePair<string, string> item)
        {
            ((ICollection<KeyValuePair<string, string>>)tables).Add(item);
        }

        public void Clear()
        {
            tables.Clear();
        }

        public bool Contains(KeyValuePair<string, string> item)
        {
            return tables.Contains(item);
        }

        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<string, string>>)tables).CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<string, string>>)tables).GetEnumerator();
        }

        public bool Remove(string key)
        {
            return tables.Remove(key);
        }

        public bool Remove(KeyValuePair<string, string> item)
        {
            return ((ICollection<KeyValuePair<string, string>>)tables).Remove(item);
        }

        public bool TryGetValue(string key, [MaybeNullWhen(false)] out string value)
        {
            return tables.TryGetValue(key, out value);
        }

        public string? TryGetValue(string key)
        {
            string? value;
            tables.TryGetValue(key, out value);
            return value;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)tables).GetEnumerator();
        }
    }
}
