namespace CodeCrafters.Bittorrent.src;

public class Decoder
{

    public (object Value, int Consumed) DecodeInput(string input)
    {
        if (string.IsNullOrEmpty(input))
            throw new ArgumentException("Nothing to decode", nameof(input));

        if (Char.IsDigit(input[0]))
            return DecodeString(input);

        else if (input[0] == 'i')
            return DecodeInteger(input);

        else if (input[0] == 'l')
            return DecodeList(input);

        else if (input[0] == 'd')
            return DecodeDictionary(input);

        else
            throw new InvalidOperationException($"Unknown bencode type '{input[0]}'");
    }

    private (string, int) DecodeString(string stringToDecode)
    {
        var colonIndex = stringToDecode.IndexOf(':');
        if (colonIndex != -1)
        {
            var strLength = int.Parse(stringToDecode[..colonIndex]);
            var strValue = stringToDecode.Substring(colonIndex + 1, strLength);
            return (strValue, colonIndex + 1 + strLength);
        }
        else
            throw new InvalidOperationException("Invalid encoded value: " + stringToDecode);
    }

    private (long, int) DecodeInteger(string stringToDecode)
    {
        int end = stringToDecode.IndexOf('e');
        if (end < 0)
            throw new FormatException("Invalid integer, missing 'e' terminator: " + stringToDecode);

        // everything between 'i' (at 0) and 'e' (at end)
        string numStr = stringToDecode.Substring(1, end - 1);

        // consumed length is up through and including that 'e'
        return (long.Parse(numStr), end + 1);
    }

    private (Dictionary<string, object>, int) DecodeDictionary(string stringToDecode)
    {
        var dict = new Dictionary<string, object>();
        var offset = 1; // skip 'd'

        while (offset < stringToDecode.Length && stringToDecode[offset] != 'e')
        {
            // Decode key (must be string)
            var (key, keyUsed) = DecodeString(stringToDecode.Substring(offset));
            offset += keyUsed;

            // Decode value
            var (value, valueUsed) = DecodeInput(stringToDecode.Substring(offset));
            dict.Add(key, value);
            offset += valueUsed;
        }

        if (offset >= stringToDecode.Length || stringToDecode[offset] != 'e')
            throw new FormatException("Unterminated dictionary");

        return (dict, offset + 1);
    }

    private (List<object>, int) DecodeList(string s)
    {
        // “l <elements> e”
        var list = new List<object>();
        int offset = 1; // skip ‘l’

        while (offset < s.Length && s[offset] != 'e')
        {
            var (elem, used) = DecodeInput(s[offset..]);
            list.Add(elem);
            offset += used;
        }

        if (offset >= s.Length || s[offset] != 'e')
            throw new FormatException("Unterminated list");

        return (list, offset + 1);
    }
}
