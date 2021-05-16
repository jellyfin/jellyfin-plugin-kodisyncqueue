using System.Xml.Serialization;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.KodiSyncQueue.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public PluginConfiguration()
        {
            RetDays = "0";
            IsEnabled = true;
            TkMovies = true;
            TkTvShows = true;
            TkMusic = true;
            TkMusicVideos = true;
            TkBoxSets = true;
        }

        public string RetDays { get; set; }

        public bool IsEnabled { get; set; }

        public bool TkMovies { get; set; }

        public bool TkTvShows { get; set; }

        public bool TkMusic { get; set; }

        public bool TkMusicVideos { get; set; }

        public bool TkBoxSets { get; set; }
    }
}
