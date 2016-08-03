using Emby.Kodi.SyncQueue.Entities;
using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using VelocityDb;
using VelocityDb.Session;
using System.IO;

namespace Emby.Kodi.SyncQueue.Data
{
    public static class DbRepo
    {
        private static readonly object _syncLock = new object();
        private static readonly object _sessLock = new object();
        private static string dataPath;

        public static string DataPath
        {
            get { return dataPath; }
            set { dataPath = Path.Combine(value, "SyncData"); }
        }

        private static SessionNoServer GetSession()
        {
            lock (_sessLock)
            {
                if (DataPath == null) { throw new Exception("Invalid Data Path!"); }
                var result = new SessionNoServer(DataPath);
                result.BeginUpdate();
                try
                {
                    var folder = new FolderRec()
                    {
                        ItemId = "111",
                        UserId = "111",
                        LastModified = 12345,
                        Status = 0
                    };
                    var item = new ItemRec()
                    {
                        ItemId = "222",
                        UserId = "222",
                        LastModified = 12345,
                        Status = 0
                    };
                    var user = new UserInfoRec()
                    {
                        ItemId = "333",
                        UserId = "333",
                        LastModified = 12345,
                        Json = "{test: \"test\"}"
                    };
                    result.Persist(folder);
                    result.Persist(item);
                    result.Persist(user);
                    result.Commit();
                }
                catch
                {
                    result.Abort();
                }

                return result;
            }
        }

        public static void SetLibrarySync(List<string> items, List<LibItem> Items, string userId, string userName, string tableName, int status, CancellationToken cancellationToken, ILogger _logger)
        {
            lock (_sessLock)
            {
                ItemRec newRec;
                var statusType = string.Empty;
                if (status == 0) { statusType = "Added"; }
                else if (status == 1) { statusType = "Updated"; }
                else { statusType = "Removed"; }
                items.ForEach(i =>
                {
                    using (var session = GetSession())
                    {
                        try
                        {
                            session.BeginUpdate();
                            var item = Items.Where(itm => itm.Id.ToString("N") == i).FirstOrDefault();
                            long newTime;
                            if (item != null)
                            {
                                newTime = item.SyncApiModified;
                            }
                            else
                            {
                                newTime = (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds);
                            }

                            ItemRec rec = session.AllObjects<ItemRec>()
                                .Where(x => x.ItemId == i && x.UserId == userId)
                                .FirstOrDefault();
                        //Repo.Items.FindOne(x => x.ItemId == i);
                        newRec = new ItemRec()
                            {
                                ItemId = i,
                                UserId = userId,
                                Status = status,
                                LastModified = newTime
                            };
                        //if (rec == null) { Repo.Items.Insert(newRec); }
                        if (rec == null) { session.Persist(newRec); }
                            else if (rec.LastModified < newTime)
                            {
                                newRec.Id = rec.Id;
                                session.Persist(newRec);
                            //Repo.Items.Update(newRec);
                        }
                            else { newRec = null; }

                            if (newRec != null)
                            {
                                _logger.Debug(String.Format("Emby.Kodi.SyncQueue:  {0} ItemId: '{1}' for UserId: '{2}'", statusType, newRec.ItemId, newRec.UserId));
                            }
                            else
                            {
                                _logger.Debug(String.Format("Emby.Kodi.SyncQueue:  ItemId: '{0}' Skipped for UserId: '{1}'", i, userId));
                            }
                            session.Commit();
                        }
                        catch (Exception ex)
                        {
                            session.Abort();
                            throw ex;
                        }
                    }
                });
            }
        }

        public static void SetUserInfoSync(List<MediaBrowser.Model.Dto.UserItemDataDto> dtos, string userName, string userId, CancellationToken cancellationToken, ILogger _logger, IJsonSerializer _jsonSerializer)
        {
            lock (_sessLock)
            {
                //Repo.DB.BeginTrans();
                dtos.ForEach(dto =>
                {
                    using (var session = GetSession())
                    {
                        try
                        {
                            session.BeginUpdate();
                            var json = _jsonSerializer.SerializeToString(dto).ToString();
                            _logger.Debug("Emby.Kodi.SyncQueue:  Updating ItemId '{0}' for UserId: '{1}'", dto.ItemId, userId);

                        //var oldRec = Repo.UserInfos.FindOne(x => x.UserId == userId && x.ItemId == dto.ItemId);
                        UserInfoRec oldRec = session.AllObjects<UserInfoRec>()
                                .Where(x => x.ItemId == dto.ItemId && x.UserId == userId)
                                .FirstOrDefault();
                            var newRec = new UserInfoRec()
                            {
                                ItemId = dto.ItemId,
                                Json = json,
                                UserId = userId,
                                LastModified = (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds)
                            };
                            if (oldRec != null)
                            {
                                newRec.Id = oldRec.Id;
                            }
                            session.Persist(newRec);
                            session.Commit();
                        }
                        catch (Exception ex)
                        {
                            session.Abort();
                            throw ex;
                        }

                    }
                });
            }
        }

