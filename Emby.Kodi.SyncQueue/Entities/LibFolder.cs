using MediaBrowser.Controller.Entities;
using System;

namespace Emby.Kodi.SyncQueue.Entities
{
    public class LibFolder
    {
        public Guid Id { get; set; }
        public long SyncApiModified { get; set; }
    }
}
