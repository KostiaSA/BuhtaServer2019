using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using Snappy.Sharp;
using System.Text;

namespace BuhtaServer
{
    public static class Utils
    {
        public static bool IsPropertyExist(dynamic settings, string name)
        {
            if (settings is ExpandoObject)
                return ((IDictionary<string, object>)settings).ContainsKey(name);

            return settings.GetType().GetProperty(name) != null;
        }


        public static string CompressToBase64Str(string str)
        {
            var data = Encoding.Default.GetBytes(str);
            var snappy = new SnappyCompressor();

            int compressedSize = snappy.MaxCompressedLength(data.Length);
            var compressed = new byte[compressedSize];

            int result = snappy.Compress(data, 0, data.Length, compressed);

            return Convert.ToBase64String(compressed.Take(result).ToArray());

        }

        public static Byte[] CompressToByteArray(string str)
        {
            var data = Encoding.Default.GetBytes(str);
            var snappy = new SnappyCompressor();

            int compressedSize = snappy.MaxCompressedLength(data.Length);
            var compressed = new byte[compressedSize];

            int result = snappy.Compress(data, 0, data.Length, compressed);

            return compressed.Take(result).ToArray();

        }
    }
}
