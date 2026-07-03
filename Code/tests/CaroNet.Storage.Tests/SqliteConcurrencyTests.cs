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

        // 2. Mô phỏng danh sách 5 ván đấu ảo kết thúc cùng một thời điểm
        int numberOfConcurrentMatches = 5;
        var concurrentMatches = new List<MatchRecord>();

        for (int i = 1; i <= numberOfConcurrentMatches; i++)
        {
            var matchId = Guid.NewGuid();
            var startedAt = DateTime.UtcNow.AddMinutes(-10);
            var endedAt = DateTime.UtcNow;

            var moves = new List<MatchMoveRecord>
            {
                new MatchMoveRecord(1, "PlayerX", 7, 7, startedAt.AddSeconds(5)),
                new MatchMoveRecord(2, "PlayerO", 8, 8, startedAt.AddSeconds(15))
            };

            var matchRecord = new MatchRecord(
                matchId,
                $"Room_Test_{i}",
                "PlayerX",
                "PlayerO",
                "PlayerX",
                startedAt,
                endedAt,
                moves
            );

            concurrentMatches.Add(matchRecord);
        }

        // 3. Kích hoạt đồng thời tất cả các hành động ghi lịch sử trận đấu (SaveMatchAsync)
        IEnumerable<Task> saveTasks = concurrentMatches.Select(match =>
            store.SaveMatchAsync(match, CancellationToken.None));

        // Thực thi đồng thời và kiểm tra xem có thăng hoa vượt qua lỗi "database is locked" không
        // Nếu có lỗi SqliteException xảy ra, Test Explorer sẽ báo Đỏ (Fail). Nếu chạy mượt, sẽ báo Xanh (Pass).
        await Task.WhenAll(saveTasks);
    }
}