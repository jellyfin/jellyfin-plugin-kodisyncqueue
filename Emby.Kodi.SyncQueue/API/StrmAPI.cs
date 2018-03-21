using System;
using System.Collections.Generic;
using System.Text;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Services;

namespace Emby.Kodi.SyncQueue.API
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
            string handler = request.Handler;

            if (handler == null || handler == "")
            {
                handler = "plugin://plugin.video.emby";
            }

            string strm = handler + "?mode=play&id=" + request.Id;

            if (request.KodiId != null && request.KodiId != "")
            {
                strm += "&dbid=" + request.KodiId;
            }

            if (request.Name != null && request.Name != "")
            {
                strm += "&filename=" + request.Name;
            }

            Logger.Info("returning strm: {0}", strm);
            return strm;
        }

        public object Get(GetStrmFileWithParent request)
        {
            string handler = request.Handler;

            if (handler == null || handler == "")
            {
                handler = "plugin://plugin.video.emby";
            }

            string strm = handler + "?mode=play&id=" + request.Id;

            if (request.KodiId != null && request.KodiId != "")
            {
                strm += "&dbid=" + request.KodiId;
            }

            if (request.Name != null && request.Name != "")
            {
                strm += "&filename=" + request.Name;
            }

            Logger.Info("returning strm: {0}", strm);
            return strm;
        }
    }
}
