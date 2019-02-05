using Jellyfin.Plugin.KodiSyncQueue.Entities;
using System;
using System.Globalization;
using MediaBrowser.Model.Services;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.KodiSyncQueue.API
{
    public class ServerTimeAPI : IService
    {
        private readonly ILogger _logger;

        public ServerTimeAPI(ILogger logger)
        {
            _logger = logger;
        }

        public ServerTimeInfo Get(GetServerTime request)
        {
            _logger.LogInformation("Server Time Requested...");
            var info = new ServerTimeInfo();
            _logger.LogDebug("Class Variable Created!");
            DateTime dtNow = DateTime.UtcNow;
            DateTime retDate;

            if (!int.TryParse(Plugin.Instance.Configuration.RetDays, out var retDays))
            {
                retDays = 0;
            }

            if (retDays == 0)
            {
                retDate = new DateTime(1900, 1, 1, 0, 0, 0);
            }
            else
            {
                retDate = new DateTime(dtNow.Year, dtNow.Month, dtNow.Day, 0, 0, 0);
                retDate = retDate.AddDays(-retDays);
            }
            _logger.LogDebug("Getting Ready to Set Variables!");
            info.ServerDateTime = $"{DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)}";
            info.RetentionDateTime = $"{retDate.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)}";

            _logger.LogDebug("ServerDateTime = {ServerDateTime}, RetentionDateTime = {RetentionDateTime}", info.ServerDateTime, info.RetentionDateTime);

            return info;
        }
    }
}
