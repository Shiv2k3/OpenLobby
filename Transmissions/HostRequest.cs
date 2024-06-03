using OpenLobby.OneLiners;

namespace OpenLobby.Transmissions
{
    /// <summary>
    /// A transmission to request to host a lobby
    /// </summary>
    internal class HostRequest : Transmission
    {
        private new const int HEADERSIZE = 1 + 1; // 7b maxClients + 1b publicVisible + 4b name Length + 4b password length
        private const int MaskPublic = 0b1000_0000;
        private const int PasswordMask = 0b1111_0000;

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
        private int NameLength
        {
            // First 4 bits
            get => Body[1] >> 4;
            set => Body[1] = (byte)(Body[1] | (((byte)value << 4) & PasswordMask));
        }

        private int PasswordLength
        {
            // Last 4 bits
            get => Body[1] & ~PasswordMask;
            set => Body[1] = (byte)(Body[1] | ((byte)value & ~PasswordMask));
        }
        public string Name
        {
            get => OL.StringFromSpan(Body.Slice(HEADERSIZE, NameLength));
        }
        public string Password
        {
            get => OL.StringFromSpan(Body.Slice(HEADERSIZE + NameLength, PasswordLength));
        }

        public HostRequest(Transmission trms) : base(trms)
        {
            if (Name.Length < 5 || Name.Length > 16)
                throw new ArgumentOutOfRangeException($"Lobby name length {NameLength} is out of range");
            if (Password.Length < 5 || Password.Length > 16)
                throw new ArgumentOutOfRangeException($"Lobby password length {PasswordLength} is out of range");
            if (MaxClients == byte.MaxValue)
                throw new ArgumentException("Max clients cannot be 255");
        }
    }
}