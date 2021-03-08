using System;
using System.Collections.Generic;
using Jellyfin.Plugin.KodiSyncQueue.Configuration;
using Jellyfin.Plugin.KodiSyncQueue.Data;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.KodiSyncQueue
{
    public class KodiSyncQueuePlugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        public KodiSyncQueuePlugin(
            IApplicationPaths applicationPaths,
            IXmlSerializer xmlSerializer,
            ILogger<DbRepo> logger)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;

            logger.LogInformation("Jellyfin.Plugin.KodiSyncQueue is now starting");

            DbRepo = new DbRepo(applicationPaths.DataPath, logger);
        }

        public DbRepo DbRepo { get; }

        public override Guid Id => new Guid("771e19d6-5385-4caf-b35c-28a0e865cf63");

        /// <summary>
        /// Gets the name of the plugin
        /// </summary>
        /// <value>The name.</value>
        public override string Name => "Kodi Sync Queue";

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        public override string Description
            => "Companion plugin that provides dynamic stream files and shorter sync times while using Jellyfin for Kodi.";

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>The instance.</value>
        public static KodiSyncQueuePlugin Instance { get; private set; }

        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = "Jellyfin.Plugin.KodiSyncQueue",
                    EmbeddedResourcePath = "Jellyfin.Plugin.KodiSyncQueue.Configuration.configPage.html"
                }
            };
        }
    }
}
