# P0 WinUI Manual Evidence

Use this checklist when the server/client-net contract is ready.

## Build

```powershell
dotnet build .\Code\CaroNet.slnx -c Debug -p:Platform=x64
dotnet test .\Code\CaroNet.slnx -c Debug -p:Platform=x64
```

## Manual Flow

1. Run the server host.
2. Open two WinUI clients in Visual Studio x64.
3. Client A enters player name, then clicks Connect.
4. Client A clicks Create Room and records the room id.
5. Client B enters player name, then clicks Connect.
6. Client B enters the room id and clicks Join Room.
7. Client A clicks one board cell.
8. Verify both clients render the same X/O after server state broadcast.
9. Try a wrong-turn or occupied-cell move.
10. Verify the server rejection appears in the UI error area.

## Current UI Branch Notes

- The WinUI views call ViewModels/services, not raw sockets.
- The local demo service exists only to keep the UI contract usable until the real client socket service from the P0 client-net/ui-contract work is available.
- Chat/history UI is intentionally excluded from P0.
- The client UI must not decide win/loss; final result must come from server state.
