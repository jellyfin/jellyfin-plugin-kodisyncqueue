using System.IO;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Plugins;

namespace Emby.Kodi.SyncQueue.Configuration
{
    /// <summary>
    /// Class NextPvrConfigurationPage
    /// </summary>
    class ESQConfigurationPage : IPluginConfigurationPage
    {
        /// <summary>
        /// Gets the type of the configuration page.
        /// </summary>
        /// <value>The type of the configuration page.</value>
        public ConfigurationPageType ConfigurationPageType
        {
            get { return ConfigurationPageType.PluginConfiguration; }
        }

        /// <summary>
        /// Gets the HTML stream.
        /// </summary>
        /// <returns>Stream.</returns>
        public Stream GetHtmlStream()
        {
            return GetType().Assembly.GetManifestResourceStream("Emby.Kodi.SyncQueue.Configuration.configPage.html");
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get { return "Emby.Kodi.SyncQueue"; }
        }

        public IPlugin Plugin
        {
            get { return Emby.Kodi.SyncQueue.Plugin.Instance; }
        }
    }
}
