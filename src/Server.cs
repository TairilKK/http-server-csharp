using System.Net;
using System.Net.Sockets;
using System.Text;

TcpListener server = new TcpListener(IPAddress.Any, 4221);
server.Start();

var acceptedSocket = server.AcceptSocket();
byte[] recived_message = new byte[256];
var bytes_recived = acceptedSocket.Receive(recived_message);
var recived = Encoding.UTF8.GetString(recived_message);
Console.WriteLine(recived);

if (recived.Contains("GET / HTTP/1.1")){
    string response = "HTTP/1.1 200 OK\r\n\r\n";
    var r = acceptedSocket.Send(Encoding.UTF8.GetBytes(response));
    Console.WriteLine(r);
} else {
    string response = "HTTP/1.1 404 Not Found\r\n\r\n";
    var r = acceptedSocket.Send(Encoding.UTF8.GetBytes(response));
    Console.WriteLine(r);
}
