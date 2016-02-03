using System;
using System.Collections.Generic;
using MediaBrowser.Model.Plugins;

namespace Emby.Kodi.SyncQueue.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public String RetDays { get; set; }
  
        public PluginConfiguration()
        {
            RetDays = "0";
        }
    }

}
