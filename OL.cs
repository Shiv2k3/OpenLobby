using System.Runtime.CompilerServices;
using System.Text;

namespace OpenLobby.OneLiners
{
    internal class OL
    {
        /// <summary>
        /// Sets the bytes from ushort into arr at index i1 & i2
        /// </summary>
        public static void SetUshort(ushort value, int i1, int i2, byte[] arr)
        {
            arr[i1] = (byte)(value >> 8);
            arr[i2] = (byte)value;
        }

        /// <summary>
        /// Gets ushort from arr by using bytes at index i1 & i2
        /// </summary>
        /// <returns></returns>
        public static ushort GetUshort(int i1, int i2, byte[] arr)
        {
            return (ushort)(arr[i1] << 8 | arr[i2]);
        }

        /// <summary>
        /// Sets the bytes from ushort into arr at index i1 & i2
        /// </summary>
        public static void SetUshort(ushort value, int i1, int i2, ArraySegment<byte> arr)
        {
            arr[i1] = (byte)(value >> 8);
            arr[i2] = (byte)value;
        }

        /// <summary>
        /// Gets ushort from arr by using bytes at index i1 & i2
        /// </summary>
        /// <returns></returns>
        public static ushort GetUshort(int i1, int i2, ArraySegment<byte> arr)
        {
            return (ushort)(arr[i1] << 8 | arr[i2]);
        }

        /// <summary>
        /// Counts Length of the strings
        /// </summary>
        /// <param name="strings">The strings to count</param>
        /// <returns>Length if Length is less than <seealso cref="ushort.MaxValue"/> </returns>
        /// <exception cref="ArgumentOutOfRangeException">The total Length of the strings were too long</exception>
        public static ushort GetWithinLength(params string[] strings)
        {
            int count = 0;
            foreach (var str in strings)
            {
                count += str.Length;
            }
            return (count <= ushort.MaxValue) ? (ushort)count : throw new ArgumentOutOfRangeException("Strings were too long");
        }

        public static string StringFromSpan(ArraySegment<byte> span)
        {
            return Encoding.ASCII.GetString(span);
        }

        internal static ReadOnlySpan<char> GetIP(ArraySegment<byte> arraySegment)
        {
            return $"{arraySegment[0]}.{arraySegment[1]}.{arraySegment[2]}.{arraySegment[3]}";
        }
    }

}