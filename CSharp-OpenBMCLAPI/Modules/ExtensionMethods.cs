using System.Reflection;
using System.Text;

namespace CSharpOpenBMCLAPI.Modules
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// 最便捷的方式，保证 null 值不会被使用
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item"></param>
        /// <returns>当输入值不为 <seealso cref="null"/> 时，返回输入值；当输入值为 <seealso cref="null"/> 时触发 <seealso cref="ArgumentNullException"/></returns>
        /// <exception cref="ArgumentException"></exception>
        public static T ThrowIfNull<T>(this T? item)
        {
            ArgumentNullException.ThrowIfNull(item, nameof(item));
            return item;
        }

        public static string ToStandardTimeString(this DateTime dt) => dt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

        public static void PrintTypeInfo<T>(this T instance)
        {
            Type type = typeof(T);
            Console.WriteLine($"Type: {type.Name}");

            // Print fields
            Console.WriteLine("Fields:");
            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                Console.WriteLine($"|---{field.Name}: {field.FieldType.Name} = {field.GetValue(instance)}");
            }

            // Print private fields
            Console.WriteLine("NonPublic fields:");
            foreach (FieldInfo field in type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                Console.WriteLine($"|---{field.Name}: {field.FieldType.Name} = {field.GetValue(instance)}");
            }

            // Print properties
            Console.WriteLine("Properties:");
            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                Console.WriteLine($"|---{property.Name}: {property.PropertyType.Name} = {property.GetValue(instance)}");
            }

            // Print methods
            Console.WriteLine("Methods:");
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                Console.WriteLine($"|---{method.Name}({string.Join(", ", method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"))})");
                if (method.ReturnType != typeof(void))
                {
                    Console.WriteLine($"|   |---returns: {method.ReturnType.Name}");
                }
            }
        }

        public static List<byte[]> Split(this byte[] bytes, string key, int count = 0) => bytes.Split(Encoding.UTF8.GetBytes(key), count);

        public static List<byte[]> Split(this byte[] bytes, byte[] key, int count = 0)
        {
            if (count <= -1)
                count = 0;
            else
                count += 1;
            int data_length = bytes.Length;
            int key_length = key.Length;
            List<byte[]> splitted_data = [];
            int start = 0;
            int cur = 0;
            bool ckd = false;
            foreach (int i in Enumerable.Range(0, data_length))
            {
                if (bytes[i] == key[0] && i + key_length <= data_length)
                    ckd = true;
                foreach (int j in Enumerable.Range(1, key_length - 1))
                {
                    if (bytes[i + j] != key[j])
                        ckd = false;
                    break;
                }
                if (ckd)
                {
                    splitted_data.Add(bytes[start..(i - 1)]);
                    cur += 1;
                    start = i + key_length;
                }
                if (count != 0 && cur >= count)
                    break;
            }
            if (count == 0 || cur < count)
                splitted_data.Add(bytes[start..]);
            return splitted_data;
        }
    }
}
