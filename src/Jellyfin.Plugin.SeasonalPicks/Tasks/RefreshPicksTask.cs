using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.SeasonalPicks.Services;
using MediaBrowser.Model.Tasks;

namespace Jellyfin.Plugin.SeasonalPicks.Tasks;

/// <summary>Dashboard > Scheduled Tasks > "Refresh Seasonal Picks". Runs daily at 04:00 by default.</summary>
public class RefreshPicksTask : IScheduledTask
{
    private readonly PicksService _picks;

    public RefreshPicksTask(PicksService picks)
    {
        _picks = picks;
    }

    public string Name => "Refresh Seasonal Picks";

    public string Key => "SeasonalPicksRefresh";

    public string Description => "Re-picks movies for each user and updates their Suggested playlists and the mirror folder.";

    public string Category => "Seasonal Picks";

    public Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        => _picks.RefreshAllAsync(progress, cancellationToken);

    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        yield return new TaskTriggerInfo
        {
            Type = TaskTriggerInfoType.DailyTrigger,
            TimeOfDayTicks = TimeSpan.FromHours(4).Ticks
        };
    }
}
