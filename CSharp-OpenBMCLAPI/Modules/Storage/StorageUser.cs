namespace CSharpOpenBMCLAPI.Modules.Storage
{
    /// <summary>
    /// 当登录存储需要凭据时使用
    /// </summary>
    public struct StorageUser
    {
        public string UserName { get; set; }
        public string Password { get; set; }
    }
}