        public static void DeleteOldData(long dtl, ILogger _logger)
        {
            lock (_sessLock)
            {
                using (var session = GetSession())
                {
                    try
                    {
                        session.BeginUpdate();
                        _logger.Info("Emby.Kodi.SyncQueue.Task: Starting Folder Retention Deletion...");
                        var folders = session.AllObjects<FolderRec>()
                                             .Where(x => x.LastModified < dtl)
                                             .ToList();
                        folders.ForEach(x =>
                        {
                            _logger.Info("Emby.Kodi.SyncQueue.Task: Deleting Folder: " + x.Id.ToString());
                            session.Unpersist(x);
                        });
                        _logger.Info("Emby.Kodi.SyncQueue.Task: Finished Folder Retention Deletion...");
                        _logger.Info("Emby.Kodi.SyncQueue.Task: Starting Item Retention Deletion...");
                        var recs = session.AllObjects<ItemRec>()
                                          .Where(x => x.LastModified < dtl)
                                          .ToList();
                        recs.ForEach(x =>
                        {
                            _logger.Info("Emby.Kodi.SyncQueue.Task: Deleting Item: " + x.Id.ToString());
                            session.Unpersist(x);
                        });
                        _logger.Info("Emby.Kodi.SyncQueue.Task: Finished Item Retention Deletion...");
                        _logger.Info("Emby.Kodi.SyncQueue.Task: Starting UserItem Retention Deletion...");
                        var uis = session.AllObjects<UserInfoRec>()
                                         .Where(x => x.LastModified < dtl)
                                         .ToList();
                        uis.ForEach(x =>
                        {
                            _logger.Info("Emby.Kodi.SyncQueue.Task: Deleting User Info: " + x.Id.ToString());
                            session.Unpersist(x);
                        });
                        _logger.Info("Emby.Kodi.SyncQueue.Task: Finished UserItem Retention Deletion...");

                        //repo.DB.BeginTrans().Commit();
                        session.Commit();
                    }
                    catch (Exception ex)
                    {
                        //repo.DB.BeginTrans().Rollback();                
                        session.Abort();
                        throw ex;
                    }
                    finally
                    {
                        //FOR DEBUGGING ONLY
                        var items = session.AllObjects<ItemRec>().ToList(); //repo.Items.FindAll();
                        items.ForEach(x => { _logger.Info("Emby.Kodi.SyncQueue.Task:  " + x.ItemId + " - " + x.LastModified); });
                        //END FOR DEBUGGING ONLY
                    }
                }
            }
        }

        public static List<string> GetItems(long dtl, int status, string userId)
        {
            lock (_sessLock)
            {
                var result = new List<string>();
                //using (var repo = new DbRepo(_applicationPaths.DataPath))
                using (var session = GetSession())
                {
                    session.BeginRead();
                    //var items = repo.Items.Find(x => x.LastModified >= dtl && x.Status == 0);
                    var items = session.AllObjects<ItemRec>().Where(x => x.LastModified >= dtl && x.Status == 0 && x.UserId == userId).ToList();
                    items.ForEach(x =>
                    {
                        if (result.Where(i => i == x.ItemId).FirstOrDefault() == null)
                        {
                            result.Add(x.ItemId);
                        }
                    });
                    session.Commit();
                }

                return result;
            }
        }

        public static List<string> GetFolders(long dtl, int status, string userId)
        {
            lock (_sessLock)
            {
                var result = new List<string>();
                //using (var repo = new DbRepo(_applicationPaths.DataPath))
                using (var session = GetSession())
                {
                    session.BeginRead();
                    //var items = repo.Items.Find(x => x.LastModified >= dtl && x.Status == 0);
                    var items = session.AllObjects<FolderRec>().Where(x => x.LastModified >= dtl && x.Status == 0 && x.UserId == userId).ToList();
                    items.ForEach(x =>
                    {
                        if (result.Where(i => i == x.ItemId).FirstOrDefault() == null)
                        {
                            result.Add(x.ItemId);
                        }
                    });
                    session.Commit();
                }

                return result;
            }
        }

