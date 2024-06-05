using System.Text;

namespace OpenLobby.Transmissions
{
    /// <summary>
    /// String encoded into an array
    /// </summary>
    internal class ByteString
    {
        public const int HEADERSIZE = 1; // 1B length
        public string Value { get => Encoding.ASCII.GetString(Body); set => Encoding.ASCII.GetBytes(value, Body.AsSpan()); }
        /// <summary>
        /// The length of the stream
        /// </summary>
        public byte StreamLength { get => Stream[0]; private set => Stream[0] = value; }

        public readonly ArraySegment<byte> Stream;
        private readonly ArraySegment<byte> Body;

        /// <summary>
        /// Encodes a string into an array from an index
        /// </summary>
        /// <param name="value">The string to encode</param>
        /// <param name="arr">The backstore</param>
        /// <param name="start">The starting index at the backstore</param>
        /// <exception cref="ArgumentException">Length of arr or value was invalid</exception>
        public ByteString(string value, ArraySegment<byte> arr, int start)
        {
            if (value.Length > byte.MaxValue + HEADERSIZE)
                throw new ArgumentException("String length must be less than 255 + HEADERSIZE");
            if (arr.Count - start < value.Length + HEADERSIZE)
                throw new ArgumentException("The array is not big enough for the encoding, don't forget to count HEADERSIZE of TString");

            Stream = arr.Slice(start, HEADERSIZE + value.Length);
            Body = Stream.Slice(HEADERSIZE, value.Length);

            StreamLength = (byte)(value.Length + HEADERSIZE);
            Value = value;
        }

        /// <summary>
        /// Reconstructs string using a backstore
        /// </summary>
        /// <param name="arr">The backstore to use for decoding the string</param>
        /// <param name="start">The starting index of the encoding in arr</param>
        public ByteString(ArraySegment<byte> arr, int start)
        {
            Stream = arr.Slice(start, arr[start]);
            Body = Stream.Slice(HEADERSIZE, StreamLength - HEADERSIZE);
        }
    }
}
