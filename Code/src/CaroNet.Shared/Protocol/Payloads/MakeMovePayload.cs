namespace CaroNet.Shared.Protocol.Payloads;

public sealed class MakeMovePayload
{
    public int Row { get; init; }

    public int Column { get; init; }

    public string PlayerId { get; init; } =
        string.Empty;
}