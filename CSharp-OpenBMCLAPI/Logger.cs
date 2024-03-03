using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpOpenBMCLAPI
{
    public class Logger
    {
        public Logger()
        {

        }

        public void LogInfo(params object[] args)
        {
            lock (this)
            {
                Console.WriteLine(string.Join(" ", args));
            }
        }
    }
}
