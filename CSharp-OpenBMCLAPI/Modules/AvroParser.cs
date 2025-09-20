using CSharpOpenBMCLAPI.Modules.Storage;
using System.Text;

namespace CSharpOpenBMCLAPI.Modules
{
    public class AvroParser(byte[] data)
    {
        private MemoryStream _stream = new(data);
        private List<ApiFileInfo> _files = new();
        private byte[] data;

        public List<ApiFileInfo> Parse()
        {
            this._files.Clear();
            long elements = this.ReadLong();
            for (long i = 0; i < elements; i++)
            {
                string path = this.ReadString();
                string hash = this.ReadString();
                long size = this.ReadLong();
                long time = this.ReadLong();
                ApiFileInfo file = new ApiFileInfo
                {
                    path = path,
                    hash = hash,
                    size = size,
                    mtime = time
                };
                this._files.Add(file);
            }
            this._stream.ReadByte();
            return this._files;
        }

        public long ReadLong()
        {
            long b = this._stream.ReadByte();
            long n = b & 0x7F;
            int shift = 7;
            while ((b & 0x80) != 0)
            {
                b = this._stream.ReadByte();
                n |= (b & 0x7F) << shift;
                shift += 7;
            }
            return (n >> 1) ^ -(n & 1);
        }

        public string ReadString()
        {
            byte[] buffer = new byte[this.ReadLong()];
            this._stream.ReadExactly(buffer, 0, buffer.Length);
            return Encoding.UTF8.GetString(buffer);
        }
    }

}
