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
            //Repo = new DbRepo(_applicationPaths.DataPath);            
            if (DbRepo.DataPath == null)
            {
                DbRepo.DataPath = _applicationPaths.DataPath;
            }
        }

        void _userDataManager_UserDataSaved(object sender, UserDataSaveEventArgs e)
        {
            if (e.SaveReason == UserDataSaveReason.PlaybackProgress)
            {
                return;
            }

            lock (_syncLock)
            {
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
                _changedItems.Clear();

                Task x = SendNotifications(changes, cTokenSource.Token);
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

        private async Task SendNotifications(IEnumerable<KeyValuePair<Guid, List<IHasUserData>>> changes, CancellationToken cancellationToken)
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

                myTasks.Add(SaveUserChanges(dtoList, user.Name, userId.ToString("N"), cancellationToken));
            }
            Task[] iTasks = myTasks.ToArray();
            await Task.WhenAll(iTasks);
        }

        private async Task SaveUserChanges(List<MediaBrowser.Model.Dto.UserItemDataDto> dtos, string userName, string userId, CancellationToken cancellationToken)
        {
            bool result = await Task.Run(() =>
            {
                DbRepo.SetUserInfoSync(dtos, userName, userId, cancellationToken, _logger, _jsonSerializer);
                return true;
            });
            
            List<string> ids = dtos.Select(s => s.ItemId).ToList();

            _logger.Info(String.Format("Emby.Kodi.SyncQueue: \"USERSYNC\" User {0}({1}) posted {2} Updates:  {3}", userId, userName, ids.Count(), String.Join(",", ids.ToArray())));
            //_logger.Info(String.Format("Emby.Kodi.SyncQueue: Item Id's: {0}", String.Join(",", itemIds.ToArray())));
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
                //if (Repo != null)
                //{
                //    Repo.Dispose();
                //    Repo = null;
                //}                

                _userDataManager.UserDataSaved -= _userDataManager_UserDataSaved;
            }
        }
    }
}
