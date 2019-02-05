using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.KodiSyncQueue.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public string RetDays { get; }
        public bool IsEnabled { get; }
        public bool tkMovies { get; }
        public bool tkTVShows { get; }
        public bool tkMusic { get; }
        public bool tkMusicVideos { get; }
        public bool tkBoxSets { get; }

        public PluginConfiguration()
        {
            RetDays = "0";
            IsEnabled = true;
            tkMovies = true;
            tkTVShows = true;
            tkMusic = true;
            tkMusicVideos = true;
            tkBoxSets = true;
        }
    }

}
