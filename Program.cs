using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Sockets;

Console.WriteLine("Listener socket is being setup");

IConfiguration config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
string portStr = config.GetRequiredSection("LISTEN_PORT").Value;
int port = int.Parse(portStr);
IPEndPoint lep = new(IPAddress.Any, port);
Socket listener = new(SocketType.Stream, ProtocolType.Tcp);
listener.Bind(lep);
listener.Listen();

Console.WriteLine("Listener socket has been setup");
Console.ReadKey();
listener.Close();