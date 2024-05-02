using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpOpenBMCLAPI.Modules
{
    internal class Tasks
    {
        public static Task? KeepAlive { get; set; }
        public static Task? CheckFile { get; set; }
    }
}
