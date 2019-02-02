namespace Jellyfin.Plugin.KodiSyncQueue.Entities
{
    public class PluginSettings
    {
        public int RetentionDays { get; set; }
        public bool IsEnabled { get; set; }
        public bool TrackMovies { get; set; }
        public bool TrackTVShows { get; set; }
        public bool TrackMusic { get; set; }
        public bool TrackMusicVideos { get; set; }
        public bool TrackBoxSets { get; set; }
    }
}
