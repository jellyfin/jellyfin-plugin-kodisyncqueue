
using VelocityDb;

namespace Emby.Kodi.SyncQueue.Entities
{
    public class FolderRec : OptimizedPersistable
    {
        private string itemId;
        private string userId;
        private long lastModified;
        private int status;
        //Status 0 = Added
        //Status 1 = Removed

        public string ItemId
        {
            get { return itemId; }
            set { Update(); itemId = value; }
        }

        public string UserId
        {
            get { return userId; }
            set { Update(); userId = value; }
        }

        public long LastModified
        {
            get { return lastModified; }
            set { Update(); lastModified = value; }
        }

        public int Status
        {
            get { return status; }
            set { Update(); status = value; }
        }
    }
}
