﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpOpenBMCLAPI.Modules.WebServer
{
    public static class WebUtils
    {
        public static List<byte[]> SplitBytes(byte[] data, byte[] key, int count = 0)
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
        private static byte[] SubArray(byte[] data, int start, int length)
        {
            byte[] result = new byte[length];
            Array.Copy(data, start, result, 0, length);
            return result;
        }
    }
}