using System;

namespace Jellyfin.Plugin.KodiSyncQueue.Entities
{
    public class LibItem
    {
        public Guid Id { get; set; }

        public long SyncApiModified { get; set; }

        public MediaType ItemType { get; set; }
    }
}
