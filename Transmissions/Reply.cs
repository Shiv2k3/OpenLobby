namespace OpenLobby.Transmissions
{
    /// <summary>
    /// Transmission type used to reply to requests
    /// </summary>
    internal class Reply : Transmission
    {
        public enum Code : byte
        {
            /// <summary>
            /// Lobby creation was a success 
            /// </summary>
            LobbyCreated,
            /// <summary>
            /// Lobby creation was unsuccessful
            /// </summary>
            HostingError
        }

        public Code ReplyCode { get => (Code)Body[0]; set => Body[0] = (byte)value; }

        public Reply(Code code) : base(typeof(Reply), 1) { ReplyCode = code; }
        public Reply(Transmission trms) : base(trms) { }
    }
}