using System.Text;
using System.Text.Json;

namespace CaroNet.Shared.Protocol;

public static class ProtocolCodec
{
    public static byte[] Encode(MessageEnvelope message)
    {
        var json = JsonSerializer.Serialize(message);
        return Encoding.UTF8.GetBytes(json);
    }

    public static MessageEnvelope Decode(byte[] data)
    {
        var json = Encoding.UTF8.GetString(data);
        return JsonSerializer.Deserialize<MessageEnvelope>(json)!;
    }

    public static async Task<MessageEnvelope> DecodeAsync(
        System.IO.Stream stream,
        CancellationToken cancellationToken = default)
    {
        var buffer = new byte[4096];
        var bytesRead = await stream.ReadAsync(buffer, cancellationToken);

        return Decode(buffer[..bytesRead]);
    }
}