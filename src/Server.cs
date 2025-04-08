using System.Net;
using System.Net.Sockets;
using System.Text;

Console.WriteLine("Logs from your program will appear here!");

TcpListener server = new TcpListener(IPAddress.Any, 4221);
server.Start();
var acceptedSocket = server.AcceptSocket();

string response = "HTTP/1.1 200 OK\r\n\r\n";
var r = acceptedSocket.Send(Encoding.UTF8.GetBytes(response));
Console.WriteLine(r);
