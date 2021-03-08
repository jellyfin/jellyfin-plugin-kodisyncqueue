using System.Collections.Generic;
using MediaBrowser.Model.Dto;

namespace Jellyfin.Plugin.KodiSyncQueue.Entities
{
    public class SyncUpdateInfo
    {
        public SyncUpdateInfo()
        {
            ItemsAdded = new List<string>();
            ItemsRemoved = new List<string>();
            ItemsUpdated = new List<string>();
            UserDataChanged = new List<UserItemDataDto>();
        }

        public List<string> ItemsAdded { get; set; }

        public List<string> ItemsRemoved { get; set; }

        public List<string> ItemsUpdated { get; set; }

        public List<UserItemDataDto> UserDataChanged { get; set; }
    }
}
