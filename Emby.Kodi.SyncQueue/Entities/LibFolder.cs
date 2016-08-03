using MediaBrowser.Controller.Entities;
using System;

namespace Emby.Kodi.SyncQueue.Entities
{
    public class LibFolder : Folder
    {
        public long SyncApiModified { get; set; }
    }
}
