using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.KodiSyncQueue.Entities;
using Jellyfin.Plugin.KodiSyncQueue.Utils;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.KodiSyncQueue.EntryPoints
{
    public class LibrarySyncNotification : IServerEntryPoint
    {
        private const int LibraryUpdateDuration = 5000;
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger<LibrarySyncNotification> _logger;
        private readonly object _libraryChangedSyncLock = new object();
        private readonly List<LibItem> _itemsAdded = new List<LibItem>();
        private readonly List<LibItem> _itemsRemoved = new List<LibItem>();
        private readonly List<LibItem> _itemsUpdated = new List<LibItem>();
        private readonly CancellationTokenSource _cTokenSource = new CancellationTokenSource();

        public LibrarySyncNotification(ILibraryManager libraryManager, ILogger<LibrarySyncNotification> logger)
        {
            _libraryManager = libraryManager;
            _logger = logger;
        }

        private Timer LibraryUpdateTimer { get; set; }

        public Task RunAsync()
        {
            _libraryManager.ItemAdded += LibraryManager_ItemAdded;
            _libraryManager.ItemUpdated += LibraryManager_ItemUpdated;
            _libraryManager.ItemRemoved += LibraryManager_ItemRemoved;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Handles the ItemAdded event of the libraryManager control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ItemChangeEventArgs"/> instance containing the event data.</param>
        private void LibraryManager_ItemAdded(object sender, ItemChangeEventArgs e)
        {
            if (!KodiHelpers.FilterAndGetMediaType(e.Item, out var type))
            {
                return;
            }

            lock (_libraryChangedSyncLock)
            {
                if (LibraryUpdateTimer == null)
                {
                    LibraryUpdateTimer = new Timer(LibraryUpdateTimerCallback, null, LibraryUpdateDuration, Timeout.Infinite);
                }
                else
                {
                    LibraryUpdateTimer.Change(LibraryUpdateDuration, Timeout.Infinite);
                }

                var item = new LibItem
                {
                    Id = e.Item.Id,
                    SyncApiModified = (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds,
                    ItemType = type,
                };

                _logger.LogDebug("Item creation queued because {ItemId} was added to database", e.Item.Id);
                _itemsAdded.Add(item);
            }
        }

        /// <summary>
        /// Handles the ItemUpdated event of the libraryManager control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ItemChangeEventArgs"/> instance containing the event data.</param>
        private void LibraryManager_ItemUpdated(object sender, ItemChangeEventArgs e)
        {
            if (!KodiHelpers.FilterAndGetMediaType(e.Item, out var type))
            {
                return;
            }

            lock (_libraryChangedSyncLock)
            {
                if (LibraryUpdateTimer == null)
                {
                    LibraryUpdateTimer = new Timer(LibraryUpdateTimerCallback, null, LibraryUpdateDuration, Timeout.Infinite);
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

                _logger.LogDebug("Item update queued because {ItemId} was modified", e.Item.Id);
                _itemsUpdated.Add(item);
            }
        }

        /// <summary>
        /// Handles the ItemRemoved event of the libraryManager control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ItemChangeEventArgs"/> instance containing the event data.</param>
        private void LibraryManager_ItemRemoved(object sender, ItemChangeEventArgs e)
        {
            if (!KodiHelpers.FilterAndGetMediaType(e.Item, out var type))
            {
                return;
            }

            lock (_libraryChangedSyncLock)
            {
                if (LibraryUpdateTimer == null)
                {
                    LibraryUpdateTimer = new Timer(LibraryUpdateTimerCallback, null, LibraryUpdateDuration, Timeout.Infinite);
                }
                else
                {
                    LibraryUpdateTimer.Change(LibraryUpdateDuration, Timeout.Infinite);
                }

                var item = new LibItem
                {
                    Id = e.Item.Id,
                    SyncApiModified = DateTimeOffset.Now.ToUnixTimeSeconds(),
                    ItemType = type
                };

                _logger.LogDebug("Item removal queued because {ItemId} was removed from library", e.Item.Id);
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
                    _logger.LogInformation("Started library sync");
                    var startTime = DateTime.UtcNow;
                    var itemsAdded = _itemsAdded.GroupBy(i => i.Id)
                                        .Select(grp => grp.First())
                                        .ToList();
                    var itemsRemoved = _itemsRemoved.GroupBy(i => i.Id)
                                        .Select(grp => grp.First())
                                        .ToList();
                    var itemsUpdated = _itemsUpdated
                                        .Where(i => itemsAdded.FirstOrDefault(a => a.Id == i.Id) == null)
                                        .GroupBy(g => g.Id)
                                        .Select(grp => grp.First())
                                        .ToList();

                    PushChangesToDb(itemsAdded, itemsUpdated, itemsRemoved);

                    if (LibraryUpdateTimer != null)
                    {
                        LibraryUpdateTimer.Dispose();
                        LibraryUpdateTimer = null;
                    }

                    TimeSpan dateDiff = DateTime.UtcNow - startTime;
                    _logger.LogInformation("Finished library sync, taking {TimeTaken}", dateDiff.ToString("c", CultureInfo.InvariantCulture));
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "An error has occurred in LibraryUpdateTimerCallback");
                }

                _itemsAdded.Clear();
                _itemsRemoved.Clear();
                _itemsUpdated.Clear();
            }
        }

        private void PushChangesToDb(IReadOnlyCollection<LibItem> itemsAdded, IReadOnlyCollection<LibItem> itemsUpdated, IReadOnlyCollection<LibItem> itemsRemoved)
        {
            UpdateLibrary(itemsAdded, ItemStatus.Added);
            UpdateLibrary(itemsUpdated, ItemStatus.Updated);
            UpdateLibrary(itemsRemoved, ItemStatus.Removed);
        }

        private void UpdateLibrary(IReadOnlyCollection<LibItem> items, ItemStatus status)
        {
            KodiSyncQueuePlugin.Instance.DbRepo.WriteLibrarySync(items, status);
            var itemCount = items.Count;

            if (itemCount > 0)
            {
                _logger.LogInformation(
                    "Library Sync: {StatusType} {NumberOfItems} items",
                    status,
                    items.Count);

                _logger.LogDebug(
                    "Affected items: {Items}",
                    items.Select(i => i.Id.ToString("N", CultureInfo.InvariantCulture)));
            }
        }

        private void TriggerCancellation()
        {
            _cTokenSource.Cancel();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and optionally managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources or <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool dispose)
        {
            if (dispose)
            {
                if (!_cTokenSource.Token.IsCancellationRequested)
                {
                    TriggerCancellation();
                }

                if (LibraryUpdateTimer != null)
                {
                    LibraryUpdateTimer.Dispose();
                    LibraryUpdateTimer = null;
                }

                _cTokenSource.Dispose();
                _libraryManager.ItemAdded -= LibraryManager_ItemAdded;
                _libraryManager.ItemUpdated -= LibraryManager_ItemUpdated;
                _libraryManager.ItemRemoved -= LibraryManager_ItemRemoved;
            }
        }
    }
}
