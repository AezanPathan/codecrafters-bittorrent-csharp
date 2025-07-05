using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CodeCrafters.Bittorrent.src;
using System.Net;

// Parse arguments
var (command, param) = args.Length switch
{
    0 => throw new InvalidOperationException("Usage: your_program.sh <command> <param>"),
    1 => throw new InvalidOperationException("Usage: your_program.sh <command> <param>"),
    _ => (args[0], args[1])
};


// Parse command and act accordingly
if (command == "decode")
{
    // You can use print statements as follows for debugging, they'll be visible when running tests.
    Console.Error.WriteLine("Logs from your program will appear here!");

    // Uncomment this line to pass the first stage
    var encodedValue = param;
    var decoder = new CodeCrafters.Bittorrent.src.Decoder();
    (object result, _) = decoder.DecodeInput(encodedValue);
    Console.WriteLine(JsonSerializer.Serialize(result));
}
else if (command == "info")
{
    var Bencodedecoder = new BencodeDecoder();
    // var trackerClient = new TrackerClient();

    var content = File.ReadAllBytes(param);
    (object result, _) = Bencodedecoder.DecodeInput(content, 0);

    var meta = (Dictionary<string, object>)result;
    var infoDict = (Dictionary<string, object>)meta["info"];

    string tracker = (string)meta["announce"];
    long length = (long)infoDict["length"];

    const string marker = "4:infod";
    int markerPosition = BencodeUtils.FindMarkerPosition(content, marker);
    int infoStartIndex = markerPosition + marker.Length - 1;
    byte[] infoBytes = content[infoStartIndex..^1];
    byte[] hashBytes = SHA1.HashData(infoBytes);
    string infoHash = Convert.ToHexString(hashBytes).ToLower();

    long pieceLength = (long)infoDict["piece length"];

    const string piecesKey = "6:pieces";
    int piecesKeyPos = BencodeUtils.FindMarkerPosition(infoBytes, piecesKey);
    int lenStart = piecesKeyPos + piecesKey.Length;
    int colonPos = Array.IndexOf(infoBytes, (byte)':', lenStart);
    string lenStr = Encoding.ASCII.GetString(infoBytes[lenStart..colonPos]);
    if (!int.TryParse(lenStr, out int piecesLen))
        throw new InvalidOperationException($"Invalid pieces length: {lenStr}");
    int dataStart = colonPos + 1;
    byte[] piecesBytes = infoBytes[dataStart..(dataStart + piecesLen)];

    List<string> pieceHashes = BencodeUtils.ExtractPieceHashes(piecesBytes);

    Console.WriteLine($"Tracker URL: {tracker}");
    Console.WriteLine($"Length: {length}");
    Console.WriteLine($"Info Hash: {infoHash}");
    Console.WriteLine($"Piece Length: {pieceLength}");
    Console.WriteLine("Piece Hashes:");
    foreach (var h in pieceHashes) Console.WriteLine(h);

    // Temporarily comment out tracker call to debug info hash
    /*
    var trackerRequest = new TrackerRequest
    {
        TrackerUrl = new Uri(tracker),
        InfoHash = hashBytes,
        PeerId = "ABCDEFGHIJKLMNO00000",
        Port = 6881,
        Uploaded = 0,
        Downloaded = 0,
        Left = length,
        Compact = true
    };

    var client = new TrackerClient();
    var peers = await client.GetPeersAsync(trackerRequest);
    foreach (var (ip, port) in peers)
        Console.WriteLine($"{ip}:{port}");

    Console.WriteLine($"Tracker URL: {tracker}");
    Console.WriteLine($"Length: {length}");
    Console.WriteLine($"Info Hash: {infoHash}");
    Console.WriteLine($"Piece Length: {pieceLength}");
    Console.WriteLine("Piece Hashes:");
    foreach (var h in pieceHashes) Console.WriteLine(h);
    */

}
else if (command == "peers")
{
    var Bencodedecoder = new BencodeDecoder();
    // var trackerClient = new TrackerClient();

    var content = File.ReadAllBytes(param);
    (object result, _) = Bencodedecoder.DecodeInput(content, 0);

    var meta = (Dictionary<string, object>)result;
    var infoDict = (Dictionary<string, object>)meta["info"];

    string tracker = (string)meta["announce"];
    long length = (long)infoDict["length"];

    const string marker = "4:infod";
    int markerPosition = BencodeUtils.FindMarkerPosition(content, marker);
    int infoStartIndex = markerPosition + marker.Length - 1;
    byte[] infoBytes = content[infoStartIndex..^1];
    byte[] hashBytes = SHA1.HashData(infoBytes);
    string infoHash = Convert.ToHexString(hashBytes).ToLower();

    long pieceLength = (long)infoDict["piece length"];

    const string piecesKey = "6:pieces";
    int piecesKeyPos = BencodeUtils.FindMarkerPosition(infoBytes, piecesKey);
    int lenStart = piecesKeyPos + piecesKey.Length;
    int colonPos = Array.IndexOf(infoBytes, (byte)':', lenStart);
    string lenStr = Encoding.ASCII.GetString(infoBytes[lenStart..colonPos]);
    if (!int.TryParse(lenStr, out int piecesLen))
        throw new InvalidOperationException($"Invalid pieces length: {lenStr}");
    int dataStart = colonPos + 1;
    byte[] piecesBytes = infoBytes[dataStart..(dataStart + piecesLen)];

    List<string> pieceHashes = BencodeUtils.ExtractPieceHashes(piecesBytes);

    var peerIdBytes = new byte[20];
    RandomNumberGenerator.Fill(peerIdBytes);

    var trackerRequest = new TrackerRequest
    {
        TrackerUrl = new Uri(tracker),
        InfoHash = hashBytes,
        PeerId = Encoding.ASCII.GetString(peerIdBytes),
        Port = 6881,
        Uploaded = 0,
        Downloaded = 0,
        Left = length,
        Compact = true
    };

    var client = new TrackerClient();
    var peers = await client.GetPeersAsync(trackerRequest);
    foreach (var (ip, port) in peers)
        Console.WriteLine($"{ip}:{port}");
}

else if (command == "handshake")
{
    // args[3] should be in the format <ip>:<port>
    string[] addressParts = args[3].Split(':');
    if (addressParts.Length != 2)
    {
        Console.WriteLine("Invalid peer address. Use format <ip>:<port>");
        return;
    }
    var client = new TrackerClient();
    var peerIp = IPAddress.Parse(addressParts[0]);
    var peerPort = int.Parse(addressParts[1]);

    // Parse the .torrent file
    var content = File.ReadAllBytes(param);
    var bdecode = new BencodeDecoder();
    (object result, _) = bdecode.DecodeInput(content, 0);
    var meta = (Dictionary<string, object>)result;
    var infoDict = (Dictionary<string, object>)meta["info"];

    const string marker = "4:infod";
    int markerPosition = BencodeUtils.FindMarkerPosition(content, marker);
    int infoStartIndex = markerPosition + marker.Length - 1;
    byte[] infoBytes = content[infoStartIndex..^1];

    byte[] infoHash = SHA1.HashData(infoBytes);

    // Generate random peer ID (20 bytes)
    var peerIdBytes = new byte[20];
    RandomNumberGenerator.Fill(peerIdBytes);

    // Perform handshake
    var receivedPeerId = await client.PerformHandshake(peerIp, peerPort, infoHash, peerIdBytes);

    // Output received peer ID
    Console.WriteLine($"Peer ID: {receivedPeerId}");
}
else
{
    throw new InvalidOperationException($"Invalid command: {command}");
}
