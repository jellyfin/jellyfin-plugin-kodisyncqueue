using NanoApi.JsonFile;
using System;

namespace Jellyfin.Plugin.KodiSyncQueue.Entities
{
    public class ItemRec
    {
        //Status 0 = Added
        //Status 1 = Updated
        //Status 2 = Removed

        [PrimaryKey]
        public int Id { get; set; }
        public Guid ItemId { get; set; }
        public long LastModified { get; set; }
        public int Status { get; set; }
        public int MediaType { get; set; }

        // 0 = Movies
        // 1 = TVShows
        // 2 = Music
        // 3 = Music Videos
        // 4 = BoxSets
    }
}
