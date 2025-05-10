#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Net.Mime;
using System.Text.Json;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Extensions.Json;
using Jellyfin.Plugin.KodiSyncQueue.Entities;
using Jellyfin.Plugin.KodiSyncQueue.Utils;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.KodiSyncQueue.API
{
    [ApiController]
    [Produces(MediaTypeNames.Application.Json)]
    public class KodiSyncQueueController : ControllerBase
    {
        private readonly ILogger<KodiSyncQueueController> _logger;
        private readonly IUserManager _userManager;
        private readonly ILibraryManager _libraryManager;
        private readonly JsonSerializerOptions _jsonSerializerOptions;

        public KodiSyncQueueController(ILogger<KodiSyncQueueController> logger, IUserManager userManager, ILibraryManager libraryManager)
        {
            _logger = logger;
            _userManager = userManager;
            _libraryManager = libraryManager;
            _jsonSerializerOptions = JsonDefaults.Options;
        }

        /// <summary>
        /// Gets Items for {UserID} from {UTC DATETIME} formatted as yyyy-MM-ddTHH:mm:ssZ using queryString LastUpdateDT.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="lastUpdateDt">UTC DateTime of Last Update, Format yyyy-MM-ddTHH:mm:ssZ.</param>
        /// <param name="filter">Comma separated list of Collection Types to filter (movies,tvshows,music,musicvideos,boxsets. The filter query must be lowercase in both the name and the items.</param>
        /// <returns>The <see cref="SyncUpdateInfo"/>.</returns>
        [HttpGet("Jellyfin.Plugin.KodiSyncQueue/{userId}/GetItems")]
        public ActionResult<SyncUpdateInfo> GetLibraryItemsQuery(
            [FromRoute] string userId,
            [FromQuery] string? lastUpdateDt,
            [FromQuery] string? filter)
        {
            _logger.LogInformation("Sync Requested for UserID: '{UserId}' with LastUpdateDT: '{LastUpdateDT}'", userId, lastUpdateDt);
            if (string.IsNullOrEmpty(lastUpdateDt))
            {
                lastUpdateDt = "1900-01-01T00:00:00Z";
            }

            var filters = filter?.Split(',').Select(f =>
            {
                Enum.TryParse(f, true, out MediaType mediaType);
                return mediaType;
            }).ToArray();

            return PopulateLibraryInfo(
                userId,
                lastUpdateDt,
                filters ?? Array.Empty<MediaType>());
        }

        /// <summary>
        /// Gets The Server Time in UTC format as yyyy-MM-ddTHH:mm:ssZ.
        /// </summary>
        /// <returns>The server UTC time as yyyy-MM-ddTHH:mm:ssZ.</returns>
        [HttpGet("Jellyfin.Plugin.KodiSyncQueue/GetServerDateTime")]
        public ActionResult<ServerTimeInfo> GetServerTime()
        {
            _logger.LogInformation("Server Time Requested...");
            var info = new ServerTimeInfo();
            _logger.LogDebug("Class Variable Created!");
            DateTime dtNow = DateTime.UtcNow;
            DateTime retDate;

            if (!int.TryParse(KodiSyncQueuePlugin.Instance.Configuration.RetDays, out var retDays))
            {
                retDays = 0;
            }

            if (retDays == 0)
            {
                retDate = new DateTime(1900, 1, 1, 0, 0, 0);
            }
            else
            {
                retDate = new DateTime(dtNow.Year, dtNow.Month, dtNow.Day, 0, 0, 0);
                retDate = retDate.AddDays(-retDays);
            }

            _logger.LogDebug("Getting Ready to Set Variables!");
            info.ServerDateTime = $"{DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)}";
            info.RetentionDateTime = $"{retDate.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)}";

            _logger.LogDebug("ServerDateTime = {ServerDateTime}, RetentionDateTime = {RetentionDateTime}", info.ServerDateTime, info.RetentionDateTime);

            return info;
        }

        /// <summary>
        /// Get SyncQueue Plugin Settings.
        /// </summary>
        /// <returns>The <see cref="PluginSettings"/>.</returns>
        [HttpGet("Jellyfin.Plugin.KodiSyncQueue/GetPluginSettings")]
        public ActionResult<PluginSettings> GetKodiPluginSettings()
        {
            _logger.LogInformation("Plugin Settings Requested...");
            var settings = new PluginSettings();
            _logger.LogDebug("Class Variable Created!");

            _logger.LogDebug("Creating Settings Object Variables!");

            if (!int.TryParse(KodiSyncQueuePlugin.Instance.Configuration.RetDays, out var retDays))
            {
                retDays = 0;
            }

            settings.RetentionDays = retDays;
            settings.IsEnabled = KodiSyncQueuePlugin.Instance.Configuration.IsEnabled;
            settings.TrackMovies = KodiSyncQueuePlugin.Instance.Configuration.TkMovies;
            settings.TrackTVShows = KodiSyncQueuePlugin.Instance.Configuration.TkTvShows;
            settings.TrackBoxSets = KodiSyncQueuePlugin.Instance.Configuration.TkBoxSets;
            settings.TrackMusic = KodiSyncQueuePlugin.Instance.Configuration.TkMusic;
            settings.TrackMusicVideos = KodiSyncQueuePlugin.Instance.Configuration.TkMusicVideos;

            _logger.LogDebug("Sending Settings Object Back.");

            return settings;
        }

        /// <summary>
        /// Create a dynamic strm.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="id">The id.</param>
        /// <param name="kodiId">The kodi id.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="name">The name.</param>
        /// <returns>The strm string.</returns>
        [HttpGet("Kodi/{type}/{id}/file.strm")]
        [SuppressMessage("Usage", "CA1801: Review unused parameters", MessageId = "type", Justification = "Legacy")]
        public ActionResult<string> GetStrmFile(
            [FromRoute] string type,
            [FromRoute] string id,
            [FromQuery] string? kodiId,
            [FromQuery] string? handler,
            [FromQuery] string? name)
        {
            if (string.IsNullOrEmpty(handler))
            {
                handler = "plugin://plugin.video.jellyfin";
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

            _logger.LogInformation("returning strm: {0}", strm);
            return strm;
        }

        /// <summary>
        /// Create a dynamic strm.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="id">The id.</param>
        /// <param name="parentId">The parent id.</param>
        /// <param name="kodiId">The kodi id.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="name">The name.</param>
        /// <returns>The strm string.</returns>
        [HttpGet("Kodi/{type}/{parentId}/{id}/file.strm")]
        [SuppressMessage("Usage", "CA1801: Review unused parameters", MessageId = "parentId", Justification = "Legacy")]
        public ActionResult<string> GetStrmFileParentId(
            [FromRoute] string type,
            [FromRoute] string id,
            [FromRoute] string? parentId,
            [FromQuery] string? kodiId,
            [FromQuery] string? handler,
            [FromQuery] string? name)
            => GetStrmFile(type, id, kodiId, handler, name);

        /// <summary>
        /// Create a dynamic strm.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="id">The id.</param>
        /// <param name="parentId">The parent id.</param>
        /// <param name="season">The season.</param>
        /// <param name="kodiId">The kodi id.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="name">The name.</param>
        /// <returns>The strm string.</returns>
        [HttpGet("Kodi/{type}/{parentId}/{season}/{id}/file.strm")]
        [SuppressMessage("Usage", "CA1801: Review unused parameters", MessageId = "parentId", Justification = "Legacy")]
        [SuppressMessage("Usage", "CA1801: Review unused parameters", MessageId = "season", Justification = "Legacy")]
        public ActionResult<string> GetStrmFileSeason(
            [FromRoute] string type,
            [FromRoute] string id,
            [FromRoute] string? parentId,
            [FromRoute] string? season,
            [FromQuery] string? kodiId,
            [FromQuery] string? handler,
            [FromQuery] string? name)
            => GetStrmFile(type, id, kodiId, handler, name);

        private List<string> GetAddedOrUpdatedItems(User user, IEnumerable<Guid> ids)
        {
            var items = ids
                .Select(id => _libraryManager.GetItemById(id))
                .Where(item => item != null)
                .ToList();

            var result = items.SelectMany(i => ApiUserCheck.TranslatePhysicalItemToUserLibrary(i, user, _libraryManager))
                .Where(i => i is not null)
                .Select(i => i!.Id.ToString("N", CultureInfo.InvariantCulture))
                .Distinct()
                .ToList();

            return result;
        }

        private SyncUpdateInfo PopulateLibraryInfo(
            string userId,
            string lastRequestedDt,
            IReadOnlyCollection<MediaType> filters)
        {
            var startTime = DateTime.UtcNow;

            _logger.LogDebug("Starting PopulateLibraryInfo...");

            var userDt = DateTime.Parse(lastRequestedDt, CultureInfo.CurrentCulture, DateTimeStyles.AssumeUniversal);
            var dtl = (long)userDt.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
            var user = _userManager.GetUserById(Guid.Parse(userId));
            if (user is null)
            {
                throw new InvalidOperationException("Unknown user");
            }

            var itemsAdded = KodiSyncQueuePlugin.Instance.DbRepo.GetItems(dtl, ItemStatus.Added, filters);
            var itemsRemoved = KodiSyncQueuePlugin.Instance.DbRepo.GetItems(dtl, ItemStatus.Removed, filters);
            var itemsUpdated = KodiSyncQueuePlugin.Instance.DbRepo.GetItems(dtl, ItemStatus.Updated, filters);
            var userDataChanged = KodiSyncQueuePlugin.Instance.DbRepo.GetUserInfos(dtl, userId, filters);

            var info = new SyncUpdateInfo(
                GetAddedOrUpdatedItems(user, itemsAdded),
                itemsRemoved.Select(id => id.ToString("N", CultureInfo.InvariantCulture)).ToList(),
                GetAddedOrUpdatedItems(user, itemsUpdated),
                userDataChanged.Select(i => JsonSerializer.Deserialize<UserItemDataDto>(i.JsonData, _jsonSerializerOptions)).ToList());

            _logger.LogInformation(
                "Added: {AddedCount}, Removed: {RemovedCount}, Updated: {UpdatedCount}, Changed User Data: {ChangedUserDataCount}",
                info.ItemsAdded.Count,
                info.ItemsRemoved.Count,
                info.ItemsUpdated.Count,
                info.UserDataChanged.Count);
            TimeSpan diffDate = DateTime.UtcNow - startTime;
            _logger.LogInformation("Request Finished Taking {TimeTaken}", diffDate.ToString("c", CultureInfo.InvariantCulture));

            return info;
        }
    }
}
