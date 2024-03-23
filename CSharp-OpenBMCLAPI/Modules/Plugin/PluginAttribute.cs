using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpOpenBMCLAPI.Modules.Plugin
{
    [AttributeUsage(AttributeTargets.Class)]
    public class PluginAttribute : Attribute
    {
        public bool Hidden { get; set; } = false;
    }
}
