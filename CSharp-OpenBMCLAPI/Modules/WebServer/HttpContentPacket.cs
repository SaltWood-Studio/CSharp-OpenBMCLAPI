namespace CSharpOpenBMCLAPI.Modules.WebServer
{
    public class HttpContentPacket
    {
        public HttpContentPacket()
        {

        }

        [Obsolete($"Please use {nameof(HttpContentPacket.Create)} instead.")]
        public HttpContentPacket(byte[] data)
        {

        }

        public static HttpContentPacket? Create(byte[] bytes)
        {
            // TODO: HttpContentPacket
            return null;
        }
    }
}