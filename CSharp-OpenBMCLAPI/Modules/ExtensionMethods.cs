using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpOpenBMCLAPI.Modules
{
    public static class ExtensionMethods
    {
        public static T ThrowIfNull<T>(this T? item)
        {
            ArgumentNullException.ThrowIfNull(item, nameof(item));
            return item;
        }
    }
}
