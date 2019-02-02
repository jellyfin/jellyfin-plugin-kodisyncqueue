﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Common.Configuration;
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
        private readonly IApplicationPaths _applicationPaths;
        private readonly IUserManager _userManager;
        private readonly ILibraryManager _libraryManager;

        //private DbRepo dbRepo = null;
        //private DataHelper dataHelper;

        public SyncAPI(ILogger logger, IJsonSerializer jsonSerializer, IApplicationPaths applicationPaths, IUserManager userManager, ILibraryManager libraryManager)
        {
            _logger = logger;
            _jsonSerializer = jsonSerializer;
            _applicationPaths = applicationPaths;
            _userManager = userManager;
            _libraryManager = libraryManager;

            _logger.LogInformation("Jellyfin.Plugin.KodiSyncQueue:  SyncAPI Created and Listening at \"/Jellyfin.Plugin.KodiSyncQueue/{UserID}/{LastUpdateDT}/GetItems?format=json\" - {LastUpdateDT} must be a UTC DateTime formatted as yyyy-MM-ddTHH:mm:ssZ");
            _logger.LogInformation("Jellyfin.Plugin.KodiSyncQueue:  SyncAPI Created and Listening at \"/Jellyfin.Plugin.KodiSyncQueue/{UserID}/GetItems?LastUpdateDT={LastUpdateDT}&format=json\" - {LastUpdateDT} must be a UTC DateTime formatted as yyyy-MM-ddTHH:mm:ssZ");
            _logger.LogInformation("Jellyfin.Plugin.KodiSyncQueue:  The following parameters also exist to filter the results:");
            _logger.LogInformation("Jellyfin.Plugin.KodiSyncQueue:  filter=movies,tvshows,music,musicvideos,boxsets");
            _logger.LogInformation("Jellyfin.Plugin.KodiSyncQueue:  Results will be included by default and only filtered if added to the filter query...");
            _logger.LogInformation("Jellyfin.Plugin.KodiSyncQueue:  the filter query must be lowercase in both the name and the items...");

            //dbRepo = new DbRepo(_applicationPaths.DataPath, _logger, _jsonSerializer);          
            //DbRepo.dbPath = _applicationPaths.DataPath;
            //DbRepo.logger = _logger;
            //DbRepo.json = _jsonSerializer;
        }

        public SyncUpdateInfo Get(GetLibraryItems request)
        {
            _logger.LogInformation(String.Format("Jellyfin.Plugin.KodiSyncQueue:  Sync Requested for UserID: '{0}' with LastUpdateDT: '{1}'", request.UserID, request.LastUpdateDT));
            _logger.LogDebug("Jellyfin.Plugin.KodiSyncQueue:  Processing message...");
            var info = new SyncUpdateInfo();
            if (request.LastUpdateDT == null || request.LastUpdateDT == "")
                request.LastUpdateDT = "1900-01-01T00:00:00Z";
            bool movies = true;
            bool tvshows = true;
            bool music = true;
            bool musicvideos = true;
            bool boxsets = true;

            if (request.filter != null && request.filter != "")
            {
                var filter = request.filter.ToLower().Split(',');
                foreach (var f in filter)
                {
                    f.Trim();
                    switch (f)
                    {
                        case "movies":
                            movies = false;
                            break;
                        case "tvshows":
                            tvshows = false;
                            break;
                        case "music":
                            music = false;
                            break;
                        case "musicvideos":
                            musicvideos = false;
                            break;
                        case "boxsets":
                            boxsets = false;
                            break;
                    }
                }
            }

            Task<SyncUpdateInfo> x = PopulateLibraryInfo(
                                                            request.UserID,
                                                            request.LastUpdateDT,
                                                            movies,
                                                            tvshows,
                                                            music,
                                                            musicvideos,
                                                            boxsets
                                                        );
            Task.WhenAll(x);
            
            _logger.LogDebug("Jellyfin.Plugin.KodiSyncQueue:  Request processed... Returning result...");
            return x.Result;
        }

        public SyncUpdateInfo Get(GetLibraryItemsQuery request)
        {
            _logger.LogInformation(String.Format("Jellyfin.Plugin.KodiSyncQueue:  Sync Requested for UserID: '{0}' with LastUpdateDT: '{1}'", request.UserID, request.LastUpdateDT));
            _logger.LogDebug("Jellyfin.Plugin.KodiSyncQueue:  Processing message...");
            if (request.LastUpdateDT == null || request.LastUpdateDT == "")
                request.LastUpdateDT = "1900-01-01T00:00:00Z";
            bool movies = true;
            bool tvshows = true;
            bool music = true;
            bool musicvideos = true;
            bool boxsets = true;

            if (request.filter != null && request.filter != "")
            {
                var filter = request.filter.ToLower().Split(',');
                foreach (var f in filter)
                {
                    f.Trim();
                    switch (f)
                    {
                        case "movies":
                            movies = false;
                            break;
                        case "tvshows":
                            tvshows = false;
                            break;
                        case "music":
                            music = false;
                            break;
                        case "musicvideos":
                            musicvideos = false;
                            break;
                        case "boxsets":
                            boxsets = false;
                            break;
                    }
                }
            }

            Task<SyncUpdateInfo> x = PopulateLibraryInfo(
                                                            request.UserID, 
                                                            request.LastUpdateDT,
                                                            movies,
                                                            tvshows,
                                                            music,
                                                            musicvideos,
                                                            boxsets
                                                        );
            Task.WhenAll(x);

            _logger.LogDebug("Jellyfin.Plugin.KodiSyncQueue:  Request processed... Returning result...");
            return x.Result;
        }        

        public async Task<SyncUpdateInfo> PopulateLibraryInfo(string userId, string lastDT, 
                                                              bool movies, bool tvshows, bool music,
                                                              bool musicvideos, bool boxsets)
        {
            var startTime = DateTime.UtcNow;

            _logger.LogDebug("Jellyfin.Plugin.KodiSyncQueue:  Starting PopulateLibraryInfo...");
            var userDataChangedJson = new List<string>();
            var tmpList = new List<string>();

            var info = new SyncUpdateInfo();

            //var userDT = Convert.ToDateTime(lastDT);
            var userDT = DateTime.Parse(lastDT, CultureInfo.CurrentCulture, DateTimeStyles.AssumeUniversal);

            var dtl = (long)(userDT.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds);

            _logger.LogDebug("Jellyfin.Plugin.KodiSyncQueue:  PopulateLibraryInfo:  Getting Items Added Info...");
            Task<List<string>> t1 = Task.Run(() =>
            {
                List<string> result = null;
                List<Guid> data = null;


                data = DbRepo.Instance.GetItems(dtl, 0, movies, tvshows, music, musicvideos, boxsets);
                
                var user = _userManager.GetUserById(Guid.Parse(userId));

                List<BaseItem> items = new List<BaseItem>();
                data.ForEach(i =>
                {
                    var item = _libraryManager.GetItemById(i);
                    if (item != null)
                    {
                        items.Add(item);
                    }
                });

                result = items.SelectMany(i => ApiUserCheck.TranslatePhysicalItemToUserLibrary(i, user, _libraryManager)).Select(i => i.Id.ToString("N")).Distinct().ToList();
                
                if (result.Count > 0)
                {
                    _logger.LogInformation(String.Format("Jellyfin.Plugin.KodiSyncQueue:  Added Items Found: {0}", string.Join(",", result.ToArray())));
                }
                else
                {
                    _logger.LogInformation("Jellyfin.Plugin.KodiSyncQueue:  No Added Items Found!");
                }
                return result;
            });

            _logger.LogDebug("Jellyfin.Plugin.KodiSyncQueue:  PopulateLibraryInfo:  Getting Items Removed Info...");
            Task<List<string>> t2 = Task.Run(() =>
            {
                List<string> result = new List<string>();
                List<Guid> data = null;

                data = DbRepo.Instance.GetItems(dtl, 2, movies, tvshows, music, musicvideos, boxsets);
                
                //var user = _userManager.GetUserById(Guid.Parse(userId));

                //List<BaseItem> items = new List<BaseItem>();
                //data.ForEach(i =>
                //{
                //    var item = _libraryManager.GetItemById(i);
                //    if (item != null)
                //    {
                //        items.Add(item);
                //    }
                //});

                if (data != null && data.Count() > 0)
                {
                    data.ForEach(i =>
                    {
                        result.Add(i.ToString("N"));
                    });
                }

                //result = items.SelectMany(i => ApiUserCheck.TranslatePhysicalItemToUserLibrary(i, user, _libraryManager, true)).Select(i => i.Id.ToString("N")).Distinct().ToList();

                if (result.Count > 0)
                {
                    _logger.LogInformation(String.Format("Jellyfin.Plugin.KodiSyncQueue:  Removed Items Found: {0}", string.Join(",", result.ToArray())));
                }
                else
                {
                    _logger.LogInformation("Jellyfin.Plugin.KodiSyncQueue:  No Removed Items Found!");
                }
                return result;
            });

            _logger.LogDebug("Jellyfin.Plugin.KodiSyncQueue:  PopulateLibraryInfo:  Getting Items Updated Info...");
            Task<List<string>> t3 = Task.Run(() =>
            {
                List<string> result = null;
                List<Guid> data = null;

                data = DbRepo.Instance.GetItems(dtl, 1, movies, tvshows, music, musicvideos, boxsets);
                
                var user = _userManager.GetUserById(Guid.Parse(userId));

                List<BaseItem> items = new List<BaseItem>();
                data.ForEach(i =>
                {
                    var item = _libraryManager.GetItemById(i);
                    if (item != null)
                    {
                        items.Add(item);
                    }
                });

                result = items.SelectMany(i => ApiUserCheck.TranslatePhysicalItemToUserLibrary(i, user, _libraryManager)).Select(i => i.Id.ToString("N")).Distinct().ToList();


                if (result.Count > 0)
                {
                    _logger.LogInformation(String.Format("Jellyfin.Plugin.KodiSyncQueue:  Updated Items Found: {0}", string.Join(",", result.ToArray())));
                }
                else
                {
                    _logger.LogInformation("Jellyfin.Plugin.KodiSyncQueue:  No Updated Items Found!");
                }
                return result;
            });

            _logger.LogDebug("Jellyfin.Plugin.KodiSyncQueue:  PopulateLibraryInfo:  Getting Folders Added To Info...");
            info.FoldersAddedTo.Clear();
            _logger.LogDebug("Jellyfin.Plugin.KodiSyncQueue:  PopulateLibraryInfo:  Getting Folders Removed From Info...");
            info.FoldersRemovedFrom.Clear();
            _logger.LogDebug("Jellyfin.Plugin.KodiSyncQueue:  PopulateLibraryInfo:  Getting User Data Changed Info...");
            Task<List<string>> t4 = Task.Run(() =>
            {
                List<UserJson> data = null;          
                List<string> result = null;

                data = DbRepo.Instance.GetUserInfos(dtl, userId, movies, tvshows, music, musicvideos, boxsets);
                
                result = data.Select(i => i.JsonData).ToList();

                if (result.Count > 0)
                {
                    _logger.LogInformation(String.Format("Jellyfin.Plugin.KodiSyncQueue:  User Data Changed Info Found: {0}", string.Join(",", data.Select(i => i.Id).ToArray())));
                }
                else
                {
                    _logger.LogInformation("Jellyfin.Plugin.KodiSyncQueue:  No User Data Changed Info Found!");
                }
                
                return result;
            });

            await Task.WhenAll(t1, t2, t3, t4);

            info.ItemsAdded = t1.Result;
            info.ItemsRemoved = t2.Result;
            info.ItemsUpdated = t3.Result;
            userDataChangedJson = t4.Result;

            info.UserDataChanged = userDataChangedJson.Select(i => _jsonSerializer.DeserializeFromString<UserItemDataDto>(i)).ToList();

            TimeSpan diffDate = DateTime.UtcNow - startTime;
            _logger.LogInformation(String.Format("Jellyfin.Plugin.KodiSyncQueue: Request Finished Taking {0}", diffDate.ToString("c")));

            return info;
        }
    }
}