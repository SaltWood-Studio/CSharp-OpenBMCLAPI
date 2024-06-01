using System.Text;

namespace CSharpOpenBMCLAPI.Modules.WebServer
{
    public static class WebUtils
    {
        public static List<byte[]> SplitBytes(this byte[] data, byte[] key, int count = -1)
        {
            if (count <= -1)
            {
                count = 0;
            }
            else
            {
                count++;
            }

            int dataLength = data.Length;
            int keyLength = key.Length;
            List<byte[]> splittedData = new List<byte[]>();
            int start = 0;
            int currentCount = 0;

            for (int i = 0; i < dataLength; i++)
            {
                if (data[i] == key[0] && i + keyLength <= dataLength)
                {
                    bool checkedAllBytes = true;
                    for (int j = 1; j < keyLength; j++)
                    {
                        if (data[i + j] != key[j])
                        {
                            checkedAllBytes = false;
                            break;
                        }
                    }

                    if (checkedAllBytes)
                    {
                        splittedData.Add(SubArray(data, start, i - start));
                        currentCount++;
                        start = i + keyLength;
                    }
                }

                if (count != 0 && currentCount >= count)
                {
                    break;
                }
            }

            if (count == 0 || currentCount < count)
            {
                splittedData.Add(SubArray(data, start, dataLength - start));
            }

            return splittedData;
        }

        // Helper method to create a subarray from an existing array  
        public static byte[] SubArray(this byte[] data, int start, int length)
        {
            byte[] result = new byte[length];
            Array.Copy(data, start, result, 0, length);
            return result;
        }

        public static object[] SubArray(this object[] data, int start, int length)
        {
            object[] result = new object[length];
            Array.Copy(data, start, result, 0, length);
            return result;
        }

        public static string Decode(this byte[] data)
        {
            return Encoding.UTF8.GetString(data);
        }

        public static byte[] Encode(this string data)
        {
            return Encoding.UTF8.GetBytes(data).ToArray();
        }
    }
}
