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
    }
}
