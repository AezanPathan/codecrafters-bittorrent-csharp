using System.Net;
using System.Text;
using System.Web;
using System.Net.Sockets;


namespace CodeCrafters.Bittorrent.src;

public record TrackerRequest
{
    public Uri TrackerUrl { get; set; }
    public byte[] InfoHash { get; set; }
    public string PeerId { get; set; }
    public int Port { get; set; }
    public long Uploaded { get; set; }
    public long Downloaded { get; set; }
    public long Left { get; set; }
    public bool Compact { get; set; }
}

public class TrackerClient
{
    private readonly HttpClient _http = new HttpClient();
    private readonly BencodeDecoder _bencodeDecoder = new BencodeDecoder();
    private readonly BencodeDecoder _trackerResponseDecoder = new BencodeDecoder();

    public async Task<IEnumerable<(IPAddress ip, int port)>> GetPeersAsync(TrackerRequest trackerRequest)
    {
        // Build query string manually to avoid double-encoding
        var infoHashStr = string.Concat(trackerRequest.InfoHash.Select(b => $"%{b:X2}"));
        var peerIdStr = Uri.EscapeDataString(trackerRequest.PeerId);

        var queryString = $"info_hash={infoHashStr}&peer_id={peerIdStr}&port={trackerRequest.Port}&uploaded={trackerRequest.Uploaded}&downloaded={trackerRequest.Downloaded}&left={trackerRequest.Left}&compact={(trackerRequest.Compact ? "1" : "0")}";

        var uri = new UriBuilder(trackerRequest.TrackerUrl) { Query = queryString }.Uri;

        var respBytes = await _http.GetByteArrayAsync(uri);

        // Parse the response manually to extract peers as raw bytes
        var peers = ParsePeersFromTrackerResponse(respBytes);
        return peers;
    }

    private List<(IPAddress, int)> ParsePeersFromTrackerResponse(byte[] responseBytes)
    {
        // Find the "peers" field and extract its raw byte value
        var peersMarker = Encoding.ASCII.GetBytes("5:peers");
        int peersPos = BencodeUtils.FindMarkerPosition(responseBytes, "5:peers");

        // Find the length of the peers data
        int lengthStart = peersPos + peersMarker.Length;
        int colonPos = Array.IndexOf(responseBytes, (byte)':', lengthStart);
        string lengthStr = Encoding.ASCII.GetString(responseBytes[lengthStart..colonPos]);
        if (!int.TryParse(lengthStr, out int peersLength))
            throw new InvalidOperationException($"Invalid peers length: {lengthStr}");

        // Extract the raw peers bytes
        int dataStart = colonPos + 1;
        byte[] peersBytes = responseBytes[dataStart..(dataStart + peersLength)];

        // Parse compact peer format
        var peers = new List<(IPAddress, int)>();
        for (int i = 0; i < peersBytes.Length; i += 6)
        {
            if (i + 5 < peersBytes.Length)
            {
                var ipBytes = peersBytes[i..(i + 4)];
                var portBytes = peersBytes[(i + 4)..(i + 6)];
                var ip = new IPAddress(ipBytes);
                var port = (portBytes[0] << 8) | portBytes[1];
                peers.Add((ip, port));
            }
        }

        return peers;
    }

    ///Builds TCP Handshake logic

    public static byte[] BuildHandshake(byte[] infoHash, byte[] peerId)
    {
        using var stream = new MemoryStream();
        stream.WriteByte(19); // length of protocol string
        stream.Write(Encoding.ASCII.GetBytes("BitTorrent protocol"));
        stream.Write(new byte[8]); 
        stream.Write(infoHash);    // 20 bytes
        stream.Write(peerId);      // 20 bytes
        return stream.ToArray();
    }

    public async Task<string> PerformHandshake(IPAddress ip, int port, byte[] infoHash, byte[] peerId)
    {
        using var client = new TcpClient();
        await client.ConnectAsync(ip, port);

        using var stream = client.GetStream();

        var handshake = BuildHandshake(infoHash, peerId);
        await stream.WriteAsync(handshake);

        var response = new byte[68];
        await stream.ReadAsync(response);

        byte[] receivedPeerId = response[48..68];
        return BitConverter.ToString(receivedPeerId).Replace("-", "").ToLower();
    }

    #region peer messages

    public static byte[] BuildMessage(byte messageId, byte[] payload = null)
    {
        payload ??= Array.Empty<byte>();
        int messageLength = 1 + payload.Length;

        using var ms = new MemoryStream();
        ms.Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(messageLength)));
        ms.WriteByte(messageId);
        ms.Write(payload);
        return ms.ToArray();
    }

    public static async Task<(byte messageId, byte[] payload)> ReadMessage(NetworkStream stream)
    {
        byte[] lengthBuffer = new byte[4];
        await stream.ReadExactlyAsync(lengthBuffer);
        int messageLength = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(lengthBuffer));

        if (messageLength == 0)
            return (255, Array.Empty<byte>()); // Keep-alive

        byte[] messageBuffer = new byte[messageLength];
        await stream.ReadExactlyAsync(messageBuffer);

        byte messageId = messageBuffer[0];
        byte[] payload = messageBuffer[1..];
        return (messageId, payload);
    }


    #endregion

    public async Task<byte[]> DownloadPiece(NetworkStream stream, int pieceIndex, int pieceLength)
    {
        const int blockSize = 16384;
        int totalBlocks = (int)Math.Ceiling(pieceLength / (double)blockSize);

        byte[] pieceBuffer = new byte[pieceLength];

        for (int i = 0; i < totalBlocks; i++)
        {
            int begin = i * blockSize;
            int blockLength = Math.Min(blockSize, pieceLength - begin);

            using var ms = new MemoryStream();
            ms.Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(pieceIndex)));
            ms.Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(begin)));
            ms.Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(blockLength)));

            var payload = ms.ToArray();

            await stream.WriteAsync(BuildMessage(6, payload));

            while (true)
            {
                var (messageId, payloadReceived) = await ReadMessage(stream);

                if (messageId != 7)
                    continue;

                // Parse payload
                int receivedIndex = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(payloadReceived, 0));
                int receivedBegin = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(payloadReceived, 4));

                if (receivedIndex != pieceIndex || receivedBegin != begin)
                    continue;

                // Copy block data
                Array.Copy(payloadReceived, 8, pieceBuffer, begin, blockLength);
                break;
            }
        }

        return pieceBuffer;
    }


}
