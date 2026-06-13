# Storage Contracts

## Scope

P1 storage hiện tại chỉ cung cấp contract và in-memory implementation để các phần server/UI có thể gọi thử mà không phụ thuộc SQLite.

Đã làm:

- `MatchRecord`: lưu room id, người chơi X/O, người thắng, thời gian bắt đầu/kết thúc và danh sách nước đi.
- `MatchMoveRecord`: lưu thứ tự nước đi, người đánh, tọa độ và thời gian.
- `InMemoryMatchHistoryStore`: lưu, đọc theo match id, đọc toàn bộ theo thứ tự mới trước và lọc theo người chơi.
- `PlayerRecord`: lưu thống kê thắng/thua/hòa và tính `TotalGames`, `WinRate`.
- `InMemoryPlayerRecordStore`: lưu best records và lấy top player theo win rate, số trận thắng, rồi tên.

Chưa làm:

- SQLite schema và migration.
- UI xem lịch sử trận.
- Replay/leaderboard nâng cao.

## Validation

Store chỉ nhận match đã kết thúc. Nếu `EndedAtUtc` chưa có, store ném lỗi thay vì tự tạo dữ liệu giả.

In-memory store trả snapshot riêng để caller không sửa trực tiếp dữ liệu đang giữ trong store.

## Verification

```powershell
dotnet test .\Code\CaroNet.slnx -c Debug -p:Platform=x64
dotnet build .\Code\CaroNet.slnx -c Debug -p:Platform=x64
```

Evidence ngày 2026-06-13:

- Tests passed: 23 total, 0 failed.
- Build passed: 0 warnings, 0 errors.
