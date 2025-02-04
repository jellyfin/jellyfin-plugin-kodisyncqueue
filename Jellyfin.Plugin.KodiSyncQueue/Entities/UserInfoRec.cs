using System;

namespace Jellyfin.Plugin.KodiSyncQueue.Entities
{
    public class UserInfoRec
    {
        public int Id { get; set; }

        public Guid ItemId { get; set; }

        public string UserId { get; set; }

        public long LastModified { get; set; }

        public string Json { get; set; }

        public MediaType MediaType { get; set; }
    }
}
