using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.SeasonalPicks.Services;

/// <summary>
/// Swaps a pick out as soon as its user finishes it (or marks it played), so the For You playlist on the TV
/// doesn't keep offering a film that was just watched.
/// </summary>
public class WatchedListener : IHostedService
{
    private readonly IUserDataManager _userData;
    private readonly IUserManager _users;
    private readonly PicksService _picks;
    private readonly ILogger<WatchedListener> _logger;
    private readonly CancellationTokenSource _stopping = new();

    public WatchedListener(IUserDataManager userData, IUserManager users, PicksService picks, ILogger<WatchedListener> logger)
    {
        _userData = userData;
        _users = users;
        _picks = picks;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _userData.UserDataSaved += OnUserDataSaved;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _userData.UserDataSaved -= OnUserDataSaved;
        _stopping.Cancel();
        return Task.CompletedTask;
    }

    /// <summary>Only a finished playback or a manual "mark played" on a movie counts (not progress or imports).</summary>
    public static bool IsWatchedEvent(UserDataSaveReason reason, bool played, object? item)
        => item is Movie
            && played
            && reason is UserDataSaveReason.PlaybackFinished or UserDataSaveReason.TogglePlayed;

    private void OnUserDataSaved(object? sender, UserDataSaveEventArgs e)
    {
        if (Plugin.Instance?.Configuration.ReplaceWatchedPicks != true
            || !IsWatchedEvent(e.SaveReason, e.UserData?.Played == true, e.Item)
            || _users.GetUserById(e.UserId) is not { } user)
        {
            return;
        }

        var item = e.Item;

        // Off the event thread: rebuilding picks reads the whole library.
        _ = Task.Run(
            async () =>
            {
                try
                {
                    await _picks.ReplaceWatchedAsync(user, item, _stopping.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "SeasonalPicks: could not replace watched pick {Movie} for {User}", item.Name, user.Username);
                }
            },
            _stopping.Token);
    }
}
