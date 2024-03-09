using CSharpOpenBMCLAPI.Modules;

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

        public void LogWarn(params object[] args)
        {
            lock (this)
            {
                Console.WriteLine(string.Join(" ", args));
            }
        }

        public void LogInfoNoNewLine(params object[] args)
        {
            lock (this)
            {
                Console.Write(string.Join(" ", args));
            }
        }

        public void LogWarnNoNewLine(params object[] args)
        {
            lock (this)
            {
                Console.Write(string.Join(" ", args));
            }
        }
    }
}
