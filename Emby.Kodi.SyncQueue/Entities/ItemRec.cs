using System;
using LiteDB;

namespace Emby.Kodi.SyncQueue.Entities
{
    public class ItemRec
    {
        //Status 0 = Added
        //Status 1 = Updated
        //Status 2 = Removed

        [BsonId]
        public int Id { get; set; }
        [BsonField]
        public Guid ItemId { get; set; }
        [BsonField]
        public long LastModified { get; set; }
        [BsonField]
        public int Status { get; set; }
        [BsonField]
        public int MediaType { get; set; }

        // 0 = Movies
        // 1 = TVShows
        // 2 = Music
        // 3 = Music Videos
        // 4 = BoxSets
    }
}
