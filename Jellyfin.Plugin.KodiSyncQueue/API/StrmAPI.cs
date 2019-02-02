using MediaBrowser.Model.Services;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.KodiSyncQueue.API
{
    class StrmAPI : IService
    {
        private readonly ILogger Logger;

        public StrmAPI(ILogger logger)
        {
            Logger = logger;
        }

        public object Get(GetStrmFile request)
        {
            if (string.IsNullOrEmpty(request.Handler))
            {
                request.Handler = "plugin://plugin.video.emby";
            }

            string strm = request.Handler + "?mode=play&id=" + request.Id;

            if (!string.IsNullOrEmpty(request.KodiId))
            {
                strm += "&dbid=" + request.KodiId;
            }

            if (!string.IsNullOrEmpty(request.Name))
            {
                strm += "&filename=" + request.Name;
            }

            Logger.LogInformation("returning strm: {0}", strm);
            return strm;
        }
    }
}
