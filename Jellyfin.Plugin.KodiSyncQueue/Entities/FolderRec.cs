
using NanoApi.JsonFile;

namespace Jellyfin.Plugin.KodiSyncQueue.Entities
{
    public class FolderRec
    {
        //Status 0 = Added
        //Status 1 = Removed

        [PrimaryKey]
        public int Id { get; set; }
        public string ItemId { get; set; }
        public string UserId { get; set; }
        public long LastModified { get; set; }
        public int Status { get; set; }          
        public string MediaType { get; set; }
        public string LibraryName { get; set; }
    }
}
