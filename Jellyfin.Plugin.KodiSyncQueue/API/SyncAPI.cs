using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Serialization;
using Jellyfin.Plugin.KodiSyncQueue.Entities;
using Jellyfin.Plugin.KodiSyncQueue.Data;
using System.Globalization;
using Jellyfin.Plugin.KodiSyncQueue.Utils;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Services;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.KodiSyncQueue.API
{
    public class SyncAPI : IService
    {
        private readonly ILogger _logger;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IUserManager _userManager;
        private readonly ILibraryManager _libraryManager;

        public SyncAPI(ILogger logger, IJsonSerializer jsonSerializer, IUserManager userManager, ILibraryManager libraryManager)
        {
            _logger = logger;
            _jsonSerializer = jsonSerializer;
            _userManager = userManager;
            _libraryManager = libraryManager;

            _logger.LogInformation("SyncAPI Created and Listening at \"/Jellyfin.Plugin.KodiSyncQueue/{UserID}/GetItems?LastUpdateDT={LastUpdateDT}&format=json\" - {LastUpdateDT} must be a UTC DateTime formatted as yyyy-MM-ddTHH:mm:ssZ");
            _logger.LogInformation("The following parameters also exist to filter the results:");
            _logger.LogInformation("filter=movies,tvshows,music,musicvideos,boxsets");
            _logger.LogInformation("Results will be included by default and only filtered if added to the filter query...");
            _logger.LogInformation("the filter query must be lowercase in both the name and the items...");
        }

        public SyncUpdateInfo Get(GetLibraryItemsQuery request)
        {
            _logger.LogInformation("Sync Requested for UserID: '{UserId}' with LastUpdateDT: '{LastUpdateDT}'", request.UserID, request.LastUpdateDT);
            if (string.IsNullOrEmpty(request.LastUpdateDT))
                request.LastUpdateDT = "1900-01-01T00:00:00Z";

            var filters = request.filter?.ToLower().Split(',').Select(f =>
            {
                Enum.TryParse(f, true, out MediaType mediaType);
                return mediaType;
            }).ToArray();

            return PopulateLibraryInfo(
                request.UserID,
                request.LastUpdateDT,
                filters
            );
        }

        private List<string> GetAddedOrUpdatedItems(User user, IEnumerable<Guid> ids)
        {
            List<BaseItem> items = new List<BaseItem>();
            foreach (Guid id in ids)
            {
                var item = _libraryManager.GetItemById(id);
                if (item != null)
                {
                    items.Add(item);
                }
            }

            var result = items.SelectMany(i => ApiUserCheck.TranslatePhysicalItemToUserLibrary(i, user, _libraryManager)).Select(i => i.Id.ToString("N")).Distinct().ToList();
            return result;
        }

        private SyncUpdateInfo PopulateLibraryInfo(string userId, string lastRequestedDt,
            IReadOnlyCollection<MediaType> filters)
        {
            var startTime = DateTime.UtcNow;

            _logger.LogDebug("Starting PopulateLibraryInfo...");

            var info = new SyncUpdateInfo();

            var userDt = DateTime.Parse(lastRequestedDt, CultureInfo.CurrentCulture, DateTimeStyles.AssumeUniversal);
            var dtl = (long)userDt.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
            var user = _userManager.GetUserById(Guid.Parse(userId));
            
            var itemsAdded = DbRepo.Instance.GetItems(dtl, ItemStatus.Added, filters);
            var itemsRemoved = DbRepo.Instance.GetItems(dtl, ItemStatus.Removed, filters);
            var itemsUpdated = DbRepo.Instance.GetItems(dtl, ItemStatus.Updated, filters);
            var userDataChanged = DbRepo.Instance.GetUserInfos(dtl, userId, filters);

            info.ItemsAdded = GetAddedOrUpdatedItems(user, itemsAdded);
            info.ItemsRemoved = itemsRemoved.Select(id => id.ToString("N")).ToList();
            info.ItemsUpdated = GetAddedOrUpdatedItems(user, itemsUpdated);
            info.UserDataChanged = userDataChanged.Select(i => _jsonSerializer.DeserializeFromString<UserItemDataDto>(i.JsonData)).ToList();

            _logger.LogInformation("Added: {AddedCount}, Removed: {RemovedCount}, Updated: {UpdatedCount}, Changed User Data: {ChangedUserDataCount}",
                info.ItemsAdded.Count, info.ItemsRemoved.Count, info.ItemsUpdated.Count, info.UserDataChanged.Count);
            TimeSpan diffDate = DateTime.UtcNow - startTime;
            _logger.LogInformation("Request Finished Taking {TimeTaken}", diffDate.ToString("c"));

            return info;
        }
    }
}
