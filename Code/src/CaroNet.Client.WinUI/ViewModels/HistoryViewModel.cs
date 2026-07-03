using CaroNet.Client.WinUI.Models;
using CaroNet.Client.WinUI.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace CaroNet.Client.WinUI.ViewModels;

public sealed class HistoryViewModel
{
    // Sử dụng đúng MatchSummary theo code gốc trong máy của bạn
    public ObservableCollection<MatchSummary> Matches { get; } = new();

    // Sử dụng đúng tên hàm LoadAsync() gốc
    public async Task LoadAsync()
    {
        // Xóa danh sách cũ trước khi nạp mới
        Matches.Clear();

        // Gọi đúng hàm GetAllMatchesAsync() từ Store của bạn
        var matches = await AppServices.MatchHistoryStore.GetAllMatchesAsync();

        System.Diagnostics.Debug.WriteLine(matches.Count);

        foreach (var match in matches)
        {
            Matches.Add(new MatchSummary
            {
                PlayerX = match.PlayerXName,
                PlayerO = match.PlayerOName,
                Winner = match.WinnerName ?? "Draw",
                PlayedAt = match.EndedAtUtc ?? match.StartedAtUtc,
                MoveCount = match.Moves.Count
            });
        }
    }
}