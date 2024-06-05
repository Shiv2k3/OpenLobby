using OpenLobby.OneLiners;

namespace OpenLobby.Transmissions
{
    /// <summary>
    /// An array of ByteStrings
    /// </summary>
    internal class StringArray
    {
        // first byte # of strings, C, followed by C many bytes stating offset to the start of the next string
        // max C is 255
        // max string length is 255
        private readonly ArraySegment<byte> Stream;
        private readonly ArraySegment<byte> Lengths;
        private readonly ArraySegment<byte> Body;

        /// <summary>
        /// Total number of strings
        /// </summary>
        public ByteMember Count { get; private set; }

        public StringArray(in ArraySegment<byte> body, int start, params string[] strings)
        {
            if (strings.Length > byte.MaxValue)
                throw new ArgumentException("Strings array was too long");

            int c = strings.Length;
            int bodyLength = OL.GetByteStringLength(strings);
            int header = 1;
            int length = header + c + bodyLength;

            Stream = body.Slice(start, length);
            Count = new(body, 0, (byte)c);

            Lengths = Stream.Slice(header, c);
            Body = Stream.Slice(c + header, bodyLength);

            start = 0;
            for (int i = 0; i < strings.Length; i++)
            {
                var s = new ByteString(strings[i], Body, start);
                this[i] = s;
                start += s.StreamLength;
            }
        }
        public StringArray(in ArraySegment<byte> body, int start)
        {
            int c = body[start];
            int header = 1;

            int indicesStart = start + header;
            int bodyStart = indicesStart + c;
            Lengths = body.Slice(indicesStart, c);

            int bodyLength = 0;
            for (int i = 0; i < c; i++)
            {
                bodyLength += Lengths[i];
            }
            int length = header + c + bodyLength;

            Stream = body.Slice(start, length);
            Count = new(Stream, 0, (byte)c);
            Body = body.Slice(bodyStart, bodyLength);
        }

        public ByteString this[int index]
        {
            get
            {
                if (index >= Stream[0])
                    throw new IndexOutOfRangeException();

                int stringIndex = 0;
                for (int i = 0; i < index; i++)
                {
                    stringIndex += Lengths[i];
                }
                return new(Body, stringIndex);
            }
            private set
            {
                if (index >= Stream[0])
                    throw new IndexOutOfRangeException();

                Lengths[index] = value.StreamLength;
            }
        }

        public static int GetHeaderSize(params string[] strings) => 1 + strings.Length + OL.GetByteStringLength(strings);
    }
}