        public static List<string> GetUserInfos(long dtl, string userId, out List<string> ids)
        {
            lock (_sessLock)
            {
                var result = new List<string>();
                var tids = new List<string>();
                //using (var repo = new DbRepo(_applicationPaths.DataPath))
                using (var session = GetSession())
                {
                    session.BeginRead();
                    //var items = repo.Items.Find(x => x.LastModified >= dtl && x.Status == 0);
                    var items = session.AllObjects<UserInfoRec>().Where(x => x.LastModified >= dtl && x.UserId == userId).ToList();
                    items.ForEach(x =>
                    {
                        result.Add(x.Json);
                        tids.Add(x.ItemId);
                    });
                    session.Commit();
                }

                ids = tids;
                return result;
            }
        }
        //public static DbRepo()
        //{
        //    var dataPath = Path.Combine(embyDataPath, "SyncData");
        //    //var dbName = "Emby.Kodi.SyncQueue.litedb";

        //    bool exists = false;
        //    if (File.Exists(dataPath)) { exists = true; }

        //    DocStore = new EmbeddableDocumentStore { DataDirectory = dataPath, DefaultDatabase = "Emby.Kodi.SyncQueue" };


        //    DB = new LiteDatabase(Path.Combine(embyDataPath, "SyncData", "Emby.Kodi.SyncQueue.litedb"));
        //    var mapper = BsonMapper.Global;

        //    Folders = DB.GetCollection<FolderRec>("Folders");            
        //    Items = DB.GetCollection<ItemRec>("Items");
        //    UserInfos = DB.GetCollection<UserInfoRec>("UserInfos");
        //    Folders.EnsureIndex(x => x.ItemId);
        //    Items.EnsureIndex(x => x.ItemId);
        //    UserInfos.EnsureIndex(x => x.ItemId);
        //    Folders.EnsureIndex(x => x.UserId);
        //    Items.EnsureIndex(x => x.UserId);
        //    UserInfos.EnsureIndex(x => x.UserId);
        //    Folders.EnsureIndex(x => x.LastModified);
        //    Items.EnsureIndex(x => x.LastModified);
        //    UserInfos.EnsureIndex(x => x.LastModified);

        //    if (!exists)
        //    {
        //        var test = new ItemRec()
        //        {
        //            Id = new Guid(),
        //            ItemId = "1111111",
        //            Status = 0,
        //            UserId = "1111111",
        //            LastModified = 12345
        //        };
        //        Items.Insert(test);
        //        test = new ItemRec()
        //        {
        //            Id = new Guid(),
        //            ItemId = "2222222",
        //            Status = 0,
        //            UserId = "2222222",
        //            LastModified = 12345
        //        };
        //        Items.Insert(test);
        //        test = new ItemRec()
        //        {
        //            Id = new Guid(),
        //            ItemId = "3333333",
        //            Status = 1,
        //            UserId = "3333333",
        //            LastModified = 12345
        //        };
        //        Items.Insert(test);
        //        test = new ItemRec()
        //        {
        //            Id = new Guid(),
        //            ItemId = "4444444",
        //            Status = 1,
        //            UserId = "4444444",
        //            LastModified = 12345
        //        };
        //        Items.Insert(test);
        //        test = new ItemRec()
        //        {
        //            Id = new Guid(),
        //            ItemId = "5555555",
        //            Status = 2,
        //            UserId = "5555555",
        //            LastModified = 12345
        //        };
        //        Items.Insert(test);
        //        test = new ItemRec()
        //        {
        //            Id = new Guid(),
        //            ItemId = "666666666",
        //            Status = 2,
        //            UserId = "666666666",
        //            LastModified = 12345
        //        };
        //        Items.Insert(test);
        //    }
        //}

        //#region Dispose

        //public void Dispose()
        //{            
        //    Dispose(true);
        //}

        //protected virtual void Dispose(bool dispose)
        //{
        //    if (dispose)
        //    {
        //        DB.Dispose();
        //    }
        //}

        //#endregion
    }
}
