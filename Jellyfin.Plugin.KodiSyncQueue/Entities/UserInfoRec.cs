
using NanoApi.JsonFile;

namespace Jellyfin.Plugin.KodiSyncQueue.Entities
{
    public class UserInfoRec
    {
        [PrimaryKey]
        public int Id { get; set; }
        public string ItemId { get; set; }
        public string UserId { get; set; }
        public long LastModified { get; set; }
        public string Json { get; set; }
        public int MediaType { get; set; }

        // 0 = Movies
        // 1 = TVShows
        // 2 = Music
        // 3 = Music Videos
        // 4 = BoxSets
    }
}
