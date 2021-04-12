using System;
using System.Collections.Generic;
using Jellyfin.Data.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Plugin.KodiSyncQueue.Utils
{
    public static class ApiUserCheck
    {
        public static IEnumerable<T> TranslatePhysicalItemToUserLibrary<T>(T item, User user, ILibraryManager libraryManager, bool includeIfNotFound = false)
            where T : BaseItem
        {
            // If the physical root changed, return the user root
            if (item is AggregateFolder)
            {
                return new[] { libraryManager.GetUserRootFolder() as T };
            }

            // Return it only if it's in the user's library
            if (includeIfNotFound || libraryManager.GetItemById(item.Id).IsVisibleStandalone(user))
            {
                return new[] { item };
            }

            return Array.Empty<T>();
        }
    }
}
