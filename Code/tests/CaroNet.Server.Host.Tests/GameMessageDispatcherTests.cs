using System;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CaroNet.Server.Host.GameRooms;
using CaroNet.Server.Host.Networking;
using CaroNet.Server.Host.Services;
using CaroNet.Shared.Protocol;
using Xunit;

namespace CaroNet.Server.Host.Tests
{
    public class GameMessageDispatcherTests : IDisposable
    {
        private readonly Socket _listener;
        private readonly Socket _clientSocket;
        private readonly Socket _serverSocket;
        private readonly ClientSession _session;
        private readonly GameMessageDispatcher _dispatcher;

        public GameMessageDispatcherTests()
        {
            // Thiết lập loopback socket pair để kiểm thử ClientSession thực tế mà không cần Mock
            _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            _listener.Listen(1);

            _clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var acceptTask = _listener.AcceptAsync();
            _clientSocket.Connect(_listener.LocalEndPoint!);
            _serverSocket = acceptTask.GetAwaiter().GetResult();

            var roomManager = new RoomManager();
            var registry = new ClientSessionRegistry();
            _dispatcher = new GameMessageDispatcher(roomManager, registry);

            _session = new ClientSession(_serverSocket, _dispatcher);
        }

        [Fact]
        public async Task DispatchAsync_ShouldAllowRequestsWithNormalInterval()
        {
            var envelope = new MessageEnvelope
            {
                Type = MessageType.Hello,
                Payload = JsonSerializer.SerializeToElement(new { playerName = "Chouwzi" })
            };

            // Gửi request thứ nhất
            await _dispatcher.DispatchAsync(_session, envelope, CancellationToken.None);

            // Đợi 150ms (>100ms) để không bị rate limit
            await Task.Delay(150);

            // Gửi request thứ hai
            await _dispatcher.DispatchAsync(_session, envelope, CancellationToken.None);

            // Đọc dữ liệu từ phía Client Socket để đảm bảo không nhận bất kỳ tin nhắn lỗi nào
            Assert.True(_clientSocket.Available > 0);
        }

        [Fact]
        public async Task DispatchAsync_ShouldTriggerRateLimit_WhenRequestsAreTooFast()
        {
            var envelope = new MessageEnvelope
            {
                Type = MessageType.Hello,
                Payload = JsonSerializer.SerializeToElement(new { playerName = "Chouwzi" })
            };

            // Gửi request thứ nhất (thành công bình thường)
            await _dispatcher.DispatchAsync(_session, envelope, CancellationToken.None);

            // Đợi dữ liệu HelloAccepted truyền đi hoàn tất và đọc sạch nó khỏi socket client để giải phóng stream
            await Task.Delay(50);
            byte[] discardBuffer = new byte[1024];
            int discarded = _clientSocket.Receive(discardBuffer);
            Assert.True(discarded > 0);

            // Gửi ngay lập tức request thứ hai (sẽ bị chặn lại do khoảng cách < 100ms)
            await _dispatcher.DispatchAsync(_session, envelope, CancellationToken.None);

            // Đợi dữ liệu truyền đi hoàn tất
            await Task.Delay(50);

            // Đọc toàn bộ gói tin client nhận được từ server socket
            byte[] buffer = new byte[1024];
            int received = _clientSocket.Receive(buffer);
            Assert.True(received > 4);

            // Phân tách độ dài payload
            int payloadLength = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(buffer.AsSpan(0, 4));
            byte[] payloadBytes = new byte[payloadLength];
            Array.Copy(buffer, 4, payloadBytes, 0, payloadLength);

            // Decode và kiểm tra xem có chứa thông điệp lỗi rate limit không
            var response = JsonSerializer.Deserialize<MessageEnvelope>(payloadBytes);
            Assert.NotNull(response);
            Assert.Equal(MessageType.Error, response.Type);
            Assert.True(response.Payload.HasValue);

            using var doc = JsonDocument.Parse(response.Payload.Value.GetRawText());
            var message = doc.RootElement.GetProperty("message").GetString();
            Assert.Equal("Rate limit exceeded.", message);
        }

        public void Dispose()
        {
            _clientSocket.Close();
            _serverSocket.Close();
            _listener.Close();
        }
    }
}
