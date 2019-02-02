using MediaBrowser.Model.Dto;
using System.Collections.Generic;

namespace Jellyfin.Plugin.KodiSyncQueue.Entities
{
    public class SyncUpdateInfo
    {
        public List<string> FoldersAddedTo { get; set; }
        public List<string> FoldersRemovedFrom { get; set; }
        public List<string> ItemsAdded { get; set; }
        public List<string> ItemsRemoved { get; set; }
        public List<string> ItemsUpdated { get; set; }
        public List<UserItemDataDto> UserDataChanged { get; set; }

        public SyncUpdateInfo()
        {
            FoldersAddedTo = new List<string>();
            FoldersRemovedFrom = new List<string>();
            ItemsAdded = new List<string>();
            ItemsRemoved = new List<string>();
            ItemsUpdated = new List<string>();
            UserDataChanged = new List<UserItemDataDto>();
        }
    }
}
