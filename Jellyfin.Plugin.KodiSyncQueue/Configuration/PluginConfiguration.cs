using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.KodiSyncQueue.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public string RetDays { get; set; }

        public bool IsEnabled { get; set; }

        public bool tkMovies { get; set; }

        public bool tkTVShows { get; set; }

        public bool tkMusic { get; set; }

        public bool tkMusicVideos { get; set; }

        public bool tkBoxSets { get; set; }

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
