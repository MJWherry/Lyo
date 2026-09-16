using Lyo.Leaderboard.Web.Components;
using MudBlazor;

namespace Lyo.TestGateway.Components.TestGateway;

public partial class LeaderboardWorkbench
{
    private Guid? _boardId;
    private int _managementKey;
    private bool _seeding;

    private async Task SeedFishingAsync()
    {
        _seeding = true;
        try {
            var board = await Client.CreateBoardAsync(LeaderboardFishingBoard.BoardRequest());
            await Client.SavePropertyAsync(board.Id, LeaderboardFishingBoard.FishType(board.Id));
            await Client.SavePropertyAsync(board.Id, LeaderboardFishingBoard.Weight(board.Id));
            await Client.SavePropertyAsync(board.Id, LeaderboardFishingBoard.Length(board.Id));
            _boardId = board.Id;
            _managementKey++;
            Snackbar.Add($"Seeded '{board.Name}' ({board.Slug}).", Severity.Success);
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
        finally {
            _seeding = false;
        }
    }
}
