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

        [XmlElement("tkMovies")]
        public bool TkMovies { get; set; }

        [XmlElement("tvTVShows")]
        public bool TkTvShows { get; set; }

        [XmlElement("tkMusic")]
        public bool TkMusic { get; set; }

        [XmlElement("tkMusicVideos")]
        public bool TkMusicVideos { get; set; }

        [XmlElement("tkBoxSets")]
        public bool TkBoxSets { get; set; }
    }
}
