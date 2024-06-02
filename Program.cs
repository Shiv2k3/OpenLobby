﻿using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;

namespace OpenLobby
{
    internal class Program
    {
        private static int ListenerPort => int.Parse(new ConfigurationBuilder().AddUserSecrets<Program>().Build()["LISTEN_PORT"]);

        private static readonly Client Listener = new(ListenerPort);
        private static readonly Queue<Client> Clients = [];
        private static readonly Dictionary<Client, Queue<Transmission>> ClientTransmissionsQueue = [];
        private static readonly Dictionary<long, Lobby> OpenLobbies = [];

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
                    Clients.Enqueue(newClient);
                    ClientTransmissionsQueue[newClient] = new Queue<Transmission>();
                    Console.WriteLine("New client connected: " + newClient.ToString());
                }
            }
        }

        private static bool Looping { get; set; }
        private static async void LoopOnce()
        {
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write("Ticking " + TotalFrames);

            Looping = true;

            // Receive transmission
            foreach (var client in Clients)
            {
                var (success, trms) = await client.TryGetTransmission();
                if (success)
                {
#nullable disable
                    ClientTransmissionsQueue[client].Enqueue(trms);
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
                                long id = NewLobbyID();
                                Lobby lobby = new(hostReq.Address, hostReq.Port, id, hostReq.Name, hostReq.Password, hostReq.PublicVisible, hostReq.MaxClients);
                                OpenLobbies.Add(id, lobby);
                                Reply success = new(Reply.Code.LobbyCreated);
                                await client.Send(success.Payload);
                                Console.WriteLine("Successfully added new lobby: " + lobby.ToString() + " | Requested form" + client.ToString());
                                break;

                            default: throw new UnknownTransmission("Unknown Transmission type");
                        }
                    }
                    catch (ArgumentException)
                    {
                        switch ((Transmission.Types)trms.TypeID)
                        {
                            case Transmission.Types.HostRequest:
                                Reply err = new(Reply.Code.HostingError);
                                await client.Send(err.Payload);
                                break;

                        }
                        continue;
                    }
                    catch { throw; }
                }
            }

            Looping = false;

            static int NewLobbyID()
            {
                int id = RandomNumberGenerator.GetInt32(int.MaxValue) + RandomNumberGenerator.GetInt32(int.MaxValue);
                while (OpenLobbies.ContainsKey(id))
                    id = RandomNumberGenerator.GetInt32(int.MaxValue) + RandomNumberGenerator.GetInt32(int.MaxValue);
                return id;
            }
        }
    }
}