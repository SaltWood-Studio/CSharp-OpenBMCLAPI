namespace CSharpOpenBMCLAPI.Modules.Storage
{
    public interface ICachedStorage : IStorage
    {
        public abstract long GetCachedFiles();
        public abstract long GetCachedMemory();
    }
}
