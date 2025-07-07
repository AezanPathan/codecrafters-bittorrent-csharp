using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CodeCrafters.Bittorrent.src;
using System.Net;
using System.Net.Sockets;

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
    string[] addressParts = args[2].Split(':');
    if (addressParts.Length != 2)
    {
        Console.WriteLine("Invalid peer address. Use format <ip>:<port>");
        return;
    }
    var client = new TrackerClient();
    var peerIp = IPAddress.Parse(addressParts[0]);
    var peerPort = int.Parse(addressParts[1]);

    // Parse the .torrent file
    var content = File.ReadAllBytes(args[1]);
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

else if (command == "download_piece")
{
    string outputPath = args[2];
    string torrentFile = args[3];
    int pieceIndex = int.Parse(args[4]);

    // Step 1: Parse .torrent file
    var content = File.ReadAllBytes(torrentFile);
    var bdecode = new BencodeDecoder();
    (object result, _) = bdecode.DecodeInput(content, 0);
    var meta = (Dictionary<string, object>)result;
    var infoDict = (Dictionary<string, object>)meta["info"];

    const string marker = "4:infod";
    int markerPosition = BencodeUtils.FindMarkerPosition(content, marker);
    int infoStartIndex = markerPosition + marker.Length - 1;
    byte[] infoBytes = content[infoStartIndex..^1];
    byte[] infoHash = SHA1.HashData(infoBytes);

    string tracker = (string)meta["announce"];
    long totalLength = (long)infoDict["length"];
    //int pieceLength = (int)(long)infoDict["piece length"];
    var pieceLength = Convert.ToInt32((long)infoDict["piece length"]);

    byte[] piecesRaw = Encoding.ASCII.GetBytes((string)infoDict["pieces"]);

    int numberOfPieces = piecesRaw.Length / 20;
    if (pieceIndex < 0 || pieceIndex >= numberOfPieces)
        throw new ArgumentOutOfRangeException(nameof(pieceIndex), $"Invalid piece index {pieceIndex}. Only {numberOfPieces} pieces available.");

    // Get expected piece hash
    byte[] expectedHash = piecesRaw[(pieceIndex * 20)..((pieceIndex + 1) * 20)];

    // Step 2: Generate peer ID
    var peerIdBytes = new byte[20];
    RandomNumberGenerator.Fill(peerIdBytes);

    // Step 3: Ask tracker for peers
    var trackerRequest = new TrackerRequest
    {
        TrackerUrl = new Uri(tracker),
        InfoHash = infoHash,
        PeerId = Encoding.ASCII.GetString(peerIdBytes),
        Port = 6881,
        Uploaded = 0,
        Downloaded = 0,
        Left = totalLength,
        Compact = true
    };

    var client = new TrackerClient();
    var peers = await client.GetPeersAsync(trackerRequest);
    var (ip, port) = peers.First();

    using var tcpClient = new TcpClient();
    await tcpClient.ConnectAsync(ip, port);
    using var stream = tcpClient.GetStream();

    // Step 4: Send handshake
    await stream.WriteAsync(TrackerClient.BuildHandshake(infoHash, peerIdBytes));

    // Step 5: Read handshake response (ignore content)
    byte[] response = new byte[68];
    await stream.ReadExactlyAsync(response);

    // Step 6: Wait for bitfield (ID 5)
    var (msg1, _) = await TrackerClient.ReadMessage(stream);
    if (msg1 != 5) throw new Exception("Expected bitfield");

    // Step 7: Send interested (ID 2)
    await stream.WriteAsync(TrackerClient.BuildMessage(2));

    // Step 8: Wait for unchoke (ID 1)
    var (msg2, _) = await TrackerClient.ReadMessage(stream);
    if (msg2 != 1) throw new Exception("Expected unchoke");

    // Step 9: Download the piece
    int actualPieceLength = Math.Min(pieceLength, (int)(totalLength - (long)pieceIndex * pieceLength));
    byte[] pieceData = await client.DownloadPiece(stream, pieceIndex, actualPieceLength);

    // Step 10: Validate hash
    byte[] actualHash = SHA1.HashData(pieceData);
    if (!expectedHash.SequenceEqual(actualHash))
        throw new Exception("Piece hash mismatch");

    // Step 11: Save to file
    await File.WriteAllBytesAsync(outputPath, pieceData);
}


else
{
    throw new InvalidOperationException($"Invalid command: {command}");
}
