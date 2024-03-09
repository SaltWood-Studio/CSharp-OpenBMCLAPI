using System.Reflection;

namespace CSharpOpenBMCLAPI.Modules
{
    public static class ExtensionMethods
    {
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
    }
}
