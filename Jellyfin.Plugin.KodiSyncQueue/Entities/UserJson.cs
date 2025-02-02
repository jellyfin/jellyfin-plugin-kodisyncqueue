using System;

namespace Jellyfin.Plugin.KodiSyncQueue.Entities
{
    public class UserJson
    {
        public Guid Id { get; set; }

        public string JsonData { get; set; }
    }
}
