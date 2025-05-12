namespace CodeCrafters.Bittorrent.src;

public class Decoder
{

    public (object Value, int Consumed) DecodeInput(string input)
    {
        if (string.IsNullOrEmpty(input))
            throw new ArgumentException("Nothing to decode", nameof(input));

        if (Char.IsDigit(input[0])) //if string 
            return DecodeString(input);

        else if (input[0] == 'i') 
            return DecodeInteger(input);

        else if (input[0] == 'l')
            return DecodeList(input);
        else
            throw new InvalidOperationException($"Unknown bencode type '{input[0]}'");

        // return input[0] switch
        // {

        //     var c when char.IsDigit(c) => DecodeString(input),
        //     'i' => DecodeInteger(input),
        //     'l' => DecodeList(input),
        //     _ => throw new InvalidOperationException($"Unknown bencode type '{input[0]}'")
        // };
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
        // Console.WriteLine(stringToDecode.Substring(1, stringToDecode.Length - 2)

        // “i<digits>e”
        // int end = stringToDecode.IndexOf('e');
        // if (end < 0) throw new FormatException("Missing 'e' in integer token");

        string num = stringToDecode.Substring(1, stringToDecode.Length - 2);
        return (long.Parse(num), stringToDecode.Length + 1);
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

    ///public enum DecodeTypes { Unknown, String, Integer, List }

    // private (string, int) DecodeString(string stringToDecode)
    // {
    //     int colonIndex = stringToDecode.IndexOf(':');
    //     if (colonIndex != -1)
    //     {
    //         int strLength = int.Parse(stringToDecode[..colonIndex]);
    //         string strValue = stringToDecode.Substring(colonIndex + 1, strLength);
    //         return (strValue, strLength + 1 + colonIndex);
    //     }
    //     else
    //     {
    //         throw new InvalidOperationException("Invalid encoded value: " +
    //                                             stringToDecode);
    //     }
    // }
    // private (long, int) DecodeInteger(string stringToDecode)
    // {
    //     int colonIndex = stringToDecode.IndexOf('e');
    //     if (colonIndex != -1)
    //     {
    //         string strValue = stringToDecode.Substring(1, colonIndex - 1);
    //         return (long.Parse(strValue), strValue.Length + 2);
    //     }
    //     else
    //     {
    //         throw new InvalidOperationException("Invalid encoded value: " +
    //                                             stringToDecode);
    //     }
    // }
    // private (List<object>, int) DecodeList(string stringToDecode)
    // {
    //     List<object> result = [];
    //     int offset = 1;
    //     while (offset < stringToDecode.Length)
    //     {
    //         if (stringToDecode[offset] == 'e')
    //             break;
    //         (object element, int elementOffset) =
    //             DecodeInput(stringToDecode[offset..]);
    //         result.Add(element);
    //         offset += elementOffset;
    //     }
    //     return (result, offset + 1);
    // }

    // public DecodeTypes GetDecodeType(char firstSymbol)
    // {
    //     return firstSymbol switch
    //     {
    //         _ when char.IsDigit(firstSymbol) => DecodeTypes.String,
    //         'i' => DecodeTypes.Integer,
    //         'l' => DecodeTypes.List,
    //         _ => DecodeTypes.Unknown,
    //     };
    // }
    // public (object, int) DecodeInput(string stringToDecode)
    // {
    //     return GetDecodeType(stringToDecode[0]) switch
    //     {
    //         DecodeTypes.String => DecodeString(stringToDecode),
    //         DecodeTypes.Integer => DecodeInteger(stringToDecode),
    //         DecodeTypes.List => DecodeList(stringToDecode),
    //         _ or DecodeTypes.Unknown => throw new InvalidOperationException(
    //             "Unhandled encoded value: " + stringToDecode),
    //     };
    // }

    /*
    private (List<object>, int) DecodeList(string stringToDecode)
        {
            List<object> result = [];
            int offset = 1;
            while (offset < stringToDecode.Length)
            {
                if (stringToDecode[offset] == 'e')
                    break;
                (object element, int elementOffset) =
                    DecodeInput(stringToDecode[offset..]);
                result.Add(element);
                offset += elementOffset;
            }
            return (result, offset + 1);
        }

    */
}

