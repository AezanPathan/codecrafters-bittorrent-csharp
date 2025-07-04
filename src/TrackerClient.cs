using System.Net;
using System.Text;
using System.Web;

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
}
