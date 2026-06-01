# Protocol

This document tracks the planned socket protocol for CaroNet.

## Transport

- Game traffic: TCP using raw `System.Net.Sockets.Socket`.
- Optional LAN discovery: UDP broadcast in a later sprint.
- Payload encoding: UTF-8 JSON.
- Framing: 4-byte big-endian length prefix followed by the JSON payload.

```text
[length: 4 bytes][json payload: length bytes]
```

## Envelope

```json
{
  "type": "MakeMove",
  "requestId": "uuid",
  "roomId": "room-001",
  "playerId": "player-001",
  "payload": {}
}
```

## Message groups

Client to server:

- `Hello`
- `CreateRoom`
- `JoinRoom`
- `Ready`
- `MakeMove`
- `Chat`
- `Heartbeat`
- `Reconnect`

Server to client:

- `HelloAccepted`
- `RoomListUpdated`
- `RoomJoined`
- `GameStarted`
- `MoveAccepted`
- `MoveRejected`
- `GameStateUpdated`
- `GameEnded`
- `ChatReceived`
- `Error`

## Socket implementation notes

- Keep one receive loop per connected socket.
- Serialize sends through one queue or lock per socket.
- Use cancellation tokens for shutdown.
- Log malformed frames, unsupported message types and timeout events.
- Never trust board state sent from the client; the server validates moves against its own state.
