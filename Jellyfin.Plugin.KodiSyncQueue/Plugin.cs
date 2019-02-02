using System;
using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Serialization;
using Jellyfin.Plugin.KodiSyncQueue.Configuration;
using MediaBrowser.Model.Plugins;
using Jellyfin.Plugin.KodiSyncQueue.Data;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.KodiSyncQueue
{
    class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        public static ILogger Logger { get; set; }

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, ILogger logger, IJsonSerializer json, IFileSystem fileSystem)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;

            Logger = logger;

            //Logger.LogInformation("Write Out Reference if it Doesn't Exist!");

            //if (!File.Exists(Path.Combine(applicationPaths.ProgramSystemPath, "Emby.Kodi.NanoApi.dll")) ||
            //    !File.Exists(Path.Combine(applicationPaths.ProgramSystemPath, "Emby.Kodi.SyncJson.dll")))
            //{
            //    var names = Assembly.GetEntryAssembly().GetManifestResourceNames();
            //    foreach (var name in names)
            //    {
            //        Logger.LogInformation("" + name);
            //    }
            //    using (Stream input = Assembly.GetExecutingAssembly().GetManifestResourceStream("Jellyfin.Plugin.KodiSyncQueue.Resources.Emby.Kodi.NanoApi.dll"))
            //    using (Stream output = File.Create(Path.Combine(applicationPaths.ProgramSystemPath, "NanoApi.JsonFile.dll")))
            //    {
            //        CopyStream(input, output);
            //    }

            //    using (Stream input = Assembly.GetExecutingAssembly().GetManifestResourceStream("Jellyfin.Plugin.KodiSyncQueue.Resources.Emby.Kodi.SyncJson.dll"))
            //    using (Stream output = File.Create(Path.Combine(applicationPaths.ProgramSystemPath, "NanoApi.JsonFile.dll")))
            //    {
            //        CopyStream(input, output);
            //    }
            //}

            Logger.LogInformation("Jellyfin.Plugin.KodiSyncQueue IS NOW STARTING!!!");

            DbRepo.dbPath = applicationPaths.DataPath;
            DbRepo.json = json;
            DbRepo.logger = logger;
            DbRepo.fileSystem = fileSystem;
            DbRepo.Instance.Initialize();
        }

        private Guid _id = new Guid("b0daa30f-2e09-4083-a6ce-459d9fecdd80");
        public override Guid Id
        {
            get { return _id; }
        }

        /// <summary>
        /// Gets the name of the plugin
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get { return "Kodi companion"; }
        }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        public override string Description
        {
            get
            {
                return "Companion for Kodi add-ons. Provides dynamic strms and shorter sync times for Emby for Kodi.";
            }
        }

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>The instance.</value>
        public static Plugin Instance { get; private set; }

        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new PluginPageInfo[]
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
