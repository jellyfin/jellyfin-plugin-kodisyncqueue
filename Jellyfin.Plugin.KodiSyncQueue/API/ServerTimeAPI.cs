using Jellyfin.Plugin.KodiSyncQueue.Entities;
using MediaBrowser.Model.Serialization;
using System;
using System.Globalization;
using MediaBrowser.Model.Services;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.KodiSyncQueue.API
{
    public class ServerTimeAPI : IService
    {
        private readonly ILogger _logger;
        private readonly IJsonSerializer _jsonSerializer;

        public ServerTimeAPI(ILogger logger, IJsonSerializer jsonSerializer)
        {
            _logger = logger;
            _jsonSerializer = jsonSerializer;
        }

        public ServerTimeInfo Get(GetServerTime request)
        {
            _logger.LogInformation("Server Time Requested...");
            var info = new ServerTimeInfo();
            _logger.LogDebug("Class Variable Created!");
            int retDays = 0;
            DateTime dtNow = DateTime.UtcNow;
            DateTime retDate;

            if (!(Int32.TryParse(Plugin.Instance.Configuration.RetDays, out retDays)))
            {
                retDays = 0;
            }

            if (retDays == 0)
            {
                retDate = new DateTime(1900, 1, 1, 0, 0, 0);
            }
            else
            {
                retDays = retDays * -1;
                retDate = new DateTime(dtNow.Year, dtNow.Month, dtNow.Day, 0, 0, 0);
                retDate = retDate.AddDays(retDays);
            }
            _logger.LogDebug("Getting Ready to Set Variables!");
            info.ServerDateTime = String.Format("{0}", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture));
            info.RetentionDateTime = String.Format("{0}", retDate.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture));

            _logger.LogDebug(String.Format("ServerDateTime = {0}", info.ServerDateTime));
            _logger.LogDebug(String.Format("RetentionDateTime = {0}", info.RetentionDateTime));

            return info;
        }
    }
}
