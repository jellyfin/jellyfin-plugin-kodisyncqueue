using Emby.Kodi.SyncQueue.Entities;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Globalization;
using MediaBrowser.Model.Services;

namespace Emby.Kodi.SyncQueue.API
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
            _logger.Info("Emby.Kodi.SyncQueue: Server Time Requested...");
            var info = new ServerTimeInfo();
            _logger.Debug("Emby.Kodi.SyncQueue: Class Variable Created!");
            int retDays = 0;
            DateTimeOffset dtNow = DateTimeOffset.UtcNow;
            DateTimeOffset retDate;

            if (!(Int32.TryParse(Plugin.Instance.Configuration.RetDays, out retDays)))
            {
                retDays = 0;
            }

            if (retDays == 0)
            {
                retDate = new DateTimeOffset(1900, 1, 1, 0, 0, 0, TimeSpan.Zero);
            }
            else
            {
                retDays = retDays * -1;
                retDate = new DateTimeOffset(dtNow.Year, dtNow.Month, dtNow.Day, 0, 0, 0, TimeSpan.Zero);
                retDate = retDate.AddDays(retDays);
            }
            _logger.Debug("Emby.Kodi.SyncQueue: Getting Ready to Set Variables!");
            info.ServerDateTime = String.Format("{0}", DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture));
            info.RetentionDateTime = String.Format("{0}", retDate.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture));

            _logger.Debug(String.Format("Emby.Kodi.SyncQueue: ServerDateTime = {0}", info.ServerDateTime));
            _logger.Debug(String.Format("Emby.Kodi.SyncQueue: RetentionDateTime = {0}", info.RetentionDateTime));

            return info;
        }
    }
}
