using System;
using CaroNet.Shared.Protocol;

namespace CaroNet.Client.WinUI.Services
{
    public sealed class ClientMessageReceivedEventArgs : EventArgs
    {
        public MessageEnvelope Message { get; }

        public ClientMessageReceivedEventArgs(MessageEnvelope message)
        {
            Message = message;
        }
    }
}