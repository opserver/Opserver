using System;
using System.Buffers;
using System.Text;

namespace Opserver
{
    public static partial class ExtensionMethods
    {
        internal static string GetString(this ref SequenceReader<byte> reader, int length)
        {
            var value = Encoding.UTF8.GetString(reader.UnreadSpan.Slice(0, length).Trim((byte)0));
            reader.Advance(length);
            return value;
        }

        internal static string GetBase64EncodedString(this ref SequenceReader<byte> reader, int length)
        {
            var value = Convert.ToBase64String(reader.UnreadSpan.Slice(0, length));
            reader.Advance(length);
            return value;
        }

        internal static bool TryReadBigEndian(this ref SequenceReader<byte> reader, out double value)
        {
            if (!reader.TryReadBigEndian(out long longValue))
            {
                value = default;
                return false;
            }

            value = BitConverter.Int64BitsToDouble(longValue);
            return true;
        }
    }
}
