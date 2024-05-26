using CSharpOpenBMCLAPI.Modules.WebServer;

namespace CSharpOpenBMCLAPI.Modules.Plugin
{
    public class PluginHttpEvent
    {
        public HttpEventType EventType { get; protected set; }
        public Action<HttpContext> Action { get; protected set; }

        public PluginHttpEvent(Action<HttpContext> action, HttpEventType type)
        {
            this.EventType = type;
            this.Action = action;
        }

        public void Trigger(HttpEventType eventType, HttpContext context)
        {
            if (this.EventType == eventType) this.Action.Invoke(context);
        }
    }
}