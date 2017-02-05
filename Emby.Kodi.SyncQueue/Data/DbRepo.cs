using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System.IO;
using Emby.Kodi.SyncQueue.Entities;
using NanoApi.Entities;

namespace Emby.Kodi.SyncQueue.Data
{
    public class DbRepo: IDisposable
    {
        private readonly object _createLock = new object();                

        private const string dbFolder = "Emby.Kodi.SyncQueue.F.1.40.json";
        private const string dbItem = "Emby.Kodi.SyncQueue.I.1.40.json";
        private const string dbUser = "Emby.Kodi.SyncQueue.U.1.40.json";
                
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
            logger.Info("Emby.Kodi.SyncQueue: Creating DB Repository...");
            this.DataPath = dPath;            

            if (File.Exists(Path.Combine(dataPath, "Emby.Kodi.SyncQueue.ldb")))
                File.Delete(Path.Combine(dataPath, "Emby.Kodi.SyncQueue.ldb"));
            if (File.Exists(Path.Combine(dataPath, "Emby.Kodi.SyncQueue.1.2.ldb")))
                File.Delete(Path.Combine(dataPath, "Emby.Kodi.SyncQueue.1.2.ldb"));
            if (File.Exists(Path.Combine(dataPath, "Emby.Kodi.SyncQueue.1.3.ldb")))
                File.Delete(Path.Combine(dataPath, "Emby.Kodi.SyncQueue.1.3.ldb"));
            if (File.Exists(Path.Combine(dataPath, "Emby.Kodi.SyncQueue.1.31.ldb")))
                File.Delete(Path.Combine(dataPath, "Emby.Kodi.SyncQueue.1.31.ldb"));
            if (File.Exists(Path.Combine(dataPath, "Emby.Kodi.SyncQueue.1.32.ldb")))
                File.Delete(Path.Combine(dataPath, "Emby.Kodi.SyncQueue.1.32.ldb"));

            if (!Directory.Exists(dataPath))
            {
                Directory.CreateDirectory(dataPath);
            }
            
            folderRecs = NanoApi.JsonFile<FolderRec>.GetInstance(dataPath, dbFolder, null, null, null);
            folderRecs.ChangeHeader("1.4.0", "Folder Repository", "This repository stores folder changes as pushed from Emby (not currently used).");    
                  
            itemRecs = NanoApi.JsonFile<ItemRec>.GetInstance(dataPath, dbItem, null, null, null);
            itemRecs.ChangeHeader("1.4.0", "Item Repository", "This repository stores item changes per user as pushed from Emby.");
            
            userInfoRecs = NanoApi.JsonFile<UserInfoRec>.GetInstance(dataPath, dbUser, null, null, null);
            userInfoRecs.ChangeHeader("1.4.0", "User Info Repository", "This repository stores deleted items per user as pushed from Emby.");            
        }

        public bool Initialize()
        {
            return true;
        }

        public List<Guid> GetItems(long dtl, int status, bool movies, bool tvshows, bool music, bool musicvideos, bool boxsets)
        {
            var result = new List<Guid>();
            List<ItemRec> final = new List<ItemRec>();

            logger.Debug(String.Format("Emby.Kodi.SyncQueue:  Using dtl {0:yyyy-MM-dd HH:mm:ss} for time {1}", new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(dtl), dtl));
            logger.Debug(String.Format("Emby.Kodi.SyncQueue:  IntStatus: {0}", status));

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
            //    _logger.Debug(result.ToString());
            //    _logger.Debug(_json.SerializeToString(i));
            //    _logger.Debug(String.Format("Emby.Kodi.SyncQueue:  Item {0} {1} {2:yyyy-MM-dd HH:mm:ss} for time {3}", i.ItemId, status,
            //                new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(i.LastModified), i.LastModified));
            //});
            //result = itms.Select(i => i.ItemId).Distinct().ToList();

            //itms.ForEach(x =>
            //{
            //    if (result.Where(i => i == x.ItemId.ToString("N")).FirstOrDefault() == null)
            //    {
            //        _logger.Debug(String.Format("Emby.Kodi.SyncQueue:  Item {0} Modified {1:yyyy-MM-dd HH:mm:ss} for time {2}", x.ItemId, 
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
            logger.Info("Emby.Kodi.SyncQueue.Task: Starting Folder Retention Deletion...");
            folderRecs.Delete(x => x.LastModified < dtl);
            logger.Info("Emby.Kodi.SyncQueue.Task: Finished Folder Retention Deletion...");

            logger.Info("Emby.Kodi.SyncQueue.Task: Starting Item Retention Deletion...");
            itemRecs.Delete(x => x.LastModified < dtl);
            logger.Info("Emby.Kodi.SyncQueue.Task: Finished Item Retention Deletion...");

            logger.Info("Emby.Kodi.SyncQueue.Task: Starting UserItem Retention Deletion...");
            userInfoRecs.Delete(x => x.LastModified < dtl);
            logger.Info("Emby.Kodi.SyncQueue.Task: Finished UserItem Retention Deletion...");
        }

        public void WriteLibrarySync(List<LibItem> Items, int status, CancellationToken cancellationToken)
        {
            ItemRec newRec;
            var statusType = string.Empty;
            if (status == 0) { statusType = "Added"; }
            else if (status == 1) { statusType = "Updated"; }
            else { statusType = "Removed"; }

            Items.ForEach(i =>
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

                if (rec == null) { itemRecs.Insert(newRec); }
                else if (rec.LastModified < newTime)
                {
                    newRec.Id = rec.Id;
                    itemRecs.Update(x => x.Id == rec.Id, x =>
                    {
                        x.ItemId = newRec.ItemId;
                        x.Status = newRec.Status;
                        x.LastModified = newRec.LastModified;
                        x.MediaType = newRec.MediaType;
                    });
                }
                else { newRec = null; }

                if (newRec != null)
                {
                    logger.Debug(String.Format("Emby.Kodi.SyncQueue:  {0} ItemId: '{1}'", statusType, newRec.ItemId.ToString("N")));
                }
                else
                {
                    logger.Debug(String.Format("Emby.Kodi.SyncQueue:  ItemId: '{0}' Skipped", i.Id.ToString("N")));
                }

            });
        }

        public void SetUserInfoSync(List<MediaBrowser.Model.Dto.UserItemDataDto> dtos, List<LibItem> itemRefs, string userName, string userId, CancellationToken cancellationToken)
        {
            dtos.ForEach(dto =>
            {

                var sJson = json.SerializeToString(dto).ToString();
                logger.Debug("Emby.Kodi.SyncQueue:  Updating ItemId '{0}' for UserId: '{1}'", dto.ItemId, userId);

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
                        userInfoRecs.Insert(newRec);
                    }
                    else
                    {
                        newRec.Id = oldRec.Id;
                        userInfoRecs.Update(u => u.Id == oldRec.Id, u =>
                        {
                            u.ItemId = newRec.ItemId;
                            u.Json = newRec.Json;
                            u.UserId = newRec.UserId;
                            u.LastModified = newRec.LastModified;
                            u.MediaType = newRec.MediaType;
                        });
                    }
                }

            });
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
