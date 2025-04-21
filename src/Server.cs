using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

string? directory = null;
for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--directory" && i + 1 < args.Length)
    {
        directory = args[i + 1];
        break;
    }
}

TcpListener server = new TcpListener(IPAddress.Any, 4221);
server.Start();

void handleSocket(Socket acceptedSocket) {
    byte[] recived_message = new byte[256];
    var bytes_recived = acceptedSocket.Receive(recived_message);
    var received = Encoding.UTF8.GetString(recived_message);
    Console.WriteLine($"Recived: {received}");
    if (Regex.IsMatch(received, @"^GET \/ HTTP\/1\.1")){
        ResponseBuilder rb = new ResponseBuilder();
        rb.SetStatus(200);
        var r = acceptedSocket.Send(rb.Create());
        Console.WriteLine(r);
    }
    else if (Regex.IsMatch(received, @"^GET \/echo\/[^ ]+ HTTP\/1\.1"))
    {
        var match = Regex.Match(received, @"^GET \/echo\/([^ ]+) HTTP\/1\.1");
        string content = match.Groups[1].Value;

        string response = $"HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\nContent-Length: {content.Length}\r\n\r\n{content}";
        var r = acceptedSocket.Send(Encoding.UTF8.GetBytes(response));
        Console.WriteLine(r);
    }
    else if (Regex.IsMatch(received, @"^GET \/user-agent HTTP\/1\.1"))
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
    else if (Regex.IsMatch(received, @"^GET \/files\/[^ ]+ HTTP\/1\.1"))
    {
        var match = Regex.Match(received, @"^GET \/files\/([^ ]+) HTTP\/1\.1");
        var filePath = $"{directory}/{match.Groups[1].Value}";
        Console.WriteLine(filePath);
        if(!File.Exists(filePath)){
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
    else if (Regex.IsMatch(received, @"^POST \/files\/[^ ]+ HTTP\/1\.1"))
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

            Console.WriteLine($"Method: {method}");
            Console.WriteLine($"Path: {path}");
            Console.WriteLine($"Host: {host}");
            Console.WriteLine($"Content-Length: {contentLength}");
            Console.WriteLine($"Content-Type: {contentType}");
            Console.WriteLine($"Body: {body}");

            try
            {

                string sanitizedPath = Path.GetFileName(path);
                if (string.IsNullOrEmpty(sanitizedPath))
                {
                    throw new ArgumentException("Invalid file name");
                }

                Directory.CreateDirectory(directory);

                string fullPath = Path.Combine(directory, sanitizedPath);

                File.WriteAllText(fullPath, body.Substring(0, contentLength));

                ResponseBuilder rb = new ResponseBuilder();
                rb.SetStatus(ResponseCodes.CREATED);
                var r = acceptedSocket.Send(rb.Create());
                Console.WriteLine(r);
            }
            catch (UnauthorizedAccessException)
            {
                ResponseBuilder rb = new ResponseBuilder();
                rb.SetStatus(ResponseCodes.FORBIDDEN);
                var r = acceptedSocket.Send(rb.Create());
                Console.WriteLine(r);
            }
            catch (IOException ex)
            {
                ResponseBuilder rb = new ResponseBuilder();
                rb.SetStatus(ResponseCodes.INTERNAL_SERVER_ERROR);
                var r = acceptedSocket.Send(rb.Create());
                Console.WriteLine(r);
            }
        }
        else{
            ResponseBuilder rb = new ResponseBuilder();
            rb.SetStatus(404);
            var r = acceptedSocket.Send(rb.Create());
            Console.WriteLine(r);
        }

    }
    else {
        ResponseBuilder rb = new ResponseBuilder();
        rb.SetStatus(404);
        var r = acceptedSocket.Send(rb.Create());
        Console.WriteLine(r);
    }
}



while (true){
    var acceptedSocket = server.AcceptSocket();
    Thread handleSocketThread = new Thread( () => handleSocket(acceptedSocket));
    handleSocketThread.Start();
}
