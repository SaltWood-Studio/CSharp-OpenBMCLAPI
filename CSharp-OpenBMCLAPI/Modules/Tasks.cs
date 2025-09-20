namespace CSharpOpenBMCLAPI.Modules
{
    internal class Tasks
    {
        public static Task? KeepAlive { get; set; }
        public static Timer? CheckFile { get; set; }
    }
}
