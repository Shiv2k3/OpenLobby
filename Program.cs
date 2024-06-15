using Microsoft.Extensions.Configuration;
using System.Net;
using System.Security.Cryptography;
using OpenLobby.Utility.Network;
using OpenLobby.Utility.Transmissions;
using OpenLobby.Utility.Utils;

namespace OpenLobby;

class Program
{
    private static int ListenerPort
    {
        get
        {
            var p = new ConfigurationBuilder().AddUserSecrets<Program>().Build()["LISTEN_PORT"];
            if (p is not null)
                return int.Parse(p);

            throw new ArgumentException("LISTEN_PORT is not defined");
        }
    }

    private static readonly Client Listener = new(ListenerPort);
    private static readonly List<Client> Clients = [];
    private static readonly Queue<Client> PendingConnected = [];
    private static readonly Queue<Client> PendingDisconnected = [];
    private static readonly Dictionary<Client, Queue<Transmission>> ClientTransmissionsQueue = [];
    private static readonly Dictionary<ulong, Lobby> OpenLobbies = [];

    private static readonly CancellationTokenSource CloseTokenSource = new();
    public static bool Closing { get; private set; }
    public static void Main()
    {
        Console.WriteLine("Server started");
        _ = AcceptConnections();
        while (!Closing)
        {
            LoopOnce();
            if (Console.KeyAvailable)
            {
                Closing = true;
                Console.WriteLine("Server closing");
                CloseTokenSource.Cancel();
                CloseConnections();
                Console.WriteLine("Server closed");
            }
        }

        static void CloseConnections()
        {
            foreach (var client in Clients)
            {
                Reply dc = new(Reply.Code.DisconnectInit);
                _ = client.Send(dc.Payload);
                Thread.Sleep(1000);
                client.Disconnect();
                Console.WriteLine("Disconnected client:" + client.ToString());
            }
        }
        static async Task AcceptConnections()
        {
            while (!Closing)
            {
                try
                {
                    var newClient = await Listener.Accept(CloseTokenSource.Token);
                    PendingConnected.Enqueue(newClient);
                    Console.WriteLine("New client connected: " + newClient.ToString());
                }
                catch
                {
                }
            }
        }
    }

    private static void LoopOnce()
    {
        // Enqueue pending connected clients
        var pending = PendingConnected.ToArray();
        PendingConnected.Clear();
        foreach (var client in pending)
        {
            Clients.Add(client);
            ClientTransmissionsQueue[client] = new Queue<Transmission>();
        }

        // Receive transmission
        foreach (var client in Clients)
        {
            var (success, trms) = client.TryGetTransmission();
            if (success)
            {
#nullable disable
                ClientTransmissionsQueue[client].Enqueue(trms);
                Console.WriteLine("Received new transmission from: " + client.ToString());
#nullable enable
            }
        }

        // Read transmissions
        foreach (var ctq in ClientTransmissionsQueue)
        {
            var (client, transmissions) = ctq;

            while (transmissions.Count != 0)
            {
                var trms = transmissions.Dequeue();
                var type = Transmission.TransmisisonType.Host;
                try { type = trms.Type; }
                catch (Exception)
                {
                    Console.WriteLine("Unable to extract transmission type");
                    continue;
                }

                try
                {
                    Console.WriteLine($"Received {type} form: " + client.ToString());
                    switch (type)
                    {
                        case Transmission.TransmisisonType.Host:
                            {
                                HostRequest hostReq = new(trms);
                                ulong id = NewLobbyID();
                                IPEndPoint rep = client.RemoteEndpoint ?? throw new ArgumentException("Not a remote endpoit");
                                Lobby lobby = new(rep, id, hostReq.Name.Value, hostReq.Password.Value, hostReq.Visible.AsBool, hostReq.MaxClients.Value);
                                OpenLobbies.Add(id, lobby);

                                Reply success = new(Reply.Code.HostingSuccess);
                                _ = client.Send(success.Payload);
                                Console.WriteLine("Successfully added new lobby: " + lobby.ToString());

                                break;
                            }
                        case Transmission.TransmisisonType.Query:
                            {
                                var idNamePair = OpenLobbies.ToArray();
                                var result = new string[idNamePair.Length*2];
                                for (int i = 0; i < idNamePair.Length; i++)
                                {
                                    result[i * 2] = idNamePair[i].Key.ToString();
                                    result[i * 2 + 1] = idNamePair[i].Value.Name;
                                }

                                LobbyQuery query = new(trms, false);
                                query = new(result);
                                _ = client.Send(query.Payload);
                                Console.WriteLine("Sent back lobby query result to: " + client.ToString());

                                break;
                            }
                        case Transmission.TransmisisonType.Join:
                            {
                                JoinRequest jr = new(trms, false);
                                if (jr.LobbyID is null || jr.LobbyPassword is null)
                                {
                                    Console.WriteLine("Join request was invalid");
                                    break;
                                }

                                if (ulong.TryParse(jr.LobbyID.Value, out var id) && OpenLobbies.TryGetValue(id, out Lobby? lobby))
                                {
                                    // Check password
                                    if (lobby.Password == jr.LobbyPassword.Value)
                                    {
                                        lobby.JoinedClients.Add(client);
                                        Console.WriteLine("Added client to lobby: " + lobby.ToString());
                                        string ipPort = lobby.Host.Address.MapToIPv4().ToString() + ":" + lobby.Host.Port;
                                        jr = new(ipPort);
                                        _ = client.Send(jr.Payload);
                                    }
                                    else
                                    {
                                        Reply r = new(Reply.Code.WrongPassword);
                                        _ = client.Send(r.Payload);
                                        Console.WriteLine("Incorrect password was provided");
                                    }
                                }

                                break;
                            }
                        case Transmission.TransmisisonType.Reply:
                            {
                                Reply r = new(trms);
                                switch (r.ReplyCode)
                                {
                                    case Reply.Code.DisconnectInit:
                                        {
                                            PendingDisconnected.Enqueue(client);
                                            Console.WriteLine("Client has been added to disconnection queue: " + client);
                                            break;
                                        }
                                }
                                break;
                            }
                        default: throw new UnknownTransmission();
                    }
                }
                catch (UnknownTransmission)
                {
                    Console.WriteLine("Skipping unknown transmisison type");
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(e);
                    Reply err = new(Reply.ErrorTypeCodeMap[type]);
                    Console.WriteLine($"Sending back {err.ReplyCode} reply to: " + client.ToString());
                    _ = client.Send(err.Payload);
                }
            }
        }

        // Remove pending disconnected client
        while (PendingDisconnected.Count != 0)
        {
            var client = PendingDisconnected.Dequeue();
            ClientTransmissionsQueue.Remove(client);
            client.Disconnect();
            Clients.Remove(client);
            Console.WriteLine("Client has been disconnected" + client.ToString());
        }

        static ulong NewLobbyID()
        {
            ulong id = (ulong)(RandomNumberGenerator.GetInt32(0, int.MaxValue) + RandomNumberGenerator.GetInt32(0, int.MaxValue));
            while (OpenLobbies.ContainsKey(id))
                id = (ulong)(RandomNumberGenerator.GetInt32(0, int.MaxValue) + RandomNumberGenerator.GetInt32(0, int.MaxValue));
            return id;
        }
    }
}
