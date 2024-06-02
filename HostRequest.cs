using OpenLobby.OneLiners;
using System.Net;

namespace OpenLobby
{
    /// <summary>
    /// A transmission to request to host a lobby
    /// </summary>
    internal class HostRequest : Transmission
    {
        private new const int HEADERSIZE = 4 + 2 + 1 + 1; // ip + port + 7/maxClients + 1/publicVisible + 4/name Length + 4/ password length
        private const int Mask = 0b1000_0000;
        private int NameLength => Body[8] >> 4;
        private int PasswordLength => Body[8] & 0b1111;

        public IPAddress Host { get => IPAddress.Parse(Body.Slice(0, 4).ToString()??"NULL"); }
        public ushort Port { get => OL.GetUshort(5, 6, Body); set => OL.SetUshort(value, 5, 6, Body); }
        public string Name { get => Body.Slice(9, NameLength).ToString(); }
        public string Password { get => Body.Slice(9 + NameLength, PasswordLength).ToString(); }
        public bool PublicVisible { get => (Body[7] & Mask) == 1; set => Body[7] = (byte)(Body[7] | (value ? Mask : 0)); }
        public byte MaxClients { get => (byte)(Body[7] & ~Mask); set => Body[7] = (byte)(value & ~Mask); }

        /// <summary>
        /// Creates a transmission for requesting to host (Client-Side)
        /// </summary>
        /// <param name="host">The lobby endpoint</param>
        /// <param name="name">The lobby name, 5 <= Length <= 16</param>
        /// <param name="password">The lobby password used to authenticate clients, 5 < Length < 16</param>
        /// <param name="publicVisible">Is the lobby publicly searchable</param>
        /// <param name="maxClients">Max number of player, must be less than 128</param>
        public HostRequest(IPEndPoint host, string name, string password, bool publicVisible, byte maxClients) : base(typeof(HostRequest), (ushort)(HEADERSIZE + OL.GetWithinLength(name, password)))
        {
            if (host.AddressFamily is not System.Net.Sockets.AddressFamily.InterNetwork)
                throw new ArgumentException("host must be IPV4");
            if (name.Length < 5 || name.Length > 16)
                throw new ArgumentOutOfRangeException($"Lobby name length {name.Length} is out of range");
            if (password.Length < 5 || password.Length > 16)
                throw new ArgumentOutOfRangeException($"Lobby password length {password.Length} is out of range");
            if (maxClients == byte.MaxValue)
                throw new ArgumentException("Max clients cannot be 255");

            // Setup ip bytes
            var ip = host.Address.GetAddressBytes();
            for (int i = 0; i < ip.Length; i++)
            {
                Body[i] = ip[i];
            }
            Port = (ushort)host.Port;

            // Setup name/password length
            byte namebyte = (byte)(name.Length << 4);
            byte passbyte = (byte)password.Length;
            Body[8] = (byte)(namebyte | passbyte);

            // Copy name
            var nameBody = Body.Slice(9, name.Length);
            for (int i = 0; i < name.Length; i++)
            {
                nameBody[i] = (byte)name[i];
            }

            // Copy password
            var passBody = Body.Slice(9 + name.Length, password.Length);
            for (int i = 0; i < password.Length; i++)
            {
                passBody[i] = (byte)password[i];
            }

            PublicVisible = publicVisible;
            MaxClients = maxClients;
        }

        public HostRequest(Transmission trms) : base(trms) { }
    }

}