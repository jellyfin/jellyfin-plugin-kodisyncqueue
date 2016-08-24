using System;
using System.Collections.Generic;
using MediaBrowser.Model.Plugins;

namespace Emby.Kodi.SyncQueue.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public String RetDays { get; set; }
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
