using Microsoft.Extensions.Configuration;
using OpenLobby.OneLiners;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace OpenLobby
{
    internal class Program
    {
        private static int ListenerPort => int.Parse(new ConfigurationBuilder().AddUserSecrets<Program>().Build()["LISTEN_PORT"]);

        private static readonly Client Listener = new(ListenerPort);
        private static readonly Queue<Client> Clients = [];
        private static readonly Queue<Transmission> Transmissions = [];
        private static readonly Dictionary<long, Lobby> OpenLobbies = [];

        public static bool Close { get; private set; }
        public static void Main(string[] args)
        {            
            Console.WriteLine("Server loop started");
            while (!Close)
            {
                while (!Looping)
                    LoopOnce();
            }
        }

        private static bool Looping { get; set; }
        private static async void LoopOnce()
        {
            Looping = true;

            // Accept new connections
            await Listener.Accept();

            // Receive transmission
            foreach (var client in Clients)
            {
                var (success, trms) = await client.TryGetTransmission();
                if (success)
                {
#nullable disable
                    Transmissions.Enqueue(trms);
#nullable enable
                }
            }

            // Read transmissions
            while (Transmissions.Count != 0)
            {
                var trms = Transmissions.Dequeue();
                switch ((Transmission.Types)trms.TypeID)
                {
                    case Transmission.Types.HostRequest:
                        HostRequest hostReq = new(trms);
                        long id = NewLobbyID();
                        Lobby lobby = new(hostReq.Host, id, hostReq.Name, hostReq.Password, hostReq.PublicVisible, hostReq.MaxClients);
                        OpenLobbies.Add(id, lobby);
                        break;

                    default: throw new ArgumentException("Unknown Transmission type");
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