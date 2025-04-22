using System.Text;
enum ResponseCodes
{
    OK, CREATED, NOT_FOUND,
    FORBIDDEN,
    INTERNAL_SERVER_ERROR
}

class ResponseBuilder
{
    string? Status;
    string? Header;
    string? Body;
    bool CompressGzip = false;
    public override string ToString()
    {
        return Body is null ? $"HTTP/1.1 {Status}\r\n{Header}\r\n\r\n" : $"HTTP/1.1 {Status}\r\n{Header}\r\n{Body}\r\n";
    }
    public byte[] Build()
    {
        if (CompressGzip && Body is not null && Header is not null){
            if(!Header.Contains("Content-Encoding: gzip\r\n"))
            {
                Header += "Content-Encoding: gzip\r\n";
            }
            var compressed = Gzip.CompressWithGzip(Body);
            Console.WriteLine(Header);
            var compressedResponse =
        $"HTTP/1.1 {Status}\r\nContent-Type: text/plain\r\nContent-Encoding: gzip\r\nContent-Length: {compressed.Length}\r\n\r\n";
    return [..Encoding.UTF8.GetBytes(compressedResponse), .. Gzip.CompressWithGzip(Body)];
        }
        else if (Header is not null)
        {
            Header.Replace("Content-Encoding: gzip\r\n", "");
        }
        Console.WriteLine("### START ###");
        Console.WriteLine(this);
        Console.WriteLine("### END ###");
        return Encoding.UTF8.GetBytes(ToString());
    }

    public void SetStatus(int V)
    {
        switch (V)
        {
            case 200:
                Status = "200 OK";
                break;
            case 201:
                Status = "201 Created";
                break;
            case 404:
                Status = "404 Not Found";
                break;
            default:
                break;
        }
        ;
    }
    public void SetStatus(ResponseCodes Code)
    {
        switch (Code)
        {
            case ResponseCodes.OK:
                Status = "200 OK";
                break;
            case ResponseCodes.CREATED:
                Status = "201 Created";
                break;
            case ResponseCodes.NOT_FOUND:
                Status = "404 Not Found";
                break;
            default:
                break;
        }
        ;
    }
    public void SetHeader(string Header)
    {
        this.Header = Header;
    }
    public void SetBody(string Body)
    {
        this.Body = Body;
    }

    public void SetCompressGzip(bool gzip){
        CompressGzip = gzip;
    }
}
