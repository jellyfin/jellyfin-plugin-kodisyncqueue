using MediaBrowser.Controller.Entities;
using System;

namespace Emby.Kodi.SyncQueue.Entities
{
    public class LibItem : BaseItem
    {
        public long SyncApiModified { get; set; }
    }
}
