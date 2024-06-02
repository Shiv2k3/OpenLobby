using System.Net;
using System.Net.Sockets;

namespace OpenLobby
{
    internal class Client
    {
        private readonly Socket Socket;
        private Transmission? StalledTransmission;

        /// <summary>
        /// True when new transmission is available
        /// </summary>
        public bool Available => Socket.Available >= Transmission.HEADERSIZE;
        public IPEndPoint? RemoteEndpoint => Socket.RemoteEndPoint as IPEndPoint;

        /// <summary>
        /// Create a listening socket
        /// </summary>
        /// <param name="port">The port to listen on</param>
        public Client(int port)
        {
            IPEndPoint lep = new(IPAddress.Any, port);
            Socket = new(SocketType.Stream, ProtocolType.Tcp);
            Socket.Bind(lep);
            Socket.Listen();
        }

        /// <summary>
        /// Creates a client using a remote socket
        /// </summary>
        /// <param name="socket">The remote socket to use</param>
        /// <exception cref="ArgumentException">The given socket was not remote</exception>
        public Client(Socket socket)
        {
            if (socket.RemoteEndPoint is null)
                throw new ArgumentException("Socket was not remote");

            Socket = socket;
        }

        ~Client()
        {
            Socket.Close();
        }

        /// <summary>
        /// Tries to get a new transmission
        /// </summary>
        /// <returns>Null is no transmission is available</returns>
        public async Task<(bool success, Transmission? trms)> TryGetTransmission()
        {
            if (StalledTransmission is not null)
            {
                var t = await CompleteTransmission(StalledTransmission);
                return (t is not null, t);
            }

            if (StalledTransmission is null && Available)
            {
                byte[] header = new byte[Transmission.HEADERSIZE];
                await Receive(header);
                var t = await CompleteTransmission(new Transmission(header));
                return t is null ? (false, StalledTransmission = t) : (true, t);
            }

            return (false, null);

            async Task<Transmission?> CompleteTransmission(Transmission stalled)
            {
                if (Socket.Available < stalled.Length)
                    return null;

                byte[] data = new byte[stalled.Length];
                await Receive(data);

                return new Transmission(stalled.Payload, data);
            }
        }

        /// <summary>
        /// Sends the payload
        /// </summary>
        /// <param name="payload">The payload to send</param>
        /// <returns>False if unable to send</returns>
        public async Task Send(byte[] payload)
        {
            int count = 0;
            do
            {
                var segment = new ArraySegment<byte>(payload, count, payload.Length - count);
                count += await Socket.SendAsync(segment, SocketFlags.None);
            }
            while (count != payload.Length);
        }

        /// <summary>
        /// Receives payload into a byte array
        /// </summary>
        /// <param name="arr">The byte array to receive into, must be initalized to how many bytes to receive</param>
        public async Task Receive(byte[] arr)
        {
            int count = 0;
            do
            {
                var segment = new ArraySegment<byte>(arr, count, arr.Length - count);
                count += await Socket.ReceiveAsync(segment, SocketFlags.None);
            }
            while (count != arr.Length);
        }

        /// <summary>
        /// Accepts one new client asynchronously
        /// </summary>
        /// <returns>The new client</returns>
        public async Task<Client> Accept()
        {
            Socket remote = await Socket.AcceptAsync(new CancellationToken(Program.Close));
            return new(remote);
        }

        public override string ToString()
        {
            return Socket.RemoteEndPoint.ToString();
        }
    }
}