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
    private static readonly Queue<Client> Clients = [];
    private static readonly Queue<Client> Pending = [];
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
            if (!Looping)
            {
                LoopOnce();
            }

            if (Console.KeyAvailable)
            {
                Closing = true;
                Console.WriteLine("\nServer closing");
                CloseTokenSource.Cancel();
            }
        }

        static async Task AcceptConnections()
        {
            while (!Close)
            {
                try
                {
                    var newClient = await Listener.Accept(CloseTokenSource.Token);
                    Pending.Enqueue(newClient);
                    Console.WriteLine("\nNew client connected: " + newClient.ToString());
                }
                catch
                {
                }
            }
        }
    }

    private static bool Looping { get; set; }
    private static void LoopOnce()
    {
        Looping = true;

        // Enqueue pending clients
        var pending = Pending.ToArray();
        Pending.Clear();
        foreach (var client in pending)
        {
            Clients.Enqueue(client);
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
                Console.WriteLine("\nReceived new transmission from: " + client.ToString());
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
                    Console.WriteLine("\nUnable to extract transmission type");
                    continue;
                }

                try
                {
                    Console.Write($"\nReceived {type} form: " + client.ToString());
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
                                client.Send(success.Payload);
                                Console.WriteLine("\nSuccessfully added new lobby: " + lobby.ToString());

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
                                client.Send(query.Payload);
                                Console.WriteLine("\nSent back lobby query result to: " + client.ToString());

                                break;
                            }
                        case Transmission.TransmisisonType.Join:
                            {
                                JoinRequest jr = new(trms, false);
                                if (jr.LobbyID is null || jr.LobbyPassword is null)
                                {
                                    Console.Write("\nJoin request was invalid");
                                    break;
                                }

                                if (ulong.TryParse(jr.LobbyID.Value, out var id) && OpenLobbies.TryGetValue(id, out Lobby? lobby))
                                {
                                    // Check password
                                    if (lobby.Password == jr.LobbyPassword.Value)
                                    {
                                        lobby.JoinedClients.Add(client);
                                        Console.WriteLine("\nAdded client to lobby: " + lobby.ToString());
                                        string ipPort = lobby.Host.Address.MapToIPv4().ToString() + ":" + lobby.Host.Port;
                                        jr = new(ipPort);
                                        client.Send(jr.Payload);
                                    }
                                    else
                                    {
                                        Reply r = new(Reply.Code.WrongPassword);
                                        client.Send(r.Payload);
                                        Console.Write("\nIncorrect password was provided");
                                    }
                                }

                                break;
                            }
                        default: throw new UnknownTransmission();
                    }
                }
                catch (UnknownTransmission)
                {
                    Console.WriteLine("\nSkipping unknown transmisison type");
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(e);
                    Reply err = new(Reply.ErrorTypeCodeMap[type]);
                    Console.WriteLine($"\nSending back {err.ReplyCode} reply to: " + client.ToString());
                    client.Send(err.Payload);
                }
            }
        }

        Looping = false;

        static ulong NewLobbyID()
        {
            ulong id = (ulong)(RandomNumberGenerator.GetInt32(0, int.MaxValue) + RandomNumberGenerator.GetInt32(0, int.MaxValue));
            while (OpenLobbies.ContainsKey(id))
                id = (ulong)(RandomNumberGenerator.GetInt32(0, int.MaxValue) + RandomNumberGenerator.GetInt32(0, int.MaxValue));
            return id;
        }
    }
}
