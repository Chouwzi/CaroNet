using System.Buffers.Binary;
using System.Text;
using System.Text.Json;

namespace CaroNet.Shared.Protocol;

public static class ProtocolFrameCodec
{
    public static byte[] Encode(MessageEnvelope envelope)
    {
        string json =
            JsonSerializer.Serialize(envelope);

        byte[] payloadBytes =
            Encoding.UTF8.GetBytes(json);

        byte[] frame =
            new byte[4 + payloadBytes.Length];

        BinaryPrimitives.WriteInt32BigEndian(
            frame.AsSpan(0, 4),
            payloadBytes.Length);

        payloadBytes.CopyTo(frame.AsSpan(4));

        return frame;
    }

    public static MessageEnvelope Decode(
        ReadOnlySpan<byte> payload)
    {
        string json =
            Encoding.UTF8.GetString(payload);

        MessageEnvelope? envelope =
            JsonSerializer.Deserialize<MessageEnvelope>(
                json);

        if (envelope is null)
        {
            throw new InvalidOperationException(
                "Invalid message.");
        }

        return envelope;
    }
}