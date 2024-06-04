namespace OpenLobby.Transmissions
{
    internal class LobbyQuery : Transmission
    {
        public TString Search;
        // TODO:
        // new way to create transmisson strings
        // think of some query parameters
        public LobbyQuery(Transmission trms) : base(trms) 
        {
            // Ensure all parameters are valid
        }
    }
}