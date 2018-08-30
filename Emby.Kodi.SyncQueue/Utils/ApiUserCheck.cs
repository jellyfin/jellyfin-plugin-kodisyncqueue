using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emby.Kodi.SyncQueue.Utils
{
    public static class ApiUserCheck
    {
        public static IEnumerable<T> TranslatePhysicalItemToUserLibrary<T>(T item, User user, ILibraryManager _libraryManager, bool includeIfNotFound = false)
            where T : BaseItem
        {
            if (item is AggregateFolder)
            {
                return new T[] { };
            }

            // Return it only if it's in the user's library
            if (includeIfNotFound || _libraryManager.GetItemById(item.Id).IsVisibleStandalone(user))
            {
                return new[] { item };
            }

            return new T[] { };
        }
    }
}
