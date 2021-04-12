using System.Collections.Generic;
using MediaBrowser.Model.Dto;

namespace Jellyfin.Plugin.KodiSyncQueue.Entities
{
    public class SyncUpdateInfo
    {
        public SyncUpdateInfo(
            IReadOnlyList<string> itemsAdded,
            IReadOnlyList<string> itemsRemoved,
            IReadOnlyList<string> itemsUpdated,
            IReadOnlyList<UserItemDataDto> userDataChanged)
        {
            ItemsAdded = itemsAdded;
            ItemsRemoved = itemsRemoved;
            ItemsUpdated = itemsUpdated;
            UserDataChanged = userDataChanged;
        }

        public IReadOnlyList<string> ItemsAdded { get; }

        public IReadOnlyList<string> ItemsRemoved { get; set; }

        public IReadOnlyList<string> ItemsUpdated { get; set; }

        public IReadOnlyList<UserItemDataDto> UserDataChanged { get; set; }
    }
}
