using System.Text;

namespace CodeCrafters.Bittorrent.src
{
    public static class BencodeEncoder
    {
        public static byte[] Encode(Dictionary<string, object> data)
        {
            using var stream = new MemoryStream();
            EncodeDictionary(data, stream);
            return stream.ToArray();
        }

        private static void EncodeDictionary(Dictionary<string, object> dict, MemoryStream stream)
        {
            stream.WriteByte((byte)'d');

            foreach (var key in dict.Keys.OrderBy(k => k, StringComparer.Ordinal))
            {
                EncodeBytes(Encoding.ASCII.GetBytes(key), stream);
                EncodeObject(dict[key], stream);
            }

            stream.WriteByte((byte)'e');
        }

        private static void EncodeObject(object obj, MemoryStream stream)
        {
            switch (obj)
            {
                case byte[] raw:
                    EncodeBytes(raw, stream);
                    break;

                case string s:
                    EncodeBytes(Encoding.ASCII.GetBytes(s), stream);
                    break;

                case long l:
                    EncodeInteger(l, stream);
                    break;

                case List<object> list:
                    EncodeList(list, stream);
                    break;

                case Dictionary<string, object> d:
                    EncodeDictionary(d, stream);
                    break;

                default:
                    throw new NotSupportedException($"Type {obj.GetType()} not supported");
            }
        }

        private static void EncodeBytes(byte[] bytes, MemoryStream stream)
        {
            var prefix = Encoding.ASCII.GetBytes($"{bytes.Length}:");
            stream.Write(prefix, 0, prefix.Length);
            stream.Write(bytes, 0, bytes.Length);
        }

        private static void EncodeInteger(long value, MemoryStream stream)
        {
            var repr = Encoding.ASCII.GetBytes($"i{value}e");
            stream.Write(repr, 0, repr.Length);
        }

        private static void EncodeList(List<object> list, MemoryStream stream)
        {
            stream.WriteByte((byte)'l');
            foreach (var item in list)
                EncodeObject(item, stream);
            stream.WriteByte((byte)'e');
        }
    }

}