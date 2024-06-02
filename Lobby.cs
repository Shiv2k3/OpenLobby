using System.Net;

namespace OpenLobby
{
    /// <summary>
    /// Repersents a lobby
    /// </summary>
    internal record Lobby
    {
        /// <summary>
        /// IP address of the lobby host
        /// </summary>
        public IPAddress Host;
        /// <summary>
        /// Lobby ID
        /// </summary>
        public long ID;
        /// <summary>
        /// Lobby name
        /// </summary>
        public string Name;
        /// <summary>
        /// Lobby password
        /// </summary>
        public string Password;
        /// <summary>
        /// Is the lobby public
        /// </summary>
        public bool PublicVisible;
        /// <summary>
        /// Lobby max client count
        /// </summary>
        public byte MaxClients;

        /// <summary>
        /// List of joined clients
        /// </summary>
        public List<Client> JoinedClients = [];

        /// <summary>
        /// Creates the lobby record
        /// </summary>
        /// <param name="host"></param>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <param name="password"></param>
        /// <param name="publicVisible"></param>
        /// <param name="maxClients"></param>
        public Lobby(IPAddress host, long id, string name, string password, bool publicVisible, byte maxClients)
        {
            Host = host;
            ID = id;
            Name = name;
            Password = password;
            PublicVisible = publicVisible;
            MaxClients = maxClients;
        }
    }


}