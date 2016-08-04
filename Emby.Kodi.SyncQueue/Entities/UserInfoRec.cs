
using LiteDB;

namespace Emby.Kodi.SyncQueue.Entities
{
    public class UserInfoRec
    {
        [BsonId]
        public int Id { get; set; }
        [BsonField]
        public string ItemId { get; set; }
        [BsonField]
        public string UserId { get; set; }
        [BsonField]
        public long LastModified { get; set; }
        [BsonField]
        public string Json { get; set; }
        [BsonField]
        public string MediaType { get; set; }
        [BsonField]
        public string LibraryName { get; set; }
    }
}
