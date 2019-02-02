using System;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MediaBrowser.Model.Serialization;
using Jellyfin.Plugin.KodiSyncQueue.Entities;
using System.Text;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.KodiSyncQueue.Data
{
    public class DbRepo: IDisposable
    {
        private readonly object _createLock = new object();
        private readonly object _userLock = new object();
        private readonly object _folderLock = new object();
        private readonly object _itemLock = new object();

        private const string dbFolder = "Jellyfin.Plugin.KodiSyncQueue.F.1.40.json";
        private const string dbItem = "Jellyfin.Plugin.KodiSyncQueue.I.1.40.json";
        private const string dbUser = "Jellyfin.Plugin.KodiSyncQueue.U.1.40.json";
                
        private string dataPath = "";        
        
        public string DataPath
        {
            get { return dataPath; }
            set { dataPath = Path.Combine(value, "SyncData"); }
        }

        private NanoApi.JsonFile<FolderRec> folderRecs = null;
        private NanoApi.JsonFile<ItemRec> itemRecs = null;
        private NanoApi.JsonFile<UserInfoRec> userInfoRecs = null;

        private static DbRepo instance = null;
        public static ILogger logger = null;
        public static IJsonSerializer json = null;
        public static string dbPath = "";
        public static IFileSystem fileSystem = null;

        public static DbRepo Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new DbRepo(dbPath);
                }
                return instance;
            }
        }


        public DbRepo(string dPath)
        {
            logger.LogInformation("Creating DB Repository...");
            DataPath = dPath;

            Directory.CreateDirectory(dataPath);

            folderRecs = NanoApi.JsonFile<FolderRec>.GetInstance(dataPath, dbFolder, Encoding.UTF8, null, null);
            if (!folderRecs.CheckVersion("1.4.0"))
                folderRecs.ChangeHeader("1.4.0", "Folder Repository", "This repository stores folder changes as pushed from Jellyfin (not currently used).");    
                  
            itemRecs = NanoApi.JsonFile<ItemRec>.GetInstance(dataPath, dbItem, Encoding.UTF8, null, null);
            if (!itemRecs.CheckVersion("1.4.0"))
                itemRecs.ChangeHeader("1.4.0", "Item Repository", "This repository stores item changes per user as pushed from Jellyfin.");
            
            userInfoRecs = NanoApi.JsonFile<UserInfoRec>.GetInstance(dataPath, dbUser, Encoding.UTF8, null, null);
            if (!userInfoRecs.CheckVersion("1.4.0"))
                userInfoRecs.ChangeHeader("1.4.0", "User Info Repository", "This repository stores deleted items per user as pushed from Jellyfin.");            
        }

        public List<Guid> GetItems(long dtl, int status, bool movies, bool tvshows, bool music, bool musicvideos, bool boxsets)
        {
            var result = new List<Guid>();
            List<ItemRec> final = new List<ItemRec>();

            logger.LogDebug("Using dtl {0:yyyy-MM-dd HH:mm:ss} for time {1}", new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(dtl), dtl);
            logger.LogDebug("IntStatus: {Status}", status);

            var items = itemRecs.Select(x => x.LastModified > dtl && x.Status == status).ToList();

            result = items.Where(x =>
                            {
                                switch (x.MediaType)
                                {
                                    case 0:
                                        if (movies) { return true; } else { return false; }
                                    case 1:
                                        if (tvshows) { return true; } else { return false; }
                                    case 2:
                                        if (music) { return true; } else { return false; }
                                    case 3:
                                        if (musicvideos) { return true; } else { return false; }
                                    case 4:
                                        if (boxsets) { return true; } else { return false; }
                                }
                                return false;
                            }).Select(i => i.ItemId).Distinct()
                            .ToList();

            //itms.ForEach(i =>
            //{
            //    _logger.LogDebug(result.ToString());
            //    _logger.LogDebug(_json.SerializeToString(i));
            //    _logger.LogDebug(String.Format("Item {0} {1} {2:yyyy-MM-dd HH:mm:ss} for time {3}", i.ItemId, status,
            //                new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(i.LastModified), i.LastModified));
            //});
            //result = itms.Select(i => i.ItemId).Distinct().ToList();

            //itms.ForEach(x =>
            //{
            //    if (result.Where(i => i == x.ItemId.ToString("N")).FirstOrDefault() == null)
            //    {
            //        _logger.LogDebug(String.Format("Item {0} Modified {1:yyyy-MM-dd HH:mm:ss} for time {2}", x.ItemId, 
            //                new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(x.LastModified), x.LastModified));
            //        result.Add(x.ItemId);
            //    }
            //});

            return result;
        }

        //public List<string> GetFolders(long dtl, int status, string userId, bool movies, bool tvshows, bool music, bool musicvideos, bool boxsets)
        //{
        //    using (var trn = DB.BeginTrans())
        //    {
        //        var result = new List<string>();
        //        List<FolderRec> final = new List<FolderRec>();
        //        //
        //        var flds = folders.Find(x => x.LastModified > dtl && 
        //                                     x.Status == status && 
        //                                     x.UserId == userId)
        //                          .Where(x =>
        //                          {
        //                              switch (x.MediaType)
        //                              {
        //                                  case "movies":
        //                                      if (movies) { return true; } else { return false; }
        //                                  case "tvshows":
        //                                      if (tvshows) { return true; } else { return false; }
        //                                  case "music":
        //                                      if (music) { return true; } else { return false; }
        //                                  case "musicvideos":
        //                                      if (musicvideos) { return true; } else { return false; }
        //                                  case "boxsets":
        //                                      if (boxsets) { return true; } else { return false; }
        //                              }
        //                              return false;
        //                          })
        //                          .ToList();

        //        //if (movies) { final.AddRange(flds.Where(x => x.MediaType == "movies").ToList()); }
        //        //if (tvshows) { final.AddRange(flds.Where(x => x.MediaType == "tvshows").ToList()); }
        //        //if (music) { final.AddRange(flds.Where(x => x.MediaType == "music").ToList()); }
        //        //if (musicvideos) { final.AddRange(flds.Where(x => x.MediaType == "musicvideos").ToList()); }
        //        //if (boxsets) { final.AddRange(flds.Where(x => x.MediaType == "boxsets").ToList()); }

        //        //final.ForEach(x =>
        //        flds.ForEach(x =>
        //        {
        //            if (result.Where(i => i == x.ItemId).FirstOrDefault() == null)
        //            {
        //                result.Add(x.ItemId);
        //            }
        //        });
        //        return result;
        //    }
        //}

        public List<UserJson> GetUserInfos(long dtl, string userId, bool movies, bool tvshows, bool music, bool musicvideos, bool boxsets)
        {
            var result = new List<UserJson>();
            var tids = new List<string>();
            var final = new List<UserInfoRec>();

            var uids = userInfoRecs.Select(x => x.LastModified > dtl && x.UserId == userId).ToList();

            uids = uids.Where(x =>
                        {
                            switch (x.MediaType)
                            {
                                case 0:
                                    if (movies) { return true; } else { return false; }
                                case 1:
                                    if (tvshows) { return true; } else { return false; }
                                case 2:
                                    if (music) { return true; } else { return false; }
                                case 3:
                                    if (musicvideos) { return true; } else { return false; }
                                case 4:
                                    if (boxsets) { return true; } else { return false; }
                            }
                            return false;
                        })
                        .ToList();


            result = uids.Select(i => new UserJson() { Id = i.ItemId, JsonData = i.Json }).ToList();

            return result;
        }

        public void DeleteOldData(long dtl)
        {
            lock (_folderLock)
            {
                logger.LogInformation("Starting Folder Retention Deletion...");
                folderRecs.Delete(x => x.LastModified < dtl);
                logger.LogInformation("Finished Folder Retention Deletion...");
            }

            lock (_itemLock)
            {
                logger.LogInformation("Starting Item Retention Deletion...");
                itemRecs.Delete(x => x.LastModified < dtl);
                logger.LogInformation("Finished Item Retention Deletion...");
            }

            lock (_userLock)
            {
                logger.LogInformation("Starting UserItem Retention Deletion...");
                userInfoRecs.Delete(x => x.LastModified < dtl);
                logger.LogInformation("Finished UserItem Retention Deletion...");
            }
        }

        public void WriteLibrarySync(List<LibItem> Items, int status, CancellationToken cancellationToken)
        {
            ItemRec newRec;
            var statusType = string.Empty;
            if (status == 0) { statusType = "Added"; }
            else if (status == 1) { statusType = "Updated"; }
            else { statusType = "Removed"; }

            lock (_itemLock)
            {
                var newRecs = new List<ItemRec>();
                var upRecs = new List<ItemRec>();

                foreach (var i in Items)
                {
                    long newTime;

                    newTime = i.SyncApiModified;

                    var rec = itemRecs.Select(x => x.ItemId == i.Id).FirstOrDefault();

                    newRec = new ItemRec()
                    {
                        ItemId = i.Id,
                        Status = status,
                        LastModified = newTime,
                        MediaType = i.ItemType
                    };

                    if (rec == null) { newRecs.Add(newRec); } 
                    else if (rec.LastModified < newTime)
                    {
                        newRec.Id = rec.Id;
                        upRecs.Add(newRec);
                    }
                    else
                    {
                        logger.LogDebug("NewTime: {NewTime}  OldTime: {LastModified}   Status: {Status}", newTime, rec.LastModified, status);
                        newRec = null;
                    }

                    if (newRec != null)
                    {
                        logger.LogDebug("{StatusType} ItemId: '{ItemId}'", statusType, newRec.ItemId.ToString("N"));
                    }
                    else
                    {
                        logger.LogDebug("ItemId: '{ItemId}' Skipped", i.Id.ToString("N"));
                    }
                }

                if (newRecs.Count > 0)
                {

                    logger.LogDebug("{@NewRecs}", newRecs);
                    itemRecs.Insert(newRecs);

                }
                if (upRecs.Count > 0)
                {
                    logger.LogDebug("THIS IS WHERE WE ENTER UPDATE FOR EXISTING ITEMS!!!!!");
                    var data = itemRecs.Select();


                    logger.LogDebug("THIS IS WHERE WE ENTER THE LOOP");
                    foreach (var rec in upRecs)
                    {
                        logger.LogDebug("THIS IS BEFORE LINQ WORK!");
                        data.Where(d => d.Id == rec.Id).ToList().ForEach(i =>
                        {
                            logger.LogDebug("THIS IS INSIDE THE LINQ UPDATING START!");
                            i.ItemId = rec.ItemId;
                            i.Status = rec.Status;
                            i.LastModified = rec.LastModified;
                            i.MediaType = rec.MediaType;
                            logger.LogDebug("THIS IS INSIDE THE LINQ UPDATING END!");
                        });
                    }

                    logger.LogDebug("THIS IS AFTER LINQ STARTING COMMIT!");
                    itemRecs.Commit(data);
                    logger.LogDebug("{@Data}", data);
                    logger.LogDebug("THIS IS AFTER LINQ FINISHED COMMIT!");

                    data = itemRecs.Select();
                    logger.LogDebug("{@Data}", data);                    
                }
            }
        }

        public void SetUserInfoSync(List<MediaBrowser.Model.Dto.UserItemDataDto> dtos, List<LibItem> itemRefs, string userName, string userId, CancellationToken cancellationToken)
        {
            var newRecs = new List<UserInfoRec>();
            var upRecs = new List<UserInfoRec>();
            lock (_userLock)
            {
                dtos.ForEach(dto =>
                {

                    var sJson = json.SerializeToString(dto).ToString();
                    logger.LogDebug("Updating ItemId '{0}' for UserId: '{1}'", dto.ItemId, userId);

                    LibItem itemref = itemRefs.Where(x => x.Id.ToString("N") == dto.ItemId).FirstOrDefault();
                    if (itemref != null)
                    {
                        var oldRec = userInfoRecs.Select(u => u.ItemId == dto.ItemId && u.UserId == userId).FirstOrDefault();
                        var newRec = new UserInfoRec()
                        {
                            ItemId = dto.ItemId,
                            Json = sJson,
                            UserId = userId,
                            LastModified = (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds),
                            MediaType = itemref.ItemType,
                            //LibraryName = itemref.CollectionName
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

                    userInfoRecs.Insert(newRecs);

                }
                if (upRecs.Count > 0)
                {
                    var data = userInfoRecs.Select();

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
                    userInfoRecs.Commit(data);
                }
            }
        }

        #region Dispose

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool dispose)
        {
            if (dispose)
            {
                if (folderRecs != null)
                {
                    folderRecs.Dispose();
                    folderRecs = null;
                }
                if (itemRecs != null)
                {
                    itemRecs.Dispose();
                    itemRecs = null;
                }
                if (userInfoRecs != null)
                {
                    userInfoRecs.Dispose();
                    userInfoRecs = null;
                }
            }
        }

        #endregion
    }
}
