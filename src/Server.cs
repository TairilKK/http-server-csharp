using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

TcpListener server = new TcpListener(IPAddress.Any, 4221);
server.Start();

void handleSocket(Socket acceptedSocket) {
    byte[] recived_message = new byte[256];
    var bytes_recived = acceptedSocket.Receive(recived_message);
    var received = Encoding.UTF8.GetString(recived_message);
    Console.WriteLine(received);
    if (Regex.IsMatch(received, @"^GET \/ HTTP\/1\.1")){
        string response = "HTTP/1.1 200 OK\r\n\r\n";
        var r = acceptedSocket.Send(Encoding.UTF8.GetBytes(response));
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
    else {
        string response = "HTTP/1.1 404 Not Found\r\n\r\n";
        var r = acceptedSocket.Send(Encoding.UTF8.GetBytes(response));
        Console.WriteLine(r);
    }
}



while (true){
    var acceptedSocket = server.AcceptSocket();
    Thread handleSocketThread = new Thread( () => handleSocket(acceptedSocket));
    // handleSocket(acceptedSocket);
    handleSocketThread.Start();
}
