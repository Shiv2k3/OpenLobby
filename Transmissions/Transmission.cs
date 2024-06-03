using OpenLobby.OneLiners;

namespace OpenLobby
{
    /// <summary>
    /// Repersents a base header-only transmission without any data transmission, inherieting classes should simply wrap over Data
    /// </summary>
    internal class Transmission
    {
        public enum Types
        {
            HostRequest,
            Reply
        }
        /// <summary>
        /// Map of index to Transmission type
        /// </summary>
        public static readonly Dictionary<ushort, Type> IndexTransmission = new()
        {
            {0, typeof(HostRequest) },
            {1, typeof(Reply) }
        };
        /// <summary>
        /// Map of Transmission type to index
        /// </summary>
        public static readonly Dictionary<Type, ushort> TransmissionIndex = new()
        {
#nullable disable
            {IndexTransmission[0], 0 },
            {IndexTransmission[1], 1 }
#nullable enable
        };

        /// <summary>
        /// The number of header bytes, 2b TypeID + 2b Length
        /// </summary>
        public const int HEADERSIZE = 4;

        /// <summary>
        /// Maximum number of transmission bytes allowed
        /// </summary>
        public const int MAXBYTES = ushort.MaxValue - HEADERSIZE;

        /// <summary>
        /// The final payload
        /// </summary>
        private readonly byte[] Stream;

        /// <summary>
        /// Stream's body
        /// </summary>
        protected readonly ArraySegment<byte> Body;

        /// <summary>
        /// Transmission type identifier
        /// </summary>
        public ushort TypeID { get => OL.GetUshort(0, 1, Stream); protected set => OL.SetUshort(value, 0, 1, Stream); }

        /// <summary>
        /// The number of bytes of data
        /// </summary>
        public ushort Length { get => OL.GetUshort(2, 3, Stream); protected set => OL.SetUshort(value, 2, 3, Stream); }

        /// <summary>
        /// Create base class members
        /// </summary>
        /// <param name="dataLength">Length of data</param>
        protected Transmission(Type transmissionType, ushort dataLength)
        {
            Stream = new byte[dataLength + HEADERSIZE];
            Body = new(Stream, HEADERSIZE, dataLength);

            TypeID = (ushort)TransmissionIndex[transmissionType];
            Length = dataLength;
        }

        /// <summary>
        /// Initalizates transmission using another transmission, shouldn't be used on the base class
        /// </summary>
        /// <param name="trms">The transmission to use</param>
        protected Transmission(Transmission trms)
        {
            Stream = trms.Stream;
            Body = trms.Body;
        }

        /// <summary>
        /// Use payload header to create instance for payload intel
        /// </summary>
        /// <param name="payload"></param>
        public Transmission(byte[] header)
        {
            Stream = header;
        }
        /// <summary>
        /// Create transmission by combining the header and body
        /// </summary>
        /// <param name="header">The head of the payload</param>
        /// <param name="body">The body of the payload</param>
        public Transmission(byte[] header, byte[] body)
        {
            if (header.Length != HEADERSIZE) throw new($"Incorrect header length: {header.Length}");

            Stream = new byte[HEADERSIZE + body.Length];
            for (int i = 0; i < Stream.Length; i++)
            {
                Stream[i] = i < HEADERSIZE ? header[i] : body[i - HEADERSIZE];
            }
            Body = new(Stream, HEADERSIZE, body.Length);
        }

        /// <summary>
        /// The final payload
        /// </summary>
        public byte[] Payload { get => Stream; }
    }

    internal class UnknownTransmission(string? message) : Exception(message) { }
}