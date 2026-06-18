using System.Buffers.Binary;

namespace CaroNet.Shared.Protocol;

public sealed class ProtocolFrameReader
{
    private readonly List<byte> _buffer = [];

    public void Append(ReadOnlySpan<byte> data)
    {
        _buffer.AddRange(data.ToArray());
    }

    public bool TryReadFrame(out byte[] frame)
    {
        frame = [];

        if (_buffer.Count < 4)
        {
            return false;
        }

        int payloadLength =
            BinaryPrimitives.ReadInt32BigEndian(
                _buffer.Take(4).ToArray());

        if (payloadLength < 0)
        {
            throw new InvalidOperationException(
                "Invalid payload length.");
        }

        int totalLength =
            4 + payloadLength;

        if (_buffer.Count < totalLength)
        {
            return false;
        }

        frame =
            _buffer.Take(totalLength)
                   .ToArray();

        _buffer.RemoveRange(
            0,
            totalLength);

        return true;
    }
}