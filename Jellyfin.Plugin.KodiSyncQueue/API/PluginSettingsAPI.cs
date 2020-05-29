using Jellyfin.Plugin.KodiSyncQueue.Entities;
using MediaBrowser.Model.Services;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.KodiSyncQueue.API
{
    public class PluginSettingsAPI : IService
    {
        private readonly ILogger<PluginSettingsAPI> _logger;

        public PluginSettingsAPI(ILogger<PluginSettingsAPI> logger)
        {
            _logger = logger;
        }

        public PluginSettings Get(GetPluginSettings request)
        {
            _logger.LogInformation("Plugin Settings Requested...");
            var settings = new PluginSettings();
            _logger.LogDebug("Class Variable Created!");

            _logger.LogDebug("Creating Settings Object Variables!");

            if (!int.TryParse(Plugin.Instance.Configuration.RetDays, out var retDays))
            {
                retDays = 0;
            }

            settings.RetentionDays = retDays;
            settings.IsEnabled = Plugin.Instance.Configuration.IsEnabled;
            settings.TrackMovies = Plugin.Instance.Configuration.tkMovies;
            settings.TrackTVShows = Plugin.Instance.Configuration.tkTVShows;
            settings.TrackBoxSets = Plugin.Instance.Configuration.tkBoxSets;
            settings.TrackMusic = Plugin.Instance.Configuration.tkMusic;
            settings.TrackMusicVideos = Plugin.Instance.Configuration.tkMusicVideos;
            
            _logger.LogDebug("Sending Settings Object Back.");

            return settings;
        }
    }
}
