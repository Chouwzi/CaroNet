using System.Text.Json;
using CaroNet.Shared.Protocol;

namespace CaroNet.Shared.Tests;

public class ProtocolFrameCodecTests
{
    [Fact]
    public void Encode_Should_Write_Length_Prefix()
    {
        var message = new MessageEnvelope
        {
            Type = MessageType.Hello,
            Payload = JsonDocument.Parse("{}").RootElement
        };

        byte[] frame = ProtocolFrameCodec.Encode(message);

        Assert.True(frame.Length > 4);
    }

    [Fact]
    public void Encode_Then_Decode_Should_Preserve_MessageType()
    {
        var message = new MessageEnvelope
        {
            Type = MessageType.Hello,
            Payload = JsonDocument.Parse("{}").RootElement
        };

        byte[] frame = ProtocolFrameCodec.Encode(message);

        byte[] payload = frame[4..];

        MessageEnvelope decoded =
            ProtocolFrameCodec.Decode(payload);

        Assert.Equal(
            MessageType.Hello,
            decoded.Type);
    }
}