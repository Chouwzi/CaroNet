using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CaroNet.Server.Host.GameRooms;
using CaroNet.Server.Host.Networking;
using CaroNet.Server.Host.Services;
using CaroNet.Shared.Game;
using CaroNet.Shared.Protocol;
using CaroNet.Storage.Statistics;
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
                Type = MessageType.CreateRoom,
                Payload = JsonSerializer.SerializeToElement(new { })
            };

            // Gửi request thứ nhất (thành công bình thường)
            await _dispatcher.DispatchAsync(_session, envelope, CancellationToken.None);

            // Đợi dữ liệu RoomJoined truyền đi hoàn tất và đọc sạch nó khỏi socket client để giải phóng stream
            await Task.Delay(50);
            Assert.Equal(MessageType.RoomJoined, ReceiveEnvelope().Type);

            // Gửi ngay lập tức request thứ hai (sẽ bị chặn lại do khoảng cách < 100ms)
            await _dispatcher.DispatchAsync(_session, envelope, CancellationToken.None);

            // Đợi dữ liệu truyền đi hoàn tất
            await Task.Delay(50);

            // Đọc toàn bộ gói tin client nhận được từ server socket
            var response = ReceiveEnvelope();
            Assert.Equal(MessageType.Error, response.Type);
            Assert.True(response.Payload.HasValue);

            using var doc = JsonDocument.Parse(response.Payload.Value.GetRawText());
            var message = doc.RootElement.GetProperty("message").GetString();
            Assert.Equal("Rate limit exceeded.", message);
        }

        [Fact]
        public async Task DispatchAsync_ShouldAllowCreateRoomImmediatelyAfterHello()
        {
            var hello = new MessageEnvelope
            {
                Type = MessageType.Hello,
                Payload = JsonSerializer.SerializeToElement(new { playerName = "Alice" })
            };

            var createRoom = new MessageEnvelope
            {
                Type = MessageType.CreateRoom,
                Payload = JsonSerializer.SerializeToElement(new { })
            };

            await _dispatcher.DispatchAsync(_session, hello, CancellationToken.None);
            await _dispatcher.DispatchAsync(_session, createRoom, CancellationToken.None);

            MessageEnvelope firstResponse = ReceiveEnvelope();
            MessageEnvelope secondResponse = ReceiveEnvelope();

            Assert.Equal(MessageType.HelloAccepted, firstResponse.Type);
            Assert.Equal(MessageType.RoomJoined, secondResponse.Type);
            Assert.False(string.IsNullOrWhiteSpace(secondResponse.RoomId));
        }

        [Fact]
        public async Task SaveMatchHistoryAsync_ShouldUpdatePlayerRecords_WhenGameEnds()
        {
            var store = new InMemoryPlayerRecordStore();
            var dispatcher = new GameMessageDispatcher(
                new RoomManager(),
                new ClientSessionRegistry(),
                playerRecordStore: store);

            using var playerXPair = SocketPair.Create(dispatcher);
            using var playerOPair = SocketPair.Create(dispatcher);
            var room = new GameRoom();
            room.TryAddPlayer(playerXPair.ServerSession, "Alice");
            room.TryAddPlayer(playerOPair.ServerSession, "Bob");

            await InvokeSaveMatchHistoryAsync(dispatcher, room, GameStatus.XWon);
            await InvokeSaveMatchHistoryAsync(dispatcher, room, GameStatus.Draw);

            PlayerRecord? alice = await store.GetAsync("Alice");
            PlayerRecord? bob = await store.GetAsync("Bob");

            Assert.Equal(new PlayerRecord("Alice", 1, 0, 1), alice);
            Assert.Equal(new PlayerRecord("Bob", 0, 1, 1), bob);
        }

        private static async Task InvokeSaveMatchHistoryAsync(
            GameMessageDispatcher dispatcher,
            GameRoom room,
            GameStatus status)
        {
            MethodInfo method = typeof(GameMessageDispatcher).GetMethod(
                "SaveMatchHistoryAsync",
                BindingFlags.Instance | BindingFlags.NonPublic)!;

            var task = (Task)method.Invoke(dispatcher, [room, status])!;
            await task;
        }

        public void Dispose()
        {
            _clientSocket.Close();
            _serverSocket.Close();
            _listener.Close();
        }

        private MessageEnvelope ReceiveEnvelope()
        {
            var lengthBuffer = new byte[4];
            ReceiveExact(lengthBuffer);

            int payloadLength = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(lengthBuffer);
            var frame = new byte[4 + payloadLength];
            lengthBuffer.CopyTo(frame, 0);
            ReceiveExact(frame.AsSpan(4));

            return ProtocolFrameCodec.Decode(frame);
        }

        private void ReceiveExact(byte[] buffer)
        {
            ReceiveExact(buffer.AsSpan());
        }

        private void ReceiveExact(Span<byte> buffer)
        {
            int offset = 0;
            while (offset < buffer.Length)
            {
                int received = _clientSocket.Receive(buffer[offset..]);
                if (received == 0)
                {
                    throw new InvalidOperationException("Socket closed while reading test frame.");
                }

                offset += received;
            }
        }

        private sealed class InMemoryPlayerRecordStore : IPlayerRecordStore
        {
            private readonly Dictionary<string, PlayerRecord> _records = new(StringComparer.OrdinalIgnoreCase);

            public Task SaveAsync(PlayerRecord record, CancellationToken cancellationToken = default)
            {
                _records[record.PlayerName] = record;
                return Task.CompletedTask;
            }

            public Task<PlayerRecord?> GetAsync(string playerName, CancellationToken cancellationToken = default)
            {
                _records.TryGetValue(playerName, out PlayerRecord? record);
                return Task.FromResult(record);
            }

            public Task<IReadOnlyList<PlayerRecord>> GetTopPlayersAsync(
                int limit,
                CancellationToken cancellationToken = default)
            {
                IReadOnlyList<PlayerRecord> records = _records.Values.Take(limit).ToList();
                return Task.FromResult(records);
            }
        }

        private sealed class SocketPair : IDisposable
        {
            private readonly Socket _listener;
            private readonly Socket _clientSocket;
            private readonly Socket _serverSocket;

            private SocketPair(
                Socket listener,
                Socket clientSocket,
                Socket serverSocket,
                ClientSession serverSession)
            {
                _listener = listener;
                _clientSocket = clientSocket;
                _serverSocket = serverSocket;
                ServerSession = serverSession;
            }

            public ClientSession ServerSession { get; }

            public static SocketPair Create(GameMessageDispatcher dispatcher)
            {
                var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                listener.Listen(1);

                var clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                var acceptTask = listener.AcceptAsync();
                clientSocket.Connect(listener.LocalEndPoint!);
                Socket serverSocket = acceptTask.GetAwaiter().GetResult();

                return new SocketPair(
                    listener,
                    clientSocket,
                    serverSocket,
                    new ClientSession(serverSocket, dispatcher));
            }

            public void Dispose()
            {
                _clientSocket.Dispose();
                _serverSocket.Dispose();
                _listener.Dispose();
            }
        }
    }
}
