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

    public static List<string> ExtractPieceHashes(byte[] piecesBytes)
    {
        const int HASH_SIZE = 20;
        int count = piecesBytes.Length / HASH_SIZE;
        var pieceHashes = new List<string>(count);

        for (int i = 0; i < piecesBytes.Length; i += HASH_SIZE)
        {
            byte[] singleHash = new byte[HASH_SIZE];
            Array.Copy(piecesBytes, i, singleHash, 0, HASH_SIZE);
            pieceHashes.Add(Convert.ToHexString(singleHash).ToLower());
        }

        return pieceHashes;
    }

}