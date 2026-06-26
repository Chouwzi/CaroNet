using System;

namespace CaroNet.Shared.Protocol.Payloads;

public class ChatReceivedPayload
{
    public string SenderName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}