using System.Text;

namespace CodeCrafters.Bittorrent.src;

public class BencodeUtils
{
    public static int FindMarkerPosition(byte[] data, string marker)
    {
        byte[] markerBytes = Encoding.ASCII.GetBytes(marker);
        for (int i = 0; i < data.Length - markerBytes.Length; i++)
        {
            if (data[i..(i + markerBytes.Length)].SequenceEqual(markerBytes)) return i;

        }
        throw new KeyNotFoundException("Marker not found");
    }

    public static int FindMatchingEnd(byte[] data, int start)
    {
        int depth = 1;
        for (int i = start + 1; i < data.Length; i++)
        {
            switch ((char)data[i])
            {
                case 'd':
                case 'l':
                    depth++;
                    break;
                case 'e':
                    depth--;
                    if (depth == 0) return i;
                    break;
            }
        }
        throw new FormatException("Unterminated dictionary");
    }
}