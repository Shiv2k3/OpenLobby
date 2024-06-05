using OpenLobby.OneLiners;

namespace OpenLobby.Transmissions
{
    internal class LobbyQuery : Transmission
    {
        /// <summary>
        /// The search parameter
        /// </summary>
        public ByteString? Search;

        /// <summary>
        /// The query result
        /// </summary>
        public StringArray? Lobbies;

        /// <summary>
        /// Creates query, client-side
        /// </summary>
        /// <param name="search">Lobby name</param>
        public LobbyQuery(string search) : base(typeof(LobbyQuery), OL.GetByteStringLength(search))
        {
            Search = new(search, Body, 0);
        }
        public LobbyQuery(params string[] lobbies) : base(typeof(LobbyQuery), OL.GetByteStringLength(lobbies))
        {
            Lobbies = new(Body, 0, lobbies);
        }
        /// <summary>
        /// Creates query reply, server-side
        /// </summary>
        public LobbyQuery(Transmission trms) : base(trms)
        {
            Lobbies = new(Body, 0);
        }
    }
}