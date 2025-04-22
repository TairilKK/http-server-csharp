using System.Text;
using System.IO.Compression;
enum ResponseCodes{
    OK, CREATED, NOT_FOUND,
    FORBIDDEN,
    INTERNAL_SERVER_ERROR
}

class ResponseBuilder {
    string? Status;
    string? Header;
    string? Body;
    public override string ToString(){
        return Body is null ? $"HTTP/1.1 {Status}\r\n{Header}\r\n\r\n" : $"HTTP/1.1 {Status}\r\n{Header}\r\n{Body}\r\n";
    }

    public byte[] Create()
    {
        Console.WriteLine("###");
        Console.WriteLine(this);
        Console.WriteLine("###");
        return Encoding.UTF8.GetBytes(ToString());
    }

    public static string CompresString(string Text)
    {
        byte[] buffer = Encoding.UTF8.GetBytes(Text);
        var memoryStream = new MemoryStream();
        using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
        {
            gZipStream.Write(buffer, 0, buffer.Length);
        }

        memoryStream.Position = 0;

        var compressedData = new byte[memoryStream.Length];
        memoryStream.Read(compressedData, 0, compressedData.Length);

        var gZipBuffer = new byte[compressedData.Length + 4];
        Buffer.BlockCopy(compressedData, 0, gZipBuffer, 4, compressedData.Length);
        Buffer.BlockCopy(BitConverter.GetBytes(buffer.Length), 0, gZipBuffer, 0, 4);
        return Convert.ToBase64String(gZipBuffer);
    }

    public static string DecompressString(string Text)
    {
        byte[] gZipBuffer = Convert.FromBase64String(Text);
        using (var memoryStream = new MemoryStream())
        {
            int dataLength = BitConverter.ToInt32(gZipBuffer, 0);
            memoryStream.Write(gZipBuffer, 4, gZipBuffer.Length - 4);

            var buffer = new byte[dataLength];

            memoryStream.Position = 0;
            using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
            {
                gZipStream.Read(buffer, 0, buffer.Length);
            }
            return Encoding.UTF8.GetString(buffer);
        }
    }

    public void SetStatus(int V)
    {
        switch (V)
        {
            case 200:
                Status="200 OK";
                break;
            case 201:
                Status="201 Created";
                break;
            case 404:
                Status="404 Not Found";
                break;
            default:
                break;
        };
    }
    public void SetStatus(ResponseCodes Code)
    {
        switch (Code)
        {
            case ResponseCodes.OK:
                Status="200 OK";
                break;
            case ResponseCodes.CREATED:
                Status="201 Created";
                break;
            case ResponseCodes.NOT_FOUND:
                Status="404 Not Found";
                break;
            default:
                break;
        };
    }
    public void SetHeader(string Header){
        this.Header = Header;
    }
    public void SetBody(string Body){
        this.Body = Body;
    }
}
