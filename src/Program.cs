using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CodeCrafters.Bittorrent.src;

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
    var BencodeUtils = new BencodeUtils();
    //var BencodeEncoder = new BencodeEncoder();

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

    Console.WriteLine($"Tracker URL: {tracker}");
    Console.WriteLine($"Length: {length}");
    Console.WriteLine($"Info Hash: {infoHash}");

    // string infoMarker = "4:infod";
    // int markerPosition = BencodeUtils.FindMarkerPosition(content, infoMarker);

    // int infoStartIndex = markerPosition + infoMarker.Length - 1;
    // int infoEndIndex = BencodeUtils.FindMatchingEnd(content, infoStartIndex);
    // byte[] infoBytes = content[infoStartIndex..(infoEndIndex + 1)];
    // Console.WriteLine($"Info Bytes: {infoBytes}");
    // Console.WriteLine($"Info StartIndex: {infoStartIndex}");
    // Console.WriteLine($"Info EndIndex: {infoEndIndex}");

    // using (SHA1 sha1 = SHA1.Create())
    // {
    //     byte[] hashBytes = sha1.ComputeHash(infoBytes);
    //     string infoHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    //     Console.WriteLine($"Tracker URL: {tracker}");
    //     Console.WriteLine($"Length: {length}");
    //     Console.WriteLine($"Info Hash: {infoHash}");
    // }

}
else
{
    throw new InvalidOperationException($"Invalid command: {command}");
}
