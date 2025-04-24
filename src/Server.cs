using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

var ArgsHandler = new ArgsHandler(args);
string Directory = ArgsHandler.GetDirectory();

TcpListener Server = new TcpListener(IPAddress.Any, 4221);
Server.Start();

byte[] HandleForbidden()
{
    ResponseBuilder rb = new ResponseBuilder();
    rb.SetStatus(404);
    return rb.Build();
}
byte[] HandleInternalServer()
{
    ResponseBuilder rb = new ResponseBuilder();
    rb.SetStatus(404);
    return rb.Build();
}
byte[] HandleNotFound()
{
    ResponseBuilder rb = new ResponseBuilder();
    rb.SetStatus(404);
    return rb.Build();
}
byte[] Get()
{
    ResponseBuilder rb = new ResponseBuilder();
    rb.SetStatus(200);
    return rb.Build();
}
byte[] GetEcho(string received, bool gzip)
{
    var match = Regex.Match(received, @"^GET \/echo\/([^ ]+) HTTP\/1\.1");
    string content = match.Groups[1].Value;
    ResponseBuilder rb = new ResponseBuilder();
    rb.SetStatus(200);
    rb.SetHeader($"Content-Type: text/plain\r\nContent-Length: {content.Length}\r\n");
    rb.SetBody(content);
    rb.SetCompressGzip(gzip);
    return rb.Build();
}
byte[] GetUserAgent(string received)
{
    var match = Regex.Match(received, @"User-Agent: (.+?)\r\n");
    if (match.Success)
    {
        string content = match.Groups[1].Value.Trim();
        ResponseBuilder rb = new ResponseBuilder();
        rb.SetStatus(200);
        rb.SetHeader($"Content-Type: text/plain\r\nContent-Length: {content.Length}\r\n");
        rb.SetBody(content);
        return rb.Build();
    }
    else
    {
        ResponseBuilder rb = new ResponseBuilder();
        rb.SetStatus(400);
        return rb.Build();
    }
}

byte[] GetFile(string received)
{
    var match = Regex.Match(received, @"^GET \/files\/([^ ]+) HTTP\/1\.1");
    var filePath = $"{Directory}/{match.Groups[1].Value}";

    if (!File.Exists(filePath))
    {
        ResponseBuilder rb = new ResponseBuilder();
        rb.SetStatus(404);
        return rb.Build();
    }
    else
    {
        string readText = File.ReadAllText(filePath);
        ResponseBuilder rb = new ResponseBuilder();
        rb.SetStatus(200);
        rb.SetHeader($"Content-Type: application/octet-stream\r\nContent-Length: {readText.Length}\r\n");
        rb.SetBody(readText);
        return rb.Build();
    }
}
byte[] PostFile(string received)
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
            return rb.Build();

        }
        catch (UnauthorizedAccessException)
        {
            return HandleForbidden();
        }
        catch (IOException)
        {
            return HandleInternalServer();
        }
    }
    else
    {
        return HandleNotFound();
    }
}

byte[] HandleEndpoints(string Received)
{
    bool gzip = false;
    Match match = Regex.Match(Received, @"Accept-Encoding:\s*([^\r\n]+)");
    if (match.Success)
    {
        string encodingMethods = match.Groups[1].Value;
        string[] methods = encodingMethods.Split([','], StringSplitOptions.RemoveEmptyEntries)
                                        .Select(m => m.Trim())
                                        .ToArray();
        gzip = methods.Contains("gzip");
    }

    if (Regex.IsMatch(Received, @"^GET \/ HTTP\/1\.1"))
    {
        return Get();
    }
    else if (Regex.IsMatch(Received, @"^GET \/echo\/[^ ]+ HTTP\/1\.1"))
    {
        return GetEcho(Received, gzip);
    }
    else if (Regex.IsMatch(Received, @"^GET \/user-agent HTTP\/1\.1"))
    {
        return GetUserAgent(Received);
    }
    else if (Regex.IsMatch(Received, @"^GET \/files\/[^ ]+ HTTP\/1\.1"))
    {
        return GetFile(Received);
    }
    else if (Regex.IsMatch(Received, @"^POST \/files\/[^ ]+ HTTP\/1\.1"))
    {
        return PostFile(Received);
    }
    else
    {
        return HandleNotFound();
    }
}

bool TryGetCompleteRequest(ref StringBuilder receivedData, out string request)
{
    request = null;
    string data = receivedData.ToString();

    // 1. Znajdź koniec nagłówków
    int headerEnd = data.IndexOf("\r\n\r\n");
    if (headerEnd == -1) return false;

    // 2. Pobierz Content-Length jeśli istnieje
    string headers = data.Substring(0, headerEnd);
    int contentLength = 0;

    var contentLengthMatch = Regex.Match(headers, @"Content-Length:\s*(\d+)", RegexOptions.IgnoreCase);
    if (contentLengthMatch.Success)
    {
        contentLength = int.Parse(contentLengthMatch.Groups[1].Value);
    }

    // 3. Sprawdź czy mamy kompletne żądanie (nagłówki + ciało)
    int totalLength = headerEnd + 4 + contentLength;
    if (data.Length < totalLength) return false;

    // 4. Wyodrębnij żądanie
    request = data.Substring(0, totalLength);
    receivedData.Remove(0, totalLength);

    return true;
}

void HandleSocket(Socket acceptedSocket)
{
    byte[] buffer = new byte[4096]; // Większy bufor
    NetworkStream stream = new NetworkStream(acceptedSocket);
    StringBuilder receivedData = new StringBuilder();

    try
    {
        while (acceptedSocket.Connected)
        {

            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            if (bytesRead == 0) break;

            receivedData.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));

            // Przetwarzaj wszystkie kompletne żądania w buforze
            while (TryGetCompleteRequest(ref receivedData, out string request))
            {
                Console.WriteLine($"Processing request:\n{request}");
                byte[] response = HandleEndpoints(request);
                acceptedSocket.Send(response);
            }
        }
    }
    finally
    {
        acceptedSocket.Close();
    }
}

while (true)
{
    var AcceptedSocket = Server.AcceptSocket();
    Thread handleSocketThread = new Thread(() => HandleSocket(AcceptedSocket));
    handleSocketThread.Start();
}
