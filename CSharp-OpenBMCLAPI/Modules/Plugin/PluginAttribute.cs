namespace CSharpOpenBMCLAPI.Modules.Plugin
{
    [AttributeUsage(AttributeTargets.Class)]
    public class PluginAttribute : Attribute
    {
        public bool Hidden { get; set; } = false;
    }
}
