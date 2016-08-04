using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Kodi.SyncQueue.Data;
using Emby.Kodi.SyncQueue.Entities;

namespace Emby.Kodi.SyncQueue.EntryPoints
{
    class UserSyncNotification : IServerEntryPoint
    {
        private readonly ISessionManager _sessionManager;
        private readonly ILogger _logger;
        private readonly IUserDataManager _userDataManager;
        private readonly IUserManager _userManager;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IApplicationPaths _applicationPaths;
        private readonly ILibraryManager _libraryManager;

        private readonly object _syncLock = new object();
        private Timer UpdateTimer { get; set; }
        private const int UpdateDuration = 500;

        private readonly Dictionary<Guid, List<IHasUserData>> _changedItems = new Dictionary<Guid, List<IHasUserData>>();
        private List<LibItem> _itemRef = new List<LibItem>();

        //private DbRepo Repo = null;
        private CancellationTokenSource cTokenSource = new CancellationTokenSource();

        public UserSyncNotification(ILibraryManager libraryManager, IUserDataManager userDataManager, ISessionManager sessionManager, ILogger logger, IUserManager userManager, IJsonSerializer jsonSerializer, IApplicationPaths applicationPaths)
        {
            _userDataManager = userDataManager;
            _sessionManager = sessionManager;
            _logger = logger;
            _userManager = userManager;
            _jsonSerializer = jsonSerializer;
            _applicationPaths = applicationPaths;
            _libraryManager = libraryManager;
            //dataHelper = new DataHelper(_logger, _jsonSerializer);
        }

        public void Run()
        {
            _userDataManager.UserDataSaved += _userDataManager_UserDataSaved;

            _logger.Info("Emby.Kodi.SyncQueue:  UserSyncNotification Startup...");            
        }

        private bool FilterItem(BaseItem item, out string type, out string cname)
        {
            type = string.Empty;
            cname = string.Empty;
            if (item.LocationType == LocationType.Virtual)
            {
                return false;
            }
            else if (String.IsNullOrEmpty(item.GetClientTypeName()) == false &&
                item.GetClientTypeName().Equals("Person", StringComparison.InvariantCultureIgnoreCase))
            {
                _logger.Debug(String.Format("Emby.Kodi.SyncQueue:  Ignorning item type Person"));
                return false;
            }
            if (item.SourceType != SourceType.Library)
            {
                return false;
            }

            var cn = _libraryManager.GetCollectionFolders(item).FirstOrDefault();
            if (cn != null) { cname = cn.Name; }
            else { cname = string.Empty; }

            var ids = item.GetAncestorIds().ToList();
            foreach (var id in ids)
            {
                var cf = _libraryManager.GetItemById(id) as ICollectionFolder;
                if (cf != null && cf.CollectionType != null && cf.CollectionType != "")
                {
                    if (cf.CollectionType == "movies" || cf.CollectionType == "tvshows" || cf.CollectionType == "music" || cf.CollectionType == "musicvideos" || cf.CollectionType == "boxsets")
                    {
                        type = cf.CollectionType;
                        break;
                    }
                }
            }

            if (type == string.Empty)
            {
                return false;
            }

            return true;
        }

        void _userDataManager_UserDataSaved(object sender, UserDataSaveEventArgs e)
        {
            if (e.SaveReason == UserDataSaveReason.PlaybackProgress)
            {
                return;
            }

            var cname = string.Empty;
            lock (_syncLock)
            {
                var type = string.Empty;
                var testItem = e.Item as BaseItem;

                if (testItem != null)
                {
                    if (!FilterItem(testItem, out type, out cname))
                    {
                        return;
                    }
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

                List<IHasUserData> keys;

                if (!_changedItems.TryGetValue(e.UserId, out keys))
                {
                    keys = new List<IHasUserData>();
                    _changedItems[e.UserId] = keys;
                }

                keys.Add(e.Item);
                
                var baseItem = e.Item as BaseItem;

                // Go up one level for indicators
                if (baseItem != null)
                {
                    _itemRef.Add(new LibItem()
                    {
                        Id = baseItem.Id,
                        ItemType = type,
                        CollectionName = cname
                    });

                    var parent = baseItem.Parent;

                    if (parent != null)
                    {
                        keys.Add(parent);
                    }
                }
            }
        }

        private void UpdateTimerCallback(object state)
        {
            if (!Plugin.Instance.Configuration.IsEnabled)
            {
                return;
            }

            lock (_syncLock)
            try
            {
                _logger.Info("Emby.Kodi.SyncQueue: Starting User Changes Sync...");
                var startDate = DateTime.UtcNow;

                // Remove dupes in case some were saved multiple times
                var changes = _changedItems.ToList();
                var itemRef = _itemRef.ToList();
                _changedItems.Clear();
                _itemRef.Clear();

                Task x = SendNotifications(changes, itemRef, cTokenSource.Token);
                Task.WaitAll(x);

                if (UpdateTimer != null)
                {
                    UpdateTimer.Dispose();
                    UpdateTimer = null;
                }
                TimeSpan dateDiff = DateTime.UtcNow - startDate;
                _logger.Info(String.Format("Emby.Kodi.SyncQueue: User Changes Sync Finished Taking {0}", dateDiff.ToString("c")));
            }
            catch (Exception e)
            {
                _logger.Error(String.Format("Emby.Kodi.SyncQueue: An Error Has Occurred in UserUpdateTimerCallback: {0}", e.Message));
                _logger.ErrorException(e.Message, e);
            }
        }

        private async Task SendNotifications(IEnumerable<KeyValuePair<Guid, List<IHasUserData>>> changes, List<LibItem> itemRefs, CancellationToken cancellationToken)
        {
            List<Task> myTasks = new List<Task>();
            
            foreach (var pair in changes)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var userId = pair.Key;
                _logger.Debug(String.Format("Emby.Kodi.SyncQueue:  Starting to save items for {0}", userId.ToString()));

                var user = _userManager.GetUserById(userId);

                var dtoList = pair.Value
                        .GroupBy(i => i.Id)
                        .Select(i => i.First())
                        .Select(i =>
                        {
                            var dto = _userDataManager.GetUserDataDto(i, user).Result;
                            dto.ItemId = i.Id.ToString("N");
                            return dto;
                        })
                        .ToList();

                _logger.Debug(String.Format("Emby.Kodi.SyncQueue:  SendNotification:  User = '{0}' dtoList = '{1}'", userId.ToString("N"), _jsonSerializer.SerializeToString(dtoList).ToString()));

                myTasks.Add(SaveUserChanges(dtoList, itemRefs, user.Name, userId.ToString("N"), cancellationToken));
            }
            Task[] iTasks = myTasks.ToArray();
            await Task.WhenAll(iTasks);
        }

        private async Task SaveUserChanges(List<MediaBrowser.Model.Dto.UserItemDataDto> dtos, List<LibItem> itemRefs, string userName, string userId, CancellationToken cancellationToken)
        {
            bool result = await Task.Run(() =>
            {
                using (var repo = new DbRepo(_applicationPaths.DataPath, _logger, _jsonSerializer))
                {
                    repo.SetUserInfoSync(dtos, itemRefs, userName, userId, cancellationToken);
                }
                return true;
            });
            
            List<string> ids = dtos.Select(s => s.ItemId).ToList();

            _logger.Info(String.Format("Emby.Kodi.SyncQueue: \"USERSYNC\" User {0}({1}) posted {2} Updates:  {3}", userId, userName, ids.Count(), String.Join(",", ids.ToArray())));
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
                Thread.Sleep(2000);
                cTokenSource.Dispose();
                cTokenSource = null;
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
