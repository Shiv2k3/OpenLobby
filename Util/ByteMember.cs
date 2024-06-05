namespace OpenLobby.Transmissions
{
    /// <summary>
    /// Wraps the serialization and deserialization of a byte into an arr
    /// </summary>
    internal class ByteMember
    {
        public byte Value { get; init; }
        public ByteMember(in ArraySegment<byte> body, int index, byte value) => Value = body[index] = value;
        public ByteMember(in ArraySegment<byte> body, int index) => Value = body[index];
    }
}