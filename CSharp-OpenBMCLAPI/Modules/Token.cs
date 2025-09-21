namespace CSharpOpenBMCLAPI.Modules
{
    /// <summary>
    /// Token 结构，承载 <seealso cref="Token.token"/> 和 <seealso cref="Token.ttl"/>
    /// </summary>
    public readonly struct Token
    {
        public readonly string token;
        public readonly int ttl;
    }
}
