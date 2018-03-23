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

        public string GetStrm(string handler, string id, string kodiId, string name)
        {
            if (string.IsNullOrEmpty(handler))
            {
                handler = "plugin://plugin.video.emby";
            }

            string strm = handler + "?mode=play&id=" + id;

            if (!string.IsNullOrEmpty(kodiId))
            {
                strm += "&dbid=" + kodiId;
            }

            if (!string.IsNullOrEmpty(name))
            {
                strm += "&filename=" + name;
            }

            return strm;
        }

        public object Get(GetStrmFile request)
        {
            string strm = GetStrm(request.Handler, request.Id, request.KodiId, request.Name);

            Logger.Info("returning strm: {0}", strm);
            return strm;
        }

        public object Get(GetStrmFileWithParent request)
        {
            string strm = GetStrm(request.Handler, request.Id, request.KodiId, request.Name);

            Logger.Info("returning strm: {0}", strm);
            return strm;
        }
    }
}
