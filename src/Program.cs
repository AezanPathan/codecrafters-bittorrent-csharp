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
    var decoder = new Decoder();
    // (object result, _) = decoder.DecodeInput(encodedValue);
    // Console.WriteLine(JsonSerializer.Serialize(result));
}
else if (command == "info")
{
    var decoder = new Decoder();
    var content = File.ReadAllBytes(param);
    (object result, _) = decoder.DecodeInput(content, 0);
    Console.WriteLine(JsonSerializer.Serialize(result));
}
else
{
    throw new InvalidOperationException($"Invalid command: {command}");
}
