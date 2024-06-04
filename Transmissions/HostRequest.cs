namespace OpenLobby.Transmissions
{
    /// <summary>
    /// A transmission to request to host a lobby
    /// </summary>
    internal class HostRequest : Transmission
    {
        private new const int HEADERSIZE = 1; // 7b maxClients + 1b publicVisible + 4b name Length + 4b password length
        private const int MaskPublic = 128;

        public bool PublicVisible
        {
            get => (Body[0] & MaskPublic) == MaskPublic;
            set => Body[0] = (byte)(Body[0] | (value ? MaskPublic : 0));
        }
        public byte MaxClients
        {
            get => (byte)(Body[0] & ~MaskPublic);
            set => Body[0] = (byte)(value & ~MaskPublic | Body[0] & MaskPublic);
        }
        public TString Name;
        public TString Password;

        public HostRequest(Transmission trms) : base(trms)
        {
            Name = new(Payload, HEADERSIZE);
            Password = new(Payload, HEADERSIZE + Name.Length);

            if (Name.Value.Length < 5 || Name.Value.Length > 16)
                throw new ArgumentOutOfRangeException($"Lobby name length {Name.Value.Length} is out of range");
            if (Password.Value.Length < 5 || Password.Value.Length > 16)
                throw new ArgumentOutOfRangeException($"Lobby password length {Password.Value.Length} is out of range");
            if (MaxClients == byte.MaxValue)
                throw new ArgumentException("Max clients cannot be 255");
        }
    }
}