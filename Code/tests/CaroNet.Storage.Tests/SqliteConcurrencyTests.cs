using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CaroNet.Storage.Matches;
using Xunit; // Hoặc sử dụng NUnit / Microsoft.VisualStudio.TestTools.UnitTesting tùy dự án

namespace CaroNet.Storage.Tests;

public class SqliteConcurrencyTests
{
    [Fact] // Đánh dấu đây là một bài test tự động
    public async Task SaveMatchAsync_ShouldHandleConcurrentWrites_WithoutLockingDatabase()
    {
        // 1. Khởi tạo SqliteMatchHistoryStore với file database test tạm thời
        string testDbPath = "caronet_concurrency_test.db";
        var store = new SqliteMatchHistoryStore(testDbPath);

        // --- SỬA Ở ĐÂY: Gọi hàm khởi tạo bảng trước khi lưu dữ liệu ---
        await store.InitializeDatabaseAsync();
        

        // 2. Mô phỏng danh sách 5 ván đấu ảo kết thúc cùng một thời điểm
        int numberOfConcurrentMatches = 5;
        var concurrentMatches = new List<MatchRecord>();

        for (int i = 1; i <= numberOfConcurrentMatches; i++)
        {
            var matchId = Guid.NewGuid();
            // ... (phần code tạo matchRecord giữ nguyên như cũ)

            var moves = new List<MatchMoveRecord>
        {
            new MatchMoveRecord(1, "PlayerX", 7, 7, DateTime.UtcNow.AddSeconds(5)),
            new MatchMoveRecord(2, "PlayerO", 8, 8, DateTime.UtcNow.AddSeconds(15))
        };

            var matchRecord = new MatchRecord(
                matchId,
                $"Room_Test_{i}",
                "PlayerX",
                "PlayerO",
                "PlayerX",
                DateTime.UtcNow.AddMinutes(-10),
                DateTime.UtcNow,
                moves
            );

            concurrentMatches.Add(matchRecord);
        }

        // 3. Kích hoạt đồng thời tất cả các hành động ghi lịch sử trận đấu
        IEnumerable<Task> saveTasks = concurrentMatches.Select(match =>
            store.SaveMatchAsync(match, CancellationToken.None));

        await Task.WhenAll(saveTasks);
    }
}