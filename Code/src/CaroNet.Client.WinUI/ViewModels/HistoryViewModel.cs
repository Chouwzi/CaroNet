using CaroNet.Client.WinUI.Models;
using CaroNet.Client.WinUI.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace CaroNet.Client.WinUI.ViewModels;

public sealed class HistoryViewModel
{
    public ObservableCollection<MatchSummary> Matches { get; }
        = new();

    public async Task LoadAsync()
    {
        Matches.Clear();

        var matches =
            await AppServices.MatchHistoryStore.GetAllMatchesAsync();

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