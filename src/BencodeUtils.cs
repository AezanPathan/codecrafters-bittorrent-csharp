using System.Text;

namespace CodeCrafters.Bittorrent.src;

public class BencodeUtils
{
    public static int FindMarkerPosition(byte[] data, string marker)
    {
        byte[] markerBytes = Encoding.ASCII.GetBytes(marker);
        for (int i = 0; i <= data.Length - markerBytes.Length; i++)
        {
            if (data[i..(i + markerBytes.Length)].SequenceEqual(markerBytes))
                return i;
        }
        throw new KeyNotFoundException("Marker not found");
    }

    public static int FindMatchingEnd(byte[] data, int startIndex)
    {
        int depth = 1;
        int i = startIndex + 1; // Start after 'd'

        while (i < data.Length && depth > 0)
        {
            byte current = data[i];
            if (current == 'd' || current == 'l')
            {
                depth++;
                i++;
            }
            else if (current == 'e')
            {
                depth--;
                i++;
                if (depth == 0)
                    return i - 1; // Position of closing 'e'
            }
            else if (char.IsDigit((char)current))
            {
                // Skip over string (e.g., "3:foo")
                int colon = Array.IndexOf(data, (byte)':', i);
                if (colon == -1) break;
                int length = int.Parse(Encoding.ASCII.GetString(data, i, colon - i));
                i = colon + 1 + length; // Move past the string
            }
            else
            {
                i++;
            }
        }

        if (depth != 0)
            throw new FormatException("Unclosed structure");

        return i - 1;
    }
}