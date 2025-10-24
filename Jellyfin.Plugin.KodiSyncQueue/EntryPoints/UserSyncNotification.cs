using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.KodiSyncQueue.Entities;
using Jellyfin.Plugin.KodiSyncQueue.Utils;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.KodiSyncQueue.EntryPoints
{
    public class UserSyncNotification : IHostedService
    {
        private const int UpdateDuration = 500;
        private readonly ILogger<UserSyncNotification> _logger;
        private readonly IUserDataManager _userDataManager;
        private readonly IUserManager _userManager;
        private readonly object _syncLock = new object();
        private readonly Dictionary<Guid, List<BaseItem>> _changedItems = new Dictionary<Guid, List<BaseItem>>();
        private readonly List<LibItem> _itemRef = new List<LibItem>();
        private readonly CancellationTokenSource _cTokenSource = new CancellationTokenSource();

        public UserSyncNotification(IUserDataManager userDataManager, ILogger<UserSyncNotification> logger, IUserManager userManager)
        {
            _userDataManager = userDataManager;
            _logger = logger;
            _userManager = userManager;
        }

        private Timer UpdateTimer { get; set; }

        private void UserDataManager_UserDataSaved(object sender, UserDataSaveEventArgs e)
        {
            if (e.SaveReason == UserDataSaveReason.PlaybackProgress)
            {
                return;
            }

            lock (_syncLock)
            {
                var testItem = e.Item;

                if (testItem != null)
                {
                    if (!KodiHelpers.FilterAndGetMediaType(testItem, out var type))
                    {
                        return;
                    }

                    if (UpdateTimer == null)
                    {
                        UpdateTimer = new Timer(UpdateTimerCallback, null, UpdateDuration, Timeout.Infinite);
                    }
                    else
                    {
                        UpdateTimer.Change(UpdateDuration, Timeout.Infinite);
                    }

                    if (!_changedItems.TryGetValue(e.UserId, out var keys))
                    {
                        keys = new List<BaseItem>();
                        _changedItems[e.UserId] = keys;
                    }

                    keys.Add(e.Item);

                    // Go up one level for indicators
                    _itemRef.Add(new LibItem
                    {
                        Id = testItem.Id,
                        ItemType = type,
                    });

                    var parent = testItem.GetParent();

                    if (parent != null)
                    {
                        keys.Add(parent);
                    }
                }
            }
        }

        private void UpdateTimerCallback(object state)
        {
            lock (_syncLock)
            {
                try
                {
                    _logger.LogInformation("Started user data sync");
                    var startDate = DateTime.UtcNow;

                    // Remove dupes in case some were saved multiple times
                    var changes = _changedItems.ToList();
                    var itemRef = _itemRef.ToList();
                    _changedItems.Clear();
                    _itemRef.Clear();

                    SendNotifications(changes, itemRef, _cTokenSource.Token);

                    if (UpdateTimer != null)
                    {
                        UpdateTimer.Dispose();
                        UpdateTimer = null;
                    }

                    TimeSpan dateDiff = DateTime.UtcNow - startDate;
                    _logger.LogInformation("Finished user data sync, taking {TimeTaken}", dateDiff.ToString("c", CultureInfo.InvariantCulture));
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "An error has occurred in UserUpdateTimerCallback");
                }
            }
        }

        private void SendNotifications(IEnumerable<KeyValuePair<Guid, List<BaseItem>>> changes, List<LibItem> itemRefs, CancellationToken cancellationToken)
        {
            foreach (var pair in changes)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var userId = pair.Key;
                _logger.LogDebug("Started saving items for {userId}", userId);

                var user = _userManager.GetUserById(userId);

                var dtoList = pair.Value
                        .GroupBy(i => i.Id)
                        .Select(i => i.First())
                        .Select(i =>
                        {
                            var dto = _userDataManager.GetUserDataDto(i, user);
                            dto.ItemId = i.Id;
                            return dto;
                        })
                        .ToList();

                SaveUserChanges(dtoList, itemRefs, user.Username, userId);
            }
        }

        private void SaveUserChanges(List<MediaBrowser.Model.Dto.UserItemDataDto> dtos, List<LibItem> itemRefs, string userName, Guid userId)
        {
            KodiSyncQueuePlugin.Instance.DbRepo.SetUserInfoSync(dtos, itemRefs, userId);
            List<Guid> ids = dtos.Select(s => s.ItemId).ToList();
            var itemCount = ids.Count;

            if (itemCount > 0)
            {
                _logger.LogInformation(
                    "User Data Sync: User {UserName} ({UserId}) posted {NumberOfUpdates} updates",
                    userName,
                    userId,
                    itemCount);

                _logger.LogDebug(
                    "Updated items: {Updates}",
                    ids);
            }
        }

        private void TriggerCancellation()
        {
            _cTokenSource.Cancel();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _userDataManager.UserDataSaved += UserDataManager_UserDataSaved;

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            if (!_cTokenSource.Token.IsCancellationRequested)
            {
                TriggerCancellation();
            }

            if (UpdateTimer != null)
            {
                UpdateTimer.Dispose();
                UpdateTimer = null;
            }

            _cTokenSource.Dispose();
            _userDataManager.UserDataSaved -= UserDataManager_UserDataSaved;
            return Task.CompletedTask;
        }
    }
}
