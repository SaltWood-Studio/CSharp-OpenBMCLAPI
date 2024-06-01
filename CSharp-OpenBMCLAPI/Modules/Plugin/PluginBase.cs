namespace CSharpOpenBMCLAPI.Modules.Plugin
{
    public abstract class PluginBase
    {
        public virtual string Name { get; private set; } = "Default";
        public virtual string Description { get; private set; } = "Default plugin implementation";

        public virtual void OnProgramStarted()
        {

        }

        public virtual void OnClusterStarted(Cluster? cluster)
        {

        }

        public virtual void OnClusterStopped(Cluster? cluster)
        {

        }

        public virtual void OnProgramStopped()
        {

        }

        public virtual void RegisterClientEvents()
        {

        }

        public virtual void RegisterStorageType()
        {

        }

        public override string ToString()
        {
            return $"<{this.GetType().FullName} Name={this.Name} at 0x{this.GetHashCode().ToString("X").PadLeft(16, '0')}>";
        }
    }
}
