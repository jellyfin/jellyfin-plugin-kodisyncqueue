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

        private readonly Dictionary<Guid, List<BaseItem>> _changedItems = new Dictionary<Guid, List<BaseItem>>();
        private List<LibItem> _itemRef = new List<LibItem>();

        //private DbRepo Repo = null;
        private CancellationTokenSource cTokenSource = new CancellationTokenSource();

        //private DbRepo dbRepo = null;

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

            //dbRepo = new DbRepo(_applicationPaths.DataPath, _logger, _jsonSerializer);
        }

        public void Run()
        {
            _userDataManager.UserDataSaved += _userDataManager_UserDataSaved;

            _logger.Info("Emby.Kodi.SyncQueue:  UserSyncNotification Startup...");            
        }

        private bool FilterItem(BaseItem item, out int type)
        {
            type = -1;

            if (!Plugin.Instance.Configuration.IsEnabled)
            {
                return false;
            }

            if (item.LocationType == LocationType.Virtual)
            {
                return false;
            }

            if (item.SourceType != SourceType.Library)
            {
                return false;
            }


            var typeName = item.GetClientTypeName();
            if (string.IsNullOrEmpty(typeName))
            {
                return false;
            }

            switch (typeName)
            {
                //MOVIES
                case "Movie":
                    if (!Plugin.Instance.Configuration.tkMovies)
                    {
                        return false;
                    }
                    type = 0;
                    break;
                case "BoxSet":
                    if (!Plugin.Instance.Configuration.tkBoxSets)
                    {
                        return false;
                    }
                    type = 4;
                    break;
                case "Episode":
                    if (!Plugin.Instance.Configuration.tkTVShows)
                    {
                        return false;
                    }
                    type = 1;
                    break;
                case "Audio":
                    if (!Plugin.Instance.Configuration.tkMusic)
                    {
                        return false;
                    }
                    type = 2;
                    break;
                case "MusicVideo":
                    if (!Plugin.Instance.Configuration.tkMusicVideos)
                    {
                        return false;
                    }
                    type = 3;
                    break;
                default:
                    type = -1;
                    _logger.Debug(String.Format("Emby.Kodi.SyncQueue:  Ingoring Type {0}", typeName));
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

            //_logger.Debug(String.Format("Emby.Kodi.SyncQueue:  Item ID: {0}", e.Item.Id.ToString()));
            //_logger.Debug(String.Format("Emby.Kodi.SyncQueue:  JsonObject: {0}", _jsonSerializer.SerializeToString(e.Item)));
            //_logger.Debug(String.Format("Emby.Kodi.SyncQueue:  User GetClientTypeName: {0}", (e.Item as BaseItem).GetClientTypeName()));


            var cname = string.Empty;
            lock (_syncLock)
            {
                var type = -1;
                var testItem = e.Item as BaseItem;

                if (testItem != null)
                {
                    if (!FilterItem(testItem, out type))
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

                    List<BaseItem> keys;

                    var userId = e.User.Id;
                    if (!_changedItems.TryGetValue(userId, out keys))
                    {
                        keys = new List<BaseItem>();
                        _changedItems[userId] = keys;
                    }

                    keys.Add(e.Item);

                    // Go up one level for indicators
                    _itemRef.Add(new LibItem()
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

        private async Task SendNotifications(IEnumerable<KeyValuePair<Guid, List<BaseItem>>> changes, List<LibItem> itemRefs, CancellationToken cancellationToken)
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
                            var dto = _userDataManager.GetUserDataDto(i, user);
                            dto.ItemId = i.Id.ToString("N");
                            return dto;
                        })
                        .ToList();

                //_logger.Debug(String.Format("Emby.Kodi.SyncQueue:  SendNotification:  User = '{0}' dtoList = '{1}'", userId.ToString("N"), _jsonSerializer.SerializeToString(dtoList).ToString()));

                myTasks.Add(SaveUserChanges(dtoList, itemRefs, user.Name, userId.ToString("N"), cancellationToken));
            }
            Task[] iTasks = myTasks.ToArray();
            await Task.WhenAll(iTasks);
        }

        private async Task SaveUserChanges(List<MediaBrowser.Model.Dto.UserItemDataDto> dtos, List<LibItem> itemRefs, string userName, string userId, CancellationToken cancellationToken)
        {
            bool result = await Task.Run(() =>
            {
                DbRepo.Instance.SetUserInfoSync(dtos, itemRefs, userName, userId, cancellationToken);

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
