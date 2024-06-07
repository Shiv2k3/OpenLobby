using Microsoft.Extensions.Configuration;
using System.Net;
using System.Security.Cryptography;
using OpenLobby.Utility.Network;
using OpenLobby.Utility.Transmissions;
using OpenLobby.Utility.Util;

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
    public static bool Close { get; private set; }
    public static int TotalFrames { get; private set; }
    public static void Main(string[] args)
    {
        Console.WriteLine("Server started");
        _ = AcceptConnections();
        while (!Close)
        {
            if (!Looping)
            {
                LoopOnce();
                TotalFrames++;
            }

            if (Console.KeyAvailable)
            {
                Close = true;
                CloseTokenSource.Cancel();
                Console.WriteLine("\nServer closing");
            }
        }

        static async Task AcceptConnections()
        {
            while (!Close)
            {
                var newClient = await Listener.Accept();
                Pending.Enqueue(newClient);
                Console.WriteLine("\nNew client connected: " + newClient.ToString());
            }
        }
    }

    private static bool Looping { get; set; }
    private static async void LoopOnce()
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
            var (success, trms) = await client.TryGetTransmission();
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
                var type = (Transmission.Types)trms.TypeID;
                try
                {
                    switch (type)
                    {
                        case Transmission.Types.HostRequest:
                            {
                                HostRequest hostReq = new(trms);
                                ulong id = NewLobbyID();
                                IPEndPoint rep = client.RemoteEndpoint ?? throw new ArgumentException("Not a remote endpoit");
                                Lobby lobby = new(rep, id, hostReq.Name.Value, hostReq.Password.Value, hostReq.Visible.Value > 0, hostReq.MaxClients.Value);
                                OpenLobbies.Add(id, lobby);

                                Reply success = new(Reply.Code.LobbyCreated);
                                await client.Send(success.Payload);
                                Console.WriteLine("\nSuccessfully added new lobby: " + lobby.ToString());

                                break;
                            }
                        case Transmission.Types.LobbyQuery:
                            {
                                LobbyQuery lq = new(trms);
                                var r = OpenLobbies.Values.ToArray();
                                var s = new string[r.Length];
                                for (int i = 0; i < s.Length; i++)
                                {
                                    s[i] = r[i].Name;
                                }
                                lq = new(s);
                                await client.Send(lq.Payload);
                                Console.WriteLine("\nSend back lobby query result to: " + client.ToString());

                                break;
                            }
                        default: throw new UnknownTransmission($"Unknown Transmission type: {type}");
                    }
                }
                catch (ArgumentException e)
                {
                    Console.Error.WriteLine(e);
                    switch (type)
                    {
                        case Transmission.Types.HostRequest:
                            Reply err = new(Reply.Code.HostingError);
                            Console.WriteLine("\nSending back HostingError reply to: " + client.ToString());
                            await client.Send(err.Payload);
                            break;

                    }
                    continue;
                }
                catch { throw; }
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