using System;

namespace Jellyfin.Plugin.KodiSyncQueue.Entities
{
    public class LibFolder
    {
        public Guid Id { get; set; }
        public long SyncApiModified { get; set; }
    }
}
