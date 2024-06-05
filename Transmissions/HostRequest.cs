using OpenLobby.OneLiners;

namespace OpenLobby.Transmissions
{
    /// <summary>
    /// A transmission to request to host a lobby
    /// </summary>
    internal class HostRequest : Transmission
    {
        public ByteMember MaxClients;
        public ByteMember Visible;
        public ByteString Name;
        public ByteString Password;

        /// <summary>
        /// Creates a transmission for requesting to host (Client-Side)
        /// </summary>
        /// <param name="name">The lobby name, 5 <= Length <= 16</param>
        /// <param name="password">The lobby password used to authenticate clients, 5 < Length < 16</param>
        /// <param name="publicVisible">Is the lobby publicly searchable</param>
        /// <param name="maxClients">Max number of playe, must be greater than 1</param>
        public HostRequest(string name, string password, bool publicVisible, byte maxClients) : base(typeof(HostRequest), (ushort)(HEADERSIZE + OL.GetByteStringLength(name, password)))
        {
            TestInput(name, password, maxClients);

            MaxClients = new(Body, 0, maxClients);
            Visible = new(Body, 1, publicVisible ? byte.MaxValue : byte.MinValue);
            Name = new(name, Body, 2);
            Password = new(password, Body, 2 + Name.StreamLength);
        }

        public HostRequest(Transmission trms) : base(trms)
        {
            MaxClients = new(Body, 0);
            Visible = new(Body, 1);
            Name = new(Body, 2);
            Password = new(Body, 2 + Name.StreamLength);

            TestInput(Name.Value, Password.Value, MaxClients.Value);
        }

        private void TestInput(string name, string pass, byte max)
        {
            if (name.Length < 5 || name.Length > 16)
                throw new ArgumentOutOfRangeException($"Lobby name length {Name.Value.Length} is out of range");
            if (pass.Length < 5 || pass.Length > 16)
                throw new ArgumentOutOfRangeException($"Lobby password length {Password.Value.Length} is out of range");
            if (max < 2)
                throw new ArgumentException("There must be more than 1 client");

        }
    }
}