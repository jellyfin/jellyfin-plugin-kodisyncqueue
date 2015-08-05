using System;
using System.Collections.Generic;
using MediaBrowser.Model.Plugins;

namespace Emby.Kodi.SyncQueue.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public SyncQueueOptions[] Options { get; set; }

        public PluginConfiguration()
        {
            Options = new SyncQueueOptions[] { };
        }
    }

    public class SyncQueueOptions
    {
        public Boolean Enabled { get; set; }
        public String Token { get; set; }
    }

}
