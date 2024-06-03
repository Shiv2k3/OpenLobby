using Microsoft.Extensions.Configuration;
using System.Net;
using System.Security.Cryptography;
using OpenLobby.Network;
using OpenLobby.Transmissions;

namespace OpenLobby
{
    internal class Program
    {
        private static int ListenerPort => int.Parse(new ConfigurationBuilder().AddUserSecrets<Program>().Build()["LISTEN_PORT"]);

        private static readonly Client Listener = new(ListenerPort);
        private static readonly Queue<Client> Clients = [];
        private static readonly Queue<Client> Pending = [];
        private static readonly Dictionary<Client, Queue<Transmission>> ClientTransmissionsQueue = [];
        private static readonly Dictionary<ulong, Lobby> OpenLobbies = [];

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
                    try
                    {
                        switch ((Transmission.Types)trms.TypeID)
                        {
                            case Transmission.Types.HostRequest:
                                HostRequest hostReq = new(trms);
                                ulong id = NewLobbyID();
                                IPEndPoint rep = client.RemoteEndpoint ?? throw new ArgumentException("Not a remote endpoit");
                                Lobby lobby = new(rep, id, hostReq.Name, hostReq.Password, hostReq.PublicVisible, hostReq.MaxClients);
                                OpenLobbies.Add(id, lobby);

                                Reply success = new(Reply.Code.LobbyCreated);
                                await client.Send(success.Payload);
                                Console.WriteLine("\nSuccessfully added new lobby: " + lobby.ToString());
                                break;

                            default: throw new UnknownTransmission("Unknown Transmission type");
                        }
                    }
                    catch (ArgumentException e)
                    {
                        Console.Error.WriteLine(e);
                        switch ((Transmission.Types)trms.TypeID)
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
}