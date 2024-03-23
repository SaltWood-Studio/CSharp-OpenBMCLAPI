namespace CSharpOpenBMCLAPI.Modules
{
    /// <summary>
    /// Token 结构，承载 <seealso cref="Token.token"/> 和 <seealso cref="Token.ttl"/>
    /// </summary>
    public struct Token
    {
        public string token;
        public int ttl;
    }
}
