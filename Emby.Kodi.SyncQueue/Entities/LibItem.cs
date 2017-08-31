using MediaBrowser.Controller.Entities;
using System;

namespace Emby.Kodi.SyncQueue.Entities
{
    public class LibItem
    {
        public Guid Id { get; set; }
        public long SyncApiModified { get; set; }
        public int ItemType { get; set; }

        // 0 = Movies
        // 1 = TVShows
        // 2 = Music
        // 3 = Music Videos
        // 4 = BoxSets

    }
}
