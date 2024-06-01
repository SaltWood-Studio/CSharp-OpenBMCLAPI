namespace CSharpOpenBMCLAPI
{
    public class Logger
    {
        private static readonly Logger _instance = new Logger();

        // 通过单例控制来使得 Logger 类不会被创建多次，进而保证日志写入锁只有一个，保证日志输出不会错位
        public static Logger Instance { get => _instance; }

        private Logger()
        {

        }

        private static void WriteLine(object[] args)
        {
            Console.Write("\r");
            Console.WriteLine(string.Join(" ", args));
        }

        private static void Write(object[] args)
        {
            Console.Write("\r");
            Console.Write(string.Join(" ", args));
        }

        public void LogDebug(params object[] args)
        {
            lock (this)
            {
                Console.ForegroundColor = ConsoleColor.White;
                WriteLine(args);
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }

        public void LogInfo(params object[] args)
        {
            lock (this)
            {
                WriteLine(args);
            }
        }

        public void LogWarn(params object[] args)
        {
            lock (this)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                WriteLine(args);
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }

        public void LogError(params object[] args)
        {
            lock (this)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                WriteLine(args);
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }

        public void LogDebugNoNewLine(params object[] args)
        {
            lock (this)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Write(args);
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }

        public void LogInfoNoNewLine(params object[] args)
        {
            lock (this)
            {
                Write(args);
            }
        }

        public void LogWarnNoNewLine(params object[] args)
        {
            lock (this)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Write(args);
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }

        public void LogErrorNoNewLine(params object[] args)
        {
            lock (this)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Write(args);
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }

        public void LogSystem(params object[] args)
        {
            lock (this)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                WriteLine(args);
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }
    }
}
