using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

TcpListener server = new TcpListener(IPAddress.Any, 4221);
server.Start();

var acceptedSocket = server.AcceptSocket();
byte[] recived_message = new byte[256];
var bytes_recived = acceptedSocket.Receive(recived_message);
var received = Encoding.UTF8.GetString(recived_message);
Console.WriteLine(received);
if (System.Text.RegularExpressions.Regex.IsMatch(received, @"^GET \/ HTTP\/1\.1")){
    string response = "HTTP/1.1 200 OK\r\n\r\n";
    var r = acceptedSocket.Send(Encoding.UTF8.GetBytes(response));
    Console.WriteLine(r);
}
else if (System.Text.RegularExpressions.Regex.IsMatch(received, @"^GET \/echo\/[^ ]+ HTTP\/1\.1"))
{
    var match = System.Text.RegularExpressions.Regex.Match(received, @"^GET \/echo\/([^ ]+) HTTP\/1\.1");
    string content = match.Groups[1].Value;

    string response = $"HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\nContent-Length: {content.Length}\r\n\r\n{content}";
    var r = acceptedSocket.Send(Encoding.UTF8.GetBytes(response));
    Console.WriteLine(r);
}
else {
    string response = "HTTP/1.1 404 Not Found\r\n\r\n";
    var r = acceptedSocket.Send(Encoding.UTF8.GetBytes(response));
    Console.WriteLine(r);
}
