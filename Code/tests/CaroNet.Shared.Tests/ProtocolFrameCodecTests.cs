using System.Buffers.Binary;
using System.Text;
using System.Text.Json;
using CaroNet.Shared.Protocol;

namespace CaroNet.Shared.Tests;

public class ProtocolFrameCodecTests
{
    [Fact]
    public void Encode_Should_Write_Correct_BigEndian_Length_Prefix()
    {
        var message = new MessageEnvelope
        {
            Type = MessageType.Hello,
            Payload = JsonDocument.Parse("{}")
                .RootElement
                .Clone()
        };

        byte[] frame = ProtocolFrameCodec.Encode(message);

        int declaredLength =
            BinaryPrimitives.ReadInt32BigEndian(
                frame.AsSpan(0, 4));

        int actualPayloadLength =
            frame.Length - 4;

        Assert.Equal(
            actualPayloadLength,
            declaredLength);
    }

    [Fact]
    public void Encode_Then_Decode_Should_Preserve_MessageType()
    {
        var message = new MessageEnvelope
        {
            Type = MessageType.Hello,
            Payload = JsonDocument.Parse("{}")
                .RootElement
                .Clone()
        };

        byte[] frame = ProtocolFrameCodec.Encode(message);

        MessageEnvelope decoded =
            ProtocolFrameCodec.Decode(frame);

        Assert.Equal(
            MessageType.Hello,
            decoded.Type);
    }

    [Fact]
    public void Decode_Should_Reject_Unsupported_MessageType()
    {
        string json =
            """
            {
                "Type": 999
            }
            """;

        byte[] payload =
            Encoding.UTF8.GetBytes(json);

        byte[] frame =
            new byte[4 + payload.Length];

        BinaryPrimitives.WriteInt32BigEndian(
            frame.AsSpan(0, 4),
            payload.Length);

        payload.CopyTo(frame.AsSpan(4));

        Assert.Throws<InvalidOperationException>(
            () => ProtocolFrameCodec.Decode(frame));
    }

    [Fact]
    public void Decode_Should_Reject_Invalid_Length_Prefix()
    {
        var message = new MessageEnvelope
        {
            Type = MessageType.Hello,
            Payload = JsonDocument.Parse("{}")
                .RootElement
                .Clone()
        };

        byte[] frame =
            ProtocolFrameCodec.Encode(message);

        // Cố tình làm sai length prefix
        frame[3]++;

        Assert.Throws<InvalidOperationException>(
            () => ProtocolFrameCodec.Decode(frame));
    }

    [Fact]
    public void Decode_Should_Reject_Frame_Too_Short()
    {
        byte[] frame = [1, 2, 3];

        Assert.Throws<InvalidOperationException>(
            () => ProtocolFrameCodec.Decode(frame));
    }
}