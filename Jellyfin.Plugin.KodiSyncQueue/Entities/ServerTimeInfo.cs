namespace Jellyfin.Plugin.KodiSyncQueue.Entities
{
    public class ServerTimeInfo
    {
        public ServerTimeInfo()
        {
            ServerDateTime = string.Empty;
            RetentionDateTime = string.Empty;
        }

        public string ServerDateTime { get; set; }

        public string RetentionDateTime { get; set; }
    }
}
