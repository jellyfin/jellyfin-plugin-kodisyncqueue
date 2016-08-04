
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
        public string ItemId { get; set; }
        [BsonField]
        public string UserId { get; set; }
        [BsonField]
        public long LastModified { get; set; }
        [BsonField]
        public int Status { get; set; }
        [BsonField]
        public string MediaType { get; set; }
        [BsonField]
        public string LibraryName { get; set; }
    }
}
