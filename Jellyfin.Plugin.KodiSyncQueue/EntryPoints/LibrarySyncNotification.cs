using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.KodiSyncQueue.Data;
using Jellyfin.Plugin.KodiSyncQueue.Entities;
using MediaBrowser.Controller.Channels;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.KodiSyncQueue.EntryPoints
{
    public class LibrarySyncNotification : IServerEntryPoint
    {
        /// <summary>
        /// The _library manager
        /// </summary>
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger _logger;

        /// <summary>
        /// The _library changed sync lock
        /// </summary>
        private readonly object _libraryChangedSyncLock = new object();

        private readonly List<LibFolder> _foldersAddedTo = new List<LibFolder>();
        private readonly List<LibFolder> _foldersRemovedFrom = new List<LibFolder>();
        private readonly List<LibItem> _itemsAdded = new List<LibItem>();
        private readonly List<LibItem> _itemsRemoved = new List<LibItem>();
        private readonly List<LibItem> _itemsUpdated = new List<LibItem>();

        //private DbRepo dbRepo = null;

        private CancellationTokenSource cTokenSource = new CancellationTokenSource();

        /// <summary>
        /// Gets or sets the library update timer.
        /// </summary>
        /// <value>The library update timer.</value>
        private Timer LibraryUpdateTimer { get; set; }

        /// <summary>
        /// The library update duration
        /// </summary>
        private const int LibraryUpdateDuration = 5000;

        public LibrarySyncNotification(ILibraryManager libraryManager, ILogger logger)
        {
            _libraryManager = libraryManager;
            _logger = logger;
        }
        
        
        public void Run()
        {
            _libraryManager.ItemAdded += libraryManager_ItemAdded;
            _libraryManager.ItemUpdated += libraryManager_ItemUpdated;
            _libraryManager.ItemRemoved += libraryManager_ItemRemoved;

            _logger.LogInformation("LibrarySyncNotification Startup...");
        }

        /// <summary>
        /// Handles the ItemAdded event of the libraryManager control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ItemChangeEventArgs"/> instance containing the event data.</param>
        void libraryManager_ItemAdded(object sender, ItemChangeEventArgs e)
        {
            if (!FilterItem(e.Item, out var type))
            {
                return;
            }

            lock (_libraryChangedSyncLock)
            {
                if (LibraryUpdateTimer == null)
                {
                    LibraryUpdateTimer = new Timer(LibraryUpdateTimerCallback, null, LibraryUpdateDuration,
                                                   Timeout.Infinite);
                }
                else
                {
                    LibraryUpdateTimer.Change(LibraryUpdateDuration, Timeout.Infinite);
                }

                var item = new LibItem
                {
                    Id = e.Item.Id,
                    SyncApiModified = (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds),
                    ItemType = type,
                };
                _logger.LogDebug("ItemAdded added for DB Saving {ItemId}", e.Item.Id);
                _itemsAdded.Add(item);
                
            }
        }

        /// <summary>
        /// Handles the ItemUpdated event of the libraryManager control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ItemChangeEventArgs"/> instance containing the event data.</param>
        void libraryManager_ItemUpdated(object sender, ItemChangeEventArgs e)
        {
            if (!FilterItem(e.Item, out var type))
            {
                return;
            }

            lock (_libraryChangedSyncLock)
            {
                if (LibraryUpdateTimer == null)
                {
                    LibraryUpdateTimer = new Timer(LibraryUpdateTimerCallback, null, LibraryUpdateDuration,
                                                   Timeout.Infinite);
                }
                else
                {
                    LibraryUpdateTimer.Change(LibraryUpdateDuration, Timeout.Infinite);
                }

                var item = new LibItem
                {
                    Id = e.Item.Id,
                    SyncApiModified = (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds,
                    ItemType = type
                };

                _logger.LogDebug("ItemUpdated added for DB Saving {ItemId}", e.Item.Id);
                _itemsUpdated.Add(item);
                
            }
        }

        /// <summary>
        /// Handles the ItemRemoved event of the libraryManager control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ItemChangeEventArgs"/> instance containing the event data.</param>
        void libraryManager_ItemRemoved(object sender, ItemChangeEventArgs e)
        {
            if (!FilterRemovedItem(e.Item, out var type))
            {
                return;
            }

            lock (_libraryChangedSyncLock)
            {
                if (LibraryUpdateTimer == null)
                {
                    LibraryUpdateTimer = new Timer(LibraryUpdateTimerCallback, null, LibraryUpdateDuration,
                                                   Timeout.Infinite);
                }
                else
                {
                    LibraryUpdateTimer.Change(LibraryUpdateDuration, Timeout.Infinite);
                }

                var item = new LibItem
                {
                    Id = e.Item.Id,
                    SyncApiModified = (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds,
                    ItemType = type
                };

                _logger.LogDebug("ItemRemoved added for DB Saving {ItemId}", e.Item.Id);
                _itemsRemoved.Add(item);
                
            }
        }

        /// <summary>
        /// Libraries the update timer callback.
        /// </summary>
        /// <param name="state">The state.</param>
        private void LibraryUpdateTimerCallback(object state)
        {

            lock (_libraryChangedSyncLock)
            {

                // Remove dupes in case some were saved multiple times
                try
                {
                    _logger.LogInformation("Starting Library Sync...");
                    var startTime = DateTime.UtcNow;                    

                    var itemsAdded = _itemsAdded.GroupBy(i => i.Id).Select(grp => grp.First()).ToList();

                    var itemsRemoved = _itemsRemoved.GroupBy(i => i.Id).Select(grp => grp.First()).ToList();

                    var itemsUpdated = _itemsUpdated
                                        .Where(i => itemsAdded.FirstOrDefault(a => a.Id == i.Id) == null)
                                        .GroupBy(g => g.Id)
                                        .Select(grp => grp.First())
                                        .ToList();

                    Task pushToDbTask = PushChangesToDB(itemsAdded, itemsUpdated, itemsRemoved, cTokenSource.Token);
                    Task.WaitAll(pushToDbTask);                    

                    itemsAdded.Clear();
                    itemsRemoved.Clear();
                    itemsUpdated.Clear();

                    if (LibraryUpdateTimer != null)
                    {
                        LibraryUpdateTimer.Dispose();
                        LibraryUpdateTimer = null;
                    }
                    TimeSpan dateDiff = DateTime.UtcNow - startTime;
                    _logger.LogInformation("Finished Library Sync Taking {TimeTaken}", dateDiff.ToString("c"));

                }
                catch (Exception e)
                {
                    _logger.LogError(e, "An Error Has Occurred in LibraryUpdateTimerCallback");
                }
                _itemsAdded.Clear();
                _itemsRemoved.Clear();
                _itemsUpdated.Clear();
                _foldersAddedTo.Clear();
                _foldersRemovedFrom.Clear();                
            }
        }

        

        public async Task PushChangesToDB(List<LibItem> itemsAdded, List<LibItem> itemsUpdated, List<LibItem> itemsRemoved, CancellationToken cancellationToken)
        {
            List<Task> myTasksList = new List<Task>();

            myTasksList.Add(UpdateLibrary(itemsAdded, "ItemsAddedQueue", 0, cancellationToken));
            myTasksList.Add(UpdateLibrary(itemsUpdated, "ItemsUpdatedQueue", 1, cancellationToken));
            myTasksList.Add(UpdateLibrary(itemsRemoved, "ItemsRemovedQueue", 2, cancellationToken));

            Task[] iTasks = myTasksList.ToArray();
            await Task.WhenAll(iTasks);
        }

        public Task UpdateLibrary(List<LibItem> Items, string tableName, int status, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                var statusType = string.Empty;
                if (status == 0) { statusType = "Added"; }
                else if (status == 1) { statusType = "Updated"; }
                else { statusType = "Removed"; }
            
                DbRepo.Instance.WriteLibrarySync(Items, status, cancellationToken);

                _logger.LogInformation("\"LIBRARYSYNC\" {StatusType} {NumberOfItems} items:  {Items}", statusType, Items.Count,
                    string.Join(",", Items.Select(i => i.Id.ToString("N")).ToArray()));
            });
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
                case "Series":
                case "Season":
                case "Episode":
                    if (!Plugin.Instance.Configuration.tkTVShows)
                    {
                        return false;
                    }
                    type = 1;
                    break;
                case "Audio":
                case "MusicArtist":
                case "MusicAlbum":
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
                    _logger.LogDebug("Ingoring Type {TypeName}", typeName);
                    return false;
            }                                   

            return true;
        }

        private bool FilterRemovedItem(BaseItem item, out int type)
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

            if (item.GetTopParent() is Channel)
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
                case "Movie":
                case "Folder":
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
                case "Series":
                case "Season":
                case "Episode":
                    if (!Plugin.Instance.Configuration.tkTVShows)
                    {
                        return false;
                    }
                    type = 1;
                    break;
                case "Audio":
                case "MusicArtist":
                case "MusicAlbum":
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
                    _logger.LogDebug("Ingoring Type {TypeName}", typeName);
                    return false;
            }

            return true;
        }

        private void TriggerCancellation()
        {
            cTokenSource.Cancel();            
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (!cTokenSource.Token.IsCancellationRequested)
            {
                TriggerCancellation();
            }
            Dispose(true);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool dispose)
        {
            if (dispose)
            {
                if (LibraryUpdateTimer != null)
                {
                    LibraryUpdateTimer.Dispose();
                    LibraryUpdateTimer = null;
                }

                _libraryManager.ItemAdded -= libraryManager_ItemAdded;
                _libraryManager.ItemUpdated -= libraryManager_ItemUpdated;
                _libraryManager.ItemRemoved -= libraryManager_ItemRemoved;
            }
        }
    }
}
