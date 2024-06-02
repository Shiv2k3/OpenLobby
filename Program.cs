using Microsoft.Extensions.Configuration;

namespace OpenLobby
{
    internal partial class Program
    {
        public static bool Close { get; private set; }

        private static Client? Listener;
        private static readonly Queue<Client> Clients = new();
        private static readonly Queue<Transmission> Transmissions = new();

        private static async void Main(string[] args)
        {
            Console.WriteLine("Listener is being setup");

            IConfiguration config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
            int port = int.Parse(config["LISTEN_PORT"]);
            Listener = new(port);

            Console.WriteLine("Listener has been setup");
            
            Console.WriteLine("Server loop started");
            while (!Close)
            {
                // Accept new connections
                await Listener.Accept();

                // Receive transmission
                foreach (var client in Clients)
                {
                    var (success, trms) = await client.TryGetTransmission();
                    if (success)
                    {
                        Transmissions.Enqueue(trms);
                    }
                }

                // Read transmissions
                while(Transmissions.Count != 0)
                {
                    var trms = Transmissions.Dequeue();
                    switch (trms.TypeID)
                    {
                        case 0: // Host request
                            HostRequest hostReq = new HostRequest(trms);
                            // TODO: Create Lobby Class, Make lobby list, new lobby using this req, add it to list
                            break;
                        default:
                            break;
                    }
                }
            }
        }
    }


}