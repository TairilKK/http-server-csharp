using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

var ArgsHandler = new ArgsHandler(args);
string? Directory = ArgsHandler.GetDirectory();
if(Directory is null)
{
    Directory = "./";
}

TcpListener Server = new TcpListener(IPAddress.Any, 4221);
Server.Start();

void HandleForbidden(Socket acceptedSocket)
{
    ResponseBuilder rb = new ResponseBuilder();
    rb.SetStatus(404);
    var r = acceptedSocket.Send(rb.Create());
    Console.WriteLine(r);
}
void HandleInternalServer(Socket acceptedSocket)
{
    ResponseBuilder rb = new ResponseBuilder();
    rb.SetStatus(404);
    var r = acceptedSocket.Send(rb.Create());
    Console.WriteLine(r);
}
void HandleNotFound(Socket acceptedSocket)
{
    ResponseBuilder rb = new ResponseBuilder();
    rb.SetStatus(404);
    var r = acceptedSocket.Send(rb.Create());
    Console.WriteLine(r);
}

void Get(Socket acceptedSocket, string received)
{
    ResponseBuilder rb = new ResponseBuilder();
    rb.SetStatus(200);
    var r = acceptedSocket.Send(rb.Create());
    Console.WriteLine(r);
}
void GetEcho(Socket acceptedSocket, string received, bool gzip)
{
    var match = Regex.Match(received, @"^GET \/echo\/([^ ]+) HTTP\/1\.1");
    string content = match.Groups[1].Value;
    ResponseBuilder rb = new ResponseBuilder();
    rb.SetStatus(200);
    if(gzip){
        content = Gzip.CompresString(content);
        rb.SetHeader($"Content-Type: text/plain\r\nContent-Length: {content.Length}\r\nContent-Encoding: gzip\r\n");
    } else {
        rb.SetHeader($"Content-Type: text/plain\r\nContent-Length: {content.Length}\r\n");
    }
    rb.SetBody(content);
    var r = acceptedSocket.Send(rb.Create());
    Console.WriteLine(r);
}
void GetUserAgent(Socket acceptedSocket, string received)
{
    var match = Regex.Match(received, @"User-Agent: (.+?)\r\n");
    if (match.Success)
    {
        string content = match.Groups[1].Value.Trim();
        Console.WriteLine(content);

        string response = $"HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\nContent-Length: {content.Length}\r\n\r\n{content}";
        var r = acceptedSocket.Send(Encoding.UTF8.GetBytes(response));
        Console.WriteLine(r);
    }
    else
    {
        string response = "HTTP/1.1 400 Bad Request\r\n\r\n";
        acceptedSocket.Send(Encoding.UTF8.GetBytes(response));
    }
}
void GetFile(Socket acceptedSocket, string received)
{
    var match = Regex.Match(received, @"^GET \/files\/([^ ]+) HTTP\/1\.1");
    var filePath = $"{Directory}/{match.Groups[1].Value}";
    Console.WriteLine(filePath);
    if (!File.Exists(filePath))
    {
        ResponseBuilder rb = new ResponseBuilder();
        rb.SetStatus(404);
        var ra = acceptedSocket.Send(rb.Create());
        Console.WriteLine(ra);
        return;
    }

    string readText = File.ReadAllText(filePath);

    string response =
        $"HTTP/1.1 200 OK\r\nContent-Type: application/octet-stream\r\nContent-Length: {readText.Length}\r\n\r\n{readText}";
    Console.WriteLine(response);
    var r = acceptedSocket.Send(Encoding.UTF8.GetBytes(response));
    Console.WriteLine(r);
}

void PostFile(Socket acceptedSocket, string received)
{
    var match = Regex.Match(received,
            @"^(?<method>\w+)\s(?<path>[^\s]+)\sHTTP/1\.1\r?\n" +
            @"Host:\s(?<host>[^\r\n]+)\r?\n" +
            @"Content-Length:\s(?<contentLength>\d+)\r?\n" +
            @"Content-Type:\s(?<contentType>[^\r\n]+)\r?\n\r?\n" +
            @"(?<body>.*)$",
            RegexOptions.Singleline);

    if (match.Success)
    {
        string method = match.Groups["method"].Value;
        string path = match.Groups["path"].Value;
        string host = match.Groups["host"].Value;
        int contentLength = int.Parse(match.Groups["contentLength"].Value);
        string contentType = match.Groups["contentType"].Value;
        string body = match.Groups["body"].Value;

        try
        {

            string sanitizedPath = Path.GetFileName(path);
            if (string.IsNullOrEmpty(sanitizedPath))
            {
                throw new ArgumentException("Invalid file name");
            }

            System.IO.Directory.CreateDirectory(Directory);

            string fullPath = Path.Combine(Directory, sanitizedPath);

            File.WriteAllText(fullPath, body.Substring(0, contentLength));

            ResponseBuilder rb = new ResponseBuilder();
            rb.SetStatus(ResponseCodes.CREATED);
            var r = acceptedSocket.Send(rb.Create());
            Console.WriteLine(r);
        }
        catch (UnauthorizedAccessException)
        {
            HandleForbidden(acceptedSocket);
        }
        catch (IOException ex)
        {
            HandleInternalServer(acceptedSocket);
            Console.WriteLine(ex);
        }
    }
    else
    {
        HandleNotFound(acceptedSocket);
    }
}

void HandleSocket(Socket AcceptedSocket)
{
    byte[] RecivedMessage = new byte[256];
    var BytesRecived = AcceptedSocket.Receive(RecivedMessage);
    var Received = Encoding.UTF8.GetString(RecivedMessage);
    Console.WriteLine($"Recived: {Received}");
    bool gzip = false;
    Match match = Regex.Match(Received, @"Accept-Encoding:\s*([^\r\n]+)");
    if (match.Success)
    {
        string encodingMethods = match.Groups[1].Value;
        string[] methods = encodingMethods.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                        .Select(m => m.Trim())
                                        .ToArray();
        gzip = methods.Contains("gzip");
    }

    if (Regex.IsMatch(Received, @"^GET \/ HTTP\/1\.1"))
    {
        Get(AcceptedSocket, Received);
    }
    else if (Regex.IsMatch(Received, @"^GET \/echo\/[^ ]+ HTTP\/1\.1"))
    {
        GetEcho(AcceptedSocket, Received, gzip);
    }
    else if (Regex.IsMatch(Received, @"^GET \/user-agent HTTP\/1\.1"))
    {
        GetUserAgent(AcceptedSocket, Received);
    }
    else if (Regex.IsMatch(Received, @"^GET \/files\/[^ ]+ HTTP\/1\.1"))
    {
        GetFile(AcceptedSocket, Received);
    }
    else if (Regex.IsMatch(Received, @"^POST \/files\/[^ ]+ HTTP\/1\.1"))
    {
        PostFile(AcceptedSocket, Received);
    }
    else
    {
        HandleNotFound(AcceptedSocket);
    }
}

while (true)
{
    var AcceptedSocket = Server.AcceptSocket();
    Thread handleSocketThread = new Thread(() => HandleSocket(AcceptedSocket));
    handleSocketThread.Start();
}
