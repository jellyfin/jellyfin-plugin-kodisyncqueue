
using VelocityDb;

namespace Emby.Kodi.SyncQueue.Entities
{
    public class UserInfoRec : OptimizedPersistable
    {
        private string itemId;
        private string userId;
        private long lastModified;
        private string json;
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

        public string Json
        {
            get { return json; }
            set { Update(); json = value; }
        }
    }
}
