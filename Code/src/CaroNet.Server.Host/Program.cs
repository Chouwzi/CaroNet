using CaroNet.Server.Host.GameRooms;
using CaroNet.Server.Host.Networking;
using CaroNet.Server.Host.Services;
using CaroNet.Storage.Database;
using CaroNet.Storage.Matches;
using CaroNet.Storage.Statistics; 

var cancellationTokenSource = new CancellationTokenSource();
Console.CancelKeyPress += (sender, eventArgs) =>
{
    Console.WriteLine("[SERVER] Shutdown requested...");
    eventArgs.Cancel = true;
    cancellationTokenSource.Cancel();
};

const string dbPath = "caronet.db";
var dbInitializer = new DatabaseInitializer(dbPath);
dbInitializer.Initialize();
Console.WriteLine("[SERVER] Database initialized.");

var matchStore = new SqliteMatchHistoryStore(dbPath);
var playerRecordStore = new SqlitePlayerRecordStore(dbPath); // Đã khởi tạo

var registry = new ClientSessionRegistry();
var roomManager = new RoomManager();

// Đã cập nhật constructor để truyền playerRecordStore
var dispatcher = new GameMessageDispatcher(
    roomManager,
    registry,
    matchStore,
    playerRecordStore);

var server = new SocketServer(registry, dispatcher);

try
{
    await server.RunAsync(cancellationTokenSource.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("[SERVER] Stopped.");
}