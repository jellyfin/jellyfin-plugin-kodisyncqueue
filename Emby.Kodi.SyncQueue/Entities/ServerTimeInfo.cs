namespace Emby.Kodi.SyncQueue.Entities
{
    public class ServerTimeInfo
    {
        public string ServerDateTime { get; set; }
        public string RetentionDateTime { get; set; }

        public ServerTimeInfo()
        {
            ServerDateTime = "";
            RetentionDateTime = "";
        }
    }
}
