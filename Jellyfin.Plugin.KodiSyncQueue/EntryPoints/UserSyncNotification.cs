using Jellyfin.Data.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.KodiSyncQueue.Entities;
using Jellyfin.Plugin.KodiSyncQueue.Utils;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.KodiSyncQueue.EntryPoints
{
    public class UserSyncNotification : IServerEntryPoint
    {
        private readonly ILogger<UserSyncNotification> _logger;
        private readonly IUserDataManager _userDataManager;
        private readonly IUserManager _userManager;

        private readonly object _syncLock = new object();
        private Timer UpdateTimer { get; set; }
        private const int UpdateDuration = 500;

        private readonly Dictionary<Guid, List<BaseItem>> _changedItems = new Dictionary<Guid, List<BaseItem>>();
        private List<LibItem> _itemRef = new List<LibItem>();

        private CancellationTokenSource cTokenSource = new CancellationTokenSource();

        public UserSyncNotification(IUserDataManager userDataManager, ILogger<UserSyncNotification> logger, IUserManager userManager)
        {
            _userDataManager = userDataManager;
            _logger = logger;
            _userManager = userManager;
        }

        public Task RunAsync()
        {
            _userDataManager.UserDataSaved += _userDataManager_UserDataSaved;

            _logger.LogInformation("UserSyncNotification Startup...");
            return Task.CompletedTask;
        }

        void _userDataManager_UserDataSaved(object sender, UserDataSaveEventArgs e)
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
                    if (!Helpers.FilterAndGetMediaType(testItem, out var type))
                    {
                        return;
                    }

                    if (UpdateTimer == null)
                    {
                        UpdateTimer = new Timer(UpdateTimerCallback, null, UpdateDuration,
                                                       Timeout.Infinite);
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

                    var parent = testItem.Parent;

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
            try
            {
                _logger.LogInformation("Starting User Changes Sync...");
                var startDate = DateTime.UtcNow;

                // Remove dupes in case some were saved multiple times
                var changes = _changedItems.ToList();
                var itemRef = _itemRef.ToList();
                _changedItems.Clear();
                _itemRef.Clear();

                SendNotifications(changes, itemRef, cTokenSource.Token);

                if (UpdateTimer != null)
                {
                    UpdateTimer.Dispose();
                    UpdateTimer = null;
                }

                TimeSpan dateDiff = DateTime.UtcNow - startDate;
                _logger.LogInformation("User Changes Sync Finished Taking {TimeTaken}", dateDiff.ToString("c"));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An Error Has Occurred in UserUpdateTimerCallback");
            }
        }

        private void SendNotifications(IEnumerable<KeyValuePair<Guid, List<BaseItem>>> changes, List<LibItem> itemRefs, CancellationToken cancellationToken)
        {
            foreach (var pair in changes)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var userId = pair.Key;
                _logger.LogDebug("Starting to save items for {userId}", userId.ToString());

                var user = _userManager.GetUserById(userId);

                var dtoList = pair.Value
                        .GroupBy(i => i.Id)
                        .Select(i => i.First())
                        .Select(i =>
                        {
                            var dto = _userDataManager.GetUserDataDto(i, user);
                            dto.ItemId = i.Id.ToString("N");
                            return dto;
                        })
                        .ToList();

                SaveUserChanges(dtoList, itemRefs, user.Username, userId.ToString("N"));
            }
        }

        private void SaveUserChanges(List<MediaBrowser.Model.Dto.UserItemDataDto> dtos, List<LibItem> itemRefs, string userName, string userId)
        {
            Plugin.Instance.DbRepo.SetUserInfoSync(dtos, itemRefs, userId);
            List<string> ids = dtos.Select(s => s.ItemId).ToList();

            _logger.LogInformation("\"USERSYNC\" User {UserId}({Username}) posted {NumberOfUpdates} Updates: {Updates}", userId, userName, ids.Count, string.Join(",", ids.ToArray()));
        }

        private void TriggerCancellation()
        {            
            cTokenSource.Cancel();
        }

        public void Dispose()
        {
            if (!cTokenSource.Token.IsCancellationRequested)
            {
                TriggerCancellation();
            }

            Dispose(true);
        }

        protected virtual void Dispose(bool dispose)
        {
            if (dispose)
            {
                if (UpdateTimer != null)
                {
                    UpdateTimer.Dispose();
                    UpdateTimer = null;
                }

                _userDataManager.UserDataSaved -= _userDataManager_UserDataSaved;
            }
        }
    }
}
