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

    public async Task<IEnumerable<(IPAddress ip, int port)>> GetPeersAsync(TrackerRequest trackerRequest)
    {
        var qb = HttpUtility.ParseQueryString(string.Empty);
        qb["info_hash"] = Uri.EscapeDataString(Encoding.ASCII.GetString(trackerRequest.InfoHash));
        qb["peer_id"] = Uri.EscapeDataString(trackerRequest.PeerId);
        qb["port"] = trackerRequest.Port.ToString();
        qb["uploaded"] = trackerRequest.Uploaded.ToString();
        qb["downloaded"] = trackerRequest.Downloaded.ToString();
        qb["left"] = trackerRequest.Left.ToString();
        qb["compact"] = trackerRequest.Compact ? "1" : "0";

        var uri = new UriBuilder(trackerRequest.TrackerUrl) { Query = qb.ToString() }.Uri;

        var respBytes = await _http.GetByteArrayAsync(uri);

        var (decoded, _) = _bencodeDecoder.DecodeInput(respBytes, 0);
        var dict = (Dictionary<string, object>)decoded;
        var peersBin = (byte[])dict["peers"];

        var peers = new List<(IPAddress, int)>();
        for (int i = 0; i < peersBin.Length; i += 6)
        {
            var ipBytes = peersBin.Skip(i).Take(4).ToArray();
            var portBytes = peersBin.Skip(i + 4).Take(2).ToArray();
            var ip = new IPAddress(ipBytes);
            var port = (portBytes[0] << 8) | portBytes[1];
            peers.Add((ip, port));
        }
        return peers;
    }
}
