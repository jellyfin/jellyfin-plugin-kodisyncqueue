using System;
using System.Reflection;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using Emby.Kodi.SyncQueue.Configuration;

namespace Emby.Kodi.SyncQueue
{
    class Plugin : BasePlugin<PluginConfiguration>
    {
        public static ILogger Logger { get; set; }

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, ILogger logger)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;

            Logger = logger;
            Logger.Info("Emby.Kodi.SyncQueue IS NOW STARTING!!!");

            try
            {
                string[] names = this.GetType().Assembly.GetManifestResourceNames();
                foreach (string name in names)
                {
                    logger.Info(String.Format("Resource: \"{0}\"", name));
                }

                string resource1 = "Emby.Kodi.SyncQueue.LiteDB.dll";

                EmbeddedAssembly.Load(resource1, "LiteDB.dll");

                AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
            }
            catch (Exception ex)
            {
                Logger.Error("Error Loading LiteDB.dll", ex);
            }                        
        }

        public Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            return EmbeddedAssembly.Get(args.Name);
        }

        /// <summary>
        /// Gets the name of the plugin
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get { return "Emby.Kodi Sync Queue"; }
        }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        public override string Description
        {
            get
            {
                return "Allows for shorter Sync Times on Kodi Startup";
            }
        }

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>The instance.</value>
        public static Plugin Instance { get; private set; }
    }
}
