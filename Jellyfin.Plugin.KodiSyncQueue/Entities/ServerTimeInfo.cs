namespace Jellyfin.Plugin.KodiSyncQueue.Entities
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
