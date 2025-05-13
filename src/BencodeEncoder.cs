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
                EncodeString(key, stream);
                EncodeObject(dict[key], stream);
            }

            stream.WriteByte((byte)'e');
        }

        private static void EncodeObject(object obj, MemoryStream stream)
        {
            switch (obj)
            {
                case string s:
                    EncodeString(s, stream);
                    break;
                case long l:
                    EncodeInteger(l, stream);
                    break;
                case Dictionary<string, object> d:
                    EncodeDictionary(d, stream);
                    break;
                case List<object> list:
                    EncodeList(list, stream);
                    break;
                default:
                    throw new NotSupportedException($"Unsupported type: {obj.GetType()}");
            }
        }

        private static void EncodeString(string s, MemoryStream stream)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(s);
            byte[] length = Encoding.ASCII.GetBytes($"{bytes.Length}:");
            stream.Write(length, 0, length.Length);
            stream.Write(bytes, 0, bytes.Length);
        }

        private static void EncodeInteger(long value, MemoryStream stream)
        {
            byte[] bytes = Encoding.ASCII.GetBytes($"i{value}e");
            stream.Write(bytes, 0, bytes.Length);
        }

        private static void EncodeList(List<object> list, MemoryStream stream)
        {
            stream.WriteByte((byte)'l');
            foreach (var item in list)
            {
                EncodeObject(item, stream);
            }
            stream.WriteByte((byte)'e');
        }
    }
}