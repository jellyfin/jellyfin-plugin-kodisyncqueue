using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Emby.Kodi.SyncQueue.Helpers;
using System.Threading.Tasks;
using MediaBrowser.Model.Session;

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

        private readonly object _syncLock = new object();
        private Timer UpdateTimer { get; set; }
        private const int UpdateDuration = 500;

        private readonly Dictionary<Guid, List<IHasUserData>> _changedItems = new Dictionary<Guid, List<IHasUserData>>();

        private DataHelper dataHelper = null;
        private CancellationTokenSource cTokenSource = new CancellationTokenSource();

        public UserSyncNotification(IUserDataManager userDataManager, ISessionManager sessionManager, ILogger logger, IUserManager userManager, IJsonSerializer jsonSerializer, IApplicationPaths applicationPaths)
        {
            _userDataManager = userDataManager;
            _sessionManager = sessionManager;
            _logger = logger;
            _userManager = userManager;
            _jsonSerializer = jsonSerializer;
            _applicationPaths = applicationPaths;
            //dataHelper = new DataHelper(_logger, _jsonSerializer);
        }

        public void Run()
        {
            _userDataManager.UserDataSaved += _userDataManager_UserDataSaved;

            _logger.Info("Emby.Kodi.SyncQueue:  UserSyncNotification Startup...");
            dataHelper = new DataHelper(_logger, _jsonSerializer);
            string dataPath = _applicationPaths.DataPath;

            dataHelper.CheckCreateFiles(dataPath);
            dataHelper.OpenConnection();
            dataHelper.CreateUserTable("UserInfoChangedQueue", "UICQUnique");
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
            lock (_syncLock)
            try
            {
                // Remove dupes in case some were saved multiple times
                var changes = _changedItems.ToList();
                _changedItems.Clear();

                Task x = SendNotifications(changes, cTokenSource.Token);

                if (UpdateTimer != null)
                {
                    UpdateTimer.Dispose();
                    UpdateTimer = null;
                }
            }
            catch (Exception e)
            {
                _logger.Error(String.Format("Emby.Kodi.SyncQueue: An Error Has Occurred in UserUpdateTimerCallback: {0}", e.Message));
                _logger.ErrorException(e.Message, e);
            }
        }

        private async Task SendNotifications(IEnumerable<KeyValuePair<Guid, List<IHasUserData>>> changes, CancellationToken cancellationToken)
        {
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

                _logger.Debug(String.Format("Emby.Kodi.SyncQueue:  SendNotification:  User = '{0}' dtoList = '{1}'", userId.ToString("N"), _jsonSerializer.SerializeToString(dtoList).ToString()));

                await SaveUserChanges(dtoList, userId.ToString("N"), "UserInfoChangedQueue", cancellationToken);
            }
        }

        private async Task SaveUserChanges(List<MediaBrowser.Model.Dto.UserItemDataDto> dtos, string user, string tableName, CancellationToken cancellationToken)
        {           
            IEnumerable<Task<int>> LibraryAddItemQuery =
                from dto in dtos select dataHelper.UserChangeSetItem(dto, user, dto.ItemId, tableName, cancellationToken);
            
            Task<int>[] addTasks = LibraryAddItemQuery.ToArray();

            int[] itemCount = await Task.WhenAll(addTasks);            
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
                if (dataHelper != null)
                {
                    dataHelper.Dispose();
                    dataHelper = null;
                }                

                _userDataManager.UserDataSaved -= _userDataManager_UserDataSaved;
            }
        }
    }
}
