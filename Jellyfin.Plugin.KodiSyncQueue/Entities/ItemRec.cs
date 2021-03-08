using System;

namespace Jellyfin.Plugin.KodiSyncQueue.Entities
{
    public class ItemRec
    {
        public int Id { get; set; }

        public Guid ItemId { get; set; }

        public long LastModified { get; set; }

        public ItemStatus Status { get; set; }

        public MediaType MediaType { get; set; }
    }
}
