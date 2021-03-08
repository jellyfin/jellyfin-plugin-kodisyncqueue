using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Jellyfin.Plugin.KodiSyncQueue.Entities;
using LiteDB;
using MediaBrowser.Common.Json;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.KodiSyncQueue.Data
{
    public class DbRepo : IDisposable
    {
        private const string ItemsCollection = "items";
        private const string UserInfoCollection = "user_info";

        private readonly LiteDatabase _liteDb;

        private readonly ILogger<DbRepo> _logger;

        public DbRepo(string dPath, ILogger<DbRepo> logger)
        {
            _logger = logger;
            _logger.LogInformation("Creating DB Repository...");
            Directory.CreateDirectory(dPath);
            _liteDb = new LiteDatabase($"filename={dPath}/kodisyncqueue.db;mode=exclusive");
        }

        public List<Guid> GetItems(long dtl, ItemStatus status, IReadOnlyCollection<MediaType> filters)
        {
            _logger.LogDebug("Using dtl {0:yyyy-MM-dd HH:mm:ss} for time {1}", new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(dtl), dtl);
            _logger.LogDebug("IntStatus: {Status}", status);

            // Get collection instance
            var itemCollection = _liteDb.GetCollection<ItemRec>(ItemsCollection);

            // Create, if not exists, new index on Id field
            itemCollection.EnsureIndex(x => x.Id);

            var items = itemCollection.Find(x => x.LastModified > dtl && x.Status == status);
            return items.Where(x => filters.All(f => f != x.MediaType)).Select(i => i.ItemId).Distinct().ToList();
        }

        public List<UserJson> GetUserInfos(long dtl, string userId, IReadOnlyCollection<MediaType> filters)
        {
            // Get collection instance
            var userInfoCollection = _liteDb.GetCollection<UserInfoRec>(UserInfoCollection);

            // Create, if not exists, new index on Id field
            userInfoCollection.EnsureIndex(x => x.Id);

            var userInfoRecs = userInfoCollection.Find(x => x.LastModified > dtl && x.UserId == userId);
            userInfoRecs = userInfoRecs.Where(x => filters.All(f => f != x.MediaType));
            return userInfoRecs.Select(i => new UserJson { Id = i.ItemId, JsonData = i.Json }).ToList();
        }

        public void DeleteOldData(long dtl)
        {
            _logger.LogInformation("Starting Item and UserItem Retention Deletion...");
            _liteDb.GetCollection<ItemRec>(ItemsCollection).Delete(x => x.LastModified < dtl);
            _liteDb.GetCollection<UserInfoRec>(UserInfoCollection).Delete(x => x.LastModified < dtl);
            _logger.LogInformation("Finished Item and UserItem Retention Deletion...");
        }

        public void WriteLibrarySync(IEnumerable<LibItem> items, ItemStatus status, CancellationToken cancellationToken)
        {
            var newRecs = new List<ItemRec>();
            var upRecs = new List<ItemRec>();
            var itemCollection = _liteDb.GetCollection<ItemRec>(ItemsCollection);
            foreach (var i in items)
            {
                var newTime = i.SyncApiModified;

                var rec = itemCollection.Find(x => x.ItemId == i.Id).FirstOrDefault();

                var newRec = new ItemRec
                {
                    ItemId = i.Id,
                    Status = status,
                    LastModified = newTime,
                    MediaType = i.ItemType
                };

                if (rec == null)
                {
                    newRecs.Add(newRec);
                }
                else if (rec.LastModified < newTime)
                {
                    newRec.Id = rec.Id;
                    upRecs.Add(newRec);
                }
                else
                {
                    _logger.LogDebug("NewTime: {NewTime}  OldTime: {LastModified}   Status: {Status}", newTime, rec.LastModified, status);
                    newRec = null;
                }

                if (newRec != null)
                {
                    _logger.LogDebug("{StatusType} ItemId: '{ItemId}'", status.ToString(), newRec.ItemId.ToString("N", CultureInfo.InvariantCulture));
                }
                else
                {
                    _logger.LogDebug("ItemId: '{ItemId}' Skipped", i.Id.ToString("N", CultureInfo.InvariantCulture));
                }
            }

            if (newRecs.Count > 0)
            {
                _logger.LogDebug("{@NewRecs}", newRecs);
                itemCollection.Insert(newRecs);
            }

            if (upRecs.Count > 0)
            {
                var data = itemCollection.FindAll().ToList();

                foreach (var rec in upRecs)
                {
                    data.Where(d => d.Id == rec.Id).ToList().ForEach(i =>
                    {
                        i.ItemId = rec.ItemId;
                        i.Status = rec.Status;
                        i.LastModified = rec.LastModified;
                        i.MediaType = rec.MediaType;
                    });
                }

                itemCollection.Update(data);

                data = itemCollection.FindAll().ToList();
                _logger.LogDebug("{@Data}", data);
            }
        }

        public void SetUserInfoSync(List<MediaBrowser.Model.Dto.UserItemDataDto> dtos, List<LibItem> itemRefs, string userId)
        {
            var newRecs = new List<UserInfoRec>();
            var upRecs = new List<UserInfoRec>();
            var userInfoCollection = _liteDb.GetCollection<UserInfoRec>(UserInfoCollection);
            var jsonOptions = JsonDefaults.GetOptions();

            dtos.ForEach(dto =>
            {
                var sJson = System.Text.Json.JsonSerializer.Serialize(dto, jsonOptions);
                _logger.LogDebug("Updating ItemId '{0}' for UserId: '{1}'", dto.ItemId, userId);

                LibItem itemref = itemRefs.FirstOrDefault(x => x.Id.ToString("N", CultureInfo.InvariantCulture) == dto.ItemId);
                if (itemref != null)
                {
                    var oldRec = userInfoCollection.Find(u => u.ItemId == dto.ItemId && u.UserId == userId).FirstOrDefault();
                    var newRec = new UserInfoRec
                    {
                        ItemId = dto.ItemId,
                        Json = sJson,
                        UserId = userId,
                        LastModified = DateTimeOffset.Now.ToUnixTimeSeconds(),
                        MediaType = itemref.ItemType
                    };
                    if (oldRec == null)
                    {
                        newRecs.Add(newRec);
                    }
                    else
                    {
                        newRec.Id = oldRec.Id;
                        upRecs.Add(newRec);
                    }
                }
            });

            if (newRecs.Count > 0)
            {
                userInfoCollection.Insert(newRecs);
            }

            if (upRecs.Count > 0)
            {
                var data = userInfoCollection.FindAll().ToList();

                foreach (var rec in upRecs)
                {
                    data.Where(d => d.Id == rec.Id).ToList().ForEach(u =>
                    {
                        u.ItemId = rec.ItemId;
                        u.Json = rec.Json;
                        u.UserId = rec.UserId;
                        u.LastModified = rec.LastModified;
                        u.MediaType = rec.MediaType;
                    });
                }

                userInfoCollection.Update(data);
            }
        }

        public void Dispose()
        {
            _liteDb.Dispose();
        }
    }
}
