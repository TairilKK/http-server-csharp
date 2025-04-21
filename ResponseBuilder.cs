using System.Text;

enum ResponseCodes{
    OK, CREATED, NOT_FOUND,
    FORBIDDEN,
    INTERNAL_SERVER_ERROR
}

class ResponseBuilder {
    string? status;
    string? header;
    string? body;

    public override string ToString(){
        return body is null ? $"HTTP/1.1 {status}\r\n{header}\r\n" : $"HTTP/1.1 {status}\r\n{header}\r\n{body}\r\n";
    }

    public byte[] Create(){
        return Encoding.UTF8.GetBytes(ToString());
    }

    public void SetStatus(int v)
    {
        switch (v)
        {
            case 200:
                status="200 OK";
                break;
            case 201:
                status="201 Created";
                break;
            case 404:
                status="404 Not Found";
                break;
            default:
                break;
        };
    }
    public void SetStatus(ResponseCodes code)
    {
        switch (code)
        {
            case ResponseCodes.OK:
                status="200 OK";
                break;
            case ResponseCodes.CREATED:
                status="201 Created";
                break;
            case ResponseCodes.NOT_FOUND:
                status="404 Not Found";
                break;
            default:
                break;
        };
    }
    public void SetHeader(string header){
        this.header = header;
    }
    public void SetBody(string body){
        this.body = body;
    }
}
