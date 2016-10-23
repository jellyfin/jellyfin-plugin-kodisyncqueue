using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System.IO;
using LiteDB;
using LiteDB.Platform;
using Emby.Kodi.SyncQueue.Entities;

namespace Emby.Kodi.SyncQueue.Data
{
    public class DbRepo: IDisposable
    {
        private readonly object _createLock = new object();        
        private LiteDatabase DB = null;
        private string dataPath;
        private string dataName = "Emby.Kodi.SyncQueue.1.3.ldb";

        private readonly ILogger _logger;
        private readonly IJsonSerializer _json;

        private LiteCollection<FolderRec> folders = null;
        private LiteCollection<ItemRec> items = null;
        private LiteCollection<UserInfoRec> userinfos = null;
        
        public string DataPath
        {
            get { return dataPath; }
            set { dataPath = Path.Combine(value, "SyncData"); }
        }

        public DbRepo(string dp, ILogger logger, IJsonSerializer json = null)
        {
            DataPath = dp;
            LitePlatform.Initialize(new LitePlatformFullDotNet());            
            var data = Path.Combine(DataPath, dataName);
 
            _logger = logger;
            _json = json;

            if (File.Exists(Path.Combine(DataPath, "Emby.Kodi.SyncQueue.ldb")))
            {
                File.Delete(Path.Combine(DataPath, "Emby.Kodi.SyncQueue.ldb"));
            }
            if (File.Exists(Path.Combine(DataPath, "Emby.Kodi.SyncQueue.1.2.ldb")))
            {
                File.Delete(Path.Combine(DataPath, "Emby.Kodi.SyncQueue.1.2.ldb"));
            }

            if (!Directory.Exists(DataPath))
            {
                Directory.CreateDirectory(DataPath);
            }

            if (!File.Exists(data))
            {

            }
            if (DB == null) { DB = new LiteDatabase(data); }
            
            folders = DB.GetCollection<FolderRec>("Folders");
            items = DB.GetCollection<ItemRec>("Items");
            userinfos = DB.GetCollection<UserInfoRec>("UserInfos");

            folders.EnsureIndex(x => x.ItemId);
            folders.EnsureIndex(x => x.UserId);
            folders.EnsureIndex(x => x.LastModified);
            folders.EnsureIndex(x => x.Status);
            folders.EnsureIndex(x => x.MediaType);
            items.EnsureIndex(x => x.ItemId);
            items.EnsureIndex(x => x.LastModified);
            items.EnsureIndex(x => x.Status);
            items.EnsureIndex(x => x.MediaType);
            userinfos.EnsureIndex(x => x.ItemId);
            userinfos.EnsureIndex(x => x.UserId);
            userinfos.EnsureIndex(x => x.LastModified);
            userinfos.EnsureIndex(x => x.MediaType);
        }      

