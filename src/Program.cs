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
    var content = File.ReadAllBytes(param);
    (object result, _) = Bencodedecoder.DecodeInput(content, 0);

    var meta = (Dictionary<string, object>)result;
    var infoDict = (Dictionary<string, object>)meta["info"];

    var announceBytes = (byte[])meta["announce"];
    string tracker = Encoding.UTF8.GetString(announceBytes);
    var length = (long)infoDict["length"];

    // Encode the info dictionary
    byte[] infoBytes = BencodeEncoder.Encode(infoDict);

    // Compute SHA-1 hash
    using (SHA1 sha1 = SHA1.Create())
    {
        byte[] hashBytes = sha1.ComputeHash(infoBytes);
        string infoHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        Console.WriteLine($"Tracker URL: {tracker}");
        Console.WriteLine($"Length: {length}");
        Console.WriteLine($"Info Hash: {infoHash}");
    }
}
else
{
    throw new InvalidOperationException($"Invalid command: {command}");
}
