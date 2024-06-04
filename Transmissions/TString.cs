using System.Text;

namespace OpenLobby.Transmissions
{
    /// <summary>
    /// String encoded into an array
    /// </summary>
    internal class TString
    {
        public const int HEADERSIZE = 1;
        public string Value { get => Encoding.ASCII.GetString(Body); set => Encoding.ASCII.GetBytes(value, Body.AsSpan()); }
        public byte Length => Backstore[0];

        private readonly ArraySegment<byte> Backstore;
        private readonly ArraySegment<byte> Body;

        /// <summary>
        /// Reconstructs string using a backstore
        /// </summary>
        /// <param name="arr">The backstore to use for decoding the string</param>
        /// <param name="offset">The starting index of the encoding in arr</param>
        public TString(byte[] arr, int offset)
        {
            Backstore = new ArraySegment<byte>(arr, offset, arr[offset]);
            Body = new ArraySegment<byte>(arr, offset, Length);
        }
    }
}