        public List<Guid> GetItems(long dtl, int status, bool movies, bool tvshows, bool music, bool musicvideos, bool boxsets)
        {
            using (var trn = DB.BeginTrans())
            {
                var result = new List<Guid>();
                List<ItemRec> final = new List<ItemRec>();

                _logger.Debug(String.Format("Emby.Kodi.SyncQueue:  Using dtl {0:yyyy-MM-dd HH:mm:ss} for time {1}", new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(dtl), dtl));
                _logger.Debug(String.Format("Emby.Kodi.SyncQueue:  IntStatus: {0}", status));

                var itms = items.Find(x => x.LastModified > dtl &&
                                           x.Status == status).ToList();
                result = itms.Where(x =>
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

                itms.ForEach(i =>
                {
                    _logger.Debug(result.ToString());
                    _logger.Debug(_json.SerializeToString(i));
                    _logger.Debug(String.Format("Emby.Kodi.SyncQueue:  Item {0} {1} {2:yyyy-MM-dd HH:mm:ss} for time {3}", i.ItemId, status,
                                new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(i.LastModified), i.LastModified));
                });
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
            using (var trn = DB.BeginTrans())
            {
                var result = new List<UserJson>();
                var tids = new List<string>();
                var final = new List<UserInfoRec>();

                var uids = userinfos.Find(x => x.LastModified > dtl &&
                                               x.UserId == userId).ToList();
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
        }

        public void DeleteOldData(long dtl)
        {
            using (var trn = DB.BeginTrans())
            {
                try
                { 
                    _logger.Info("Emby.Kodi.SyncQueue.Task: Starting Folder Retention Deletion...");
                    folders.Delete(x => x.LastModified < dtl);
                    _logger.Info("Emby.Kodi.SyncQueue.Task: Finished Folder Retention Deletion...");

                    _logger.Info("Emby.Kodi.SyncQueue.Task: Starting Item Retention Deletion...");
                    items.Delete(x => x.LastModified < dtl);
                    _logger.Info("Emby.Kodi.SyncQueue.Task: Finished Item Retention Deletion...");

                    _logger.Info("Emby.Kodi.SyncQueue.Task: Starting UserItem Retention Deletion...");
                    userinfos.Delete(x => x.LastModified < dtl);
                    _logger.Info("Emby.Kodi.SyncQueue.Task: Finished UserItem Retention Deletion...");

                    trn.Commit();
                }
                catch (Exception)
                {               
                    trn.Rollback();
                    throw;
                }
            }
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
                using (var trn = DB.BeginTrans())
                {
                    try
                    {
                        long newTime;

                        newTime = i.SyncApiModified;

                        ItemRec rec = items.Find(x => x.ItemId == i.Id).FirstOrDefault();

                        newRec = new ItemRec()
                        {
                            ItemId = i.Id,
                            Status = status,
                            LastModified = newTime,
                            MediaType = i.ItemType
                        };

                        if (rec == null) { items.Insert(newRec); }
                        else if (rec.LastModified < newTime)
                        {
                            newRec.Id = rec.Id;
                            items.Update(newRec);
                        }
                        else { newRec = null; }

                        if (newRec != null)
                        {
                            _logger.Debug(String.Format("Emby.Kodi.SyncQueue:  {0} ItemId: '{1}'", statusType, newRec.ItemId.ToString("N")));
                        }
                        else
                        {
                            _logger.Debug(String.Format("Emby.Kodi.SyncQueue:  ItemId: '{0}' Skipped", i.Id.ToString("N")));
                        }
                        trn.Commit();
                    }
                    catch (Exception ex)
                    {
                        trn.Rollback();
                        throw ex;
                    }
                }
            });
        }

        public void SetUserInfoSync(List<MediaBrowser.Model.Dto.UserItemDataDto> dtos, List<LibItem> itemRefs, string userName, string userId, CancellationToken cancellationToken)
        {
            dtos.ForEach(dto =>
            {
                using (var trn = DB.BeginTrans())
                {
                    try
                    {
                        var json = _json.SerializeToString(dto).ToString();
                        _logger.Debug("Emby.Kodi.SyncQueue:  Updating ItemId '{0}' for UserId: '{1}'", dto.ItemId, userId);

                        LibItem itemref = itemRefs.Where(x => x.Id.ToString("N") == dto.ItemId).FirstOrDefault();
                        if (itemref != null)
                        {
                            UserInfoRec oldRec = userinfos.Find(x => x.ItemId == dto.ItemId && x.UserId == userId)
                                                            .FirstOrDefault();
                            var newRec = new UserInfoRec()
                            {
                                ItemId = dto.ItemId,
                                Json = json,
                                UserId = userId,
                                LastModified = (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds),
                                MediaType = itemref.ItemType,
                                //LibraryName = itemref.CollectionName
                            };
                            if (oldRec == null)
                            {
                                userinfos.Insert(newRec);
                            }
                            else
                            {
                                newRec.Id = oldRec.Id;
                                userinfos.Update(newRec);

                            }
                        }
                        trn.Commit();
                    }
                    catch (Exception)
                    {
                        trn.Rollback();
                        throw;
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
                if (DB != null) { DB.Dispose(); }
            }
        }

        #endregion
    }
}
