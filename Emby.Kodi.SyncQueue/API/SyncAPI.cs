using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Common.Configuration;
using ServiceStack;
using Emby.Kodi.SyncQueue.Helpers;
using System.Threading;

namespace Emby.Kodi.SyncQueue.API
{
    //OLD METHOD LEFT IN FOR BACKWARDS COMPATIBILITY
    [Route("/Emby.Kodi.SyncQueue/{UserID}/{LastUpdateDT}/GetItems", "GET", Summary = "Gets Items for {USER} from {UTC DATETIME} formatted as yyyy-MM-ddThh:mm:ssZ")]
    public class GetLibraryItems : IReturn<SyncUpdateInfo>
    {
        [ApiMember(Name = "UserID", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        [ApiMember(Name = "LastUpdateDT", Description = "UTC DateTime of Last Update, Format yyyy-MM-ddTHH:mm:ssZ", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string UserID { get; set; }
        public string LastUpdateDT { get; set; }
    }

    [Route("/Emby.Kodi.SyncQueue/{UserID}/GetItems", "GET", Summary = "Gets Items for {UserID} from {UTC DATETIME} formatted as yyyy-MM-ddTHH:mm:ssZ using queryString LastUpdateDT")]
    public class GetLibraryItemsQuery : IReturn<SyncUpdateInfo>
    {
        [ApiMember(Name = "UserID", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        [ApiMember(Name = "LastUpdateDT", Description = "UTC DateTime of Last Update, Format yyyy-MM-ddTHH:mm:ssZ", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string UserID { get; set; }
        public string LastUpdateDT { get; set; }
    }

    [Route("/Emby.Kodi.SyncQueue/GetServerDateTime", "GET", Summary = "Gets The Server Time in UTC format as yyyy-MM-ddTHH:mm:ssZ")]
    public class GetServerTime : IReturn<ServerTimeInfo>
    {
        
    }

    public class ServerTimeInfo
    {
        public String ServerDateTime { get; set; }
        public String RetentionDateTime { get; set; }

        public ServerTimeInfo()
        {
            ServerDateTime = "";
            RetentionDateTime = "";
        }
    }

    public class SyncUpdateInfo
    {
        public List<string> FoldersAddedTo { get; set; }
        public List<string> FoldersRemovedFrom { get; set; }
        public List<string> ItemsAdded { get; set; }
        public List<string> ItemsRemoved { get; set; }
        public List<string> ItemsUpdated { get; set; }
        public List<UserItemDataDto> UserDataChanged { get; set; }

        public SyncUpdateInfo()
        {
            FoldersAddedTo = new List<string>();
            FoldersRemovedFrom = new List<string>();
            ItemsAdded = new List<string>();
            ItemsRemoved = new List<string>();
            ItemsUpdated = new List<string>();
            UserDataChanged = new List<UserItemDataDto>();
        }
    }

    class ServerTimeAPI: IRestfulService
    {
        private readonly ILogger _logger;
        private readonly IJsonSerializer _jsonSerializer;
        public ServerTimeAPI(ILogger logger, IJsonSerializer jsonSerializer)
        {
            _logger = logger;
            _jsonSerializer = jsonSerializer;
        }

        public ServerTimeInfo Get(GetServerTime request)
        {
            _logger.Info("Emby.Kodi.SyncQueue: Server Time Requested...");
            var info = new ServerTimeInfo();
            _logger.Debug("Emby.Kodi.SyncQueue: Class Variable Created!");
            int retDays = 0;
            DateTime dtNow = DateTime.UtcNow;
            DateTime retDate;

            if (!(Int32.TryParse(Plugin.Instance.Configuration.RetDays, out retDays)))
            {
                retDays = 0;
            }

            if (retDays == 0)
            {
                retDate = new DateTime(1900, 1, 1, 0, 0, 0);
            }
            else
            {
                retDays = retDays * -1;
                retDate = new DateTime(dtNow.Year, dtNow.Month, dtNow.Day, 0, 0, 0);
                retDate = retDate.AddDays(retDays);
            }
            _logger.Debug("Emby.Kodi.SyncQueue: Getting Ready to Set Variables!");
            info.ServerDateTime = String.Format("{0}", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture));
            info.RetentionDateTime = String.Format("{0}", retDate.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)); 
            
            _logger.Debug(String.Format("Emby.Kodi.SyncQueue: ServerDateTime = {0}", info.ServerDateTime));
            _logger.Debug(String.Format("Emby.Kodi.SyncQueue: RetentionDateTime = {0}", info.RetentionDateTime));

            return info;
        }
    }

    

    class SyncAPI : IRestfulService
    {

        private readonly ILogger _logger;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IApplicationPaths _applicationPaths;

        //private DataHelper dataHelper;

        public SyncAPI(ILogger logger, IJsonSerializer jsonSerializer, IApplicationPaths applicationPaths)
        {
            _logger = logger;
            _jsonSerializer = jsonSerializer;
            _applicationPaths = applicationPaths;

            _logger.Debug("Emby.Kodi.SyncQueue:  SyncAPI Created and Listening at \"/Emby.Kodi.SyncQueue/{UserID}/{LastUpdateDT}/GetItems?format=json\" - {LastUpdateDT} must be a UTC DateTime formatted as yyyy-MM-ddTHH:mm:ssZ");
            _logger.Debug("Emby.Kodi.SyncQueue:  SyncAPI Created and Listening at \"/Emby.Kodi.SyncQueue/{UserID}/GetItems?LastUpdateDT={LastUpdateDT}&format=json\" - {LastUpdateDT} must be a UTC DateTime formatted as yyyy-MM-ddTHH:mm:ssZ");
        }

        public SyncUpdateInfo Get(GetLibraryItems request)
        {
            _logger.Info(String.Format("Emby.Kodi.SyncQueue:  Sync Requested for UserID: '{0}' with LastUpdateDT: '{1}'", request.UserID, request.LastUpdateDT));
            _logger.Debug("Emby.Kodi.SyncQueue:  Processing message...");
            var info = new SyncUpdateInfo();
            if (request.LastUpdateDT == null || request.LastUpdateDT == "")
                request.LastUpdateDT = "2010-01-01T00:00:00Z";

            Task<SyncUpdateInfo> x = PopulateLibraryInfo(request.UserID, request.LastUpdateDT);
            Task.WhenAll(x);
            
            _logger.Debug("Emby.Kodi.SyncQueue:  Request processed... Returning result...");
            return x.Result;
        }

        public SyncUpdateInfo Get(GetLibraryItemsQuery request)
        {
            _logger.Info(String.Format("Emby.Kodi.SyncQueue:  Sync Requested for UserID: '{0}' with LastUpdateDT: '{1}'", request.UserID, request.LastUpdateDT));
            _logger.Debug("Emby.Kodi.SyncQueue:  Processing message...");
            if (request.LastUpdateDT == null || request.LastUpdateDT == "")
                request.LastUpdateDT = "2010-01-01T00:00:00Z";

            Task<SyncUpdateInfo> x = PopulateLibraryInfo(request.UserID, request.LastUpdateDT);
            Task.WhenAll(x);

            _logger.Debug("Emby.Kodi.SyncQueue:  Request processed... Returning result...");
            return x.Result;
        }

        public async Task<SyncUpdateInfo> PopulateLibraryInfo(string userId, string lastDT)
        {
            using (var dataHelper = new DataHelper(_logger, _jsonSerializer))
            {
                var startTime = DateTime.UtcNow;
                string dataPath = _applicationPaths.DataPath;
                dataHelper.CheckCreateFiles(dataPath);
                dataHelper.OpenConnection();
                
                _logger.Debug("Emby.Kodi.SyncQueue:  Starting PopulateLibraryInfo...");
                var userDataChangedJson = new List<string>();
                var tmpList = new List<string>();

                var info = new SyncUpdateInfo();

                _logger.Debug("Emby.Kodi.SyncQueue:  PopulateLibraryInfo:  Getting Items Added Info...");
                Task<List<string>> t1 = dataHelper.FillItemsAddedAsync(userId, lastDT);
                //info.ItemsAdded = dataHelper.FillItemsAdded(userId, lastDT);
                _logger.Debug("Emby.Kodi.SyncQueue:  PopulateLibraryInfo:  Getting Items Removed Info...");
                Task<List<string>> t2 = dataHelper.FillItemsRemovedAsync(userId, lastDT);
                //info.ItemsRemoved = dataHelper.FillItemsRemoved(userId, lastDT);
                _logger.Debug("Emby.Kodi.SyncQueue:  PopulateLibraryInfo:  Getting Items Updated Info...");
                Task<List<string>> t3 = dataHelper.FillItemsUpdatedAsync(userId, lastDT);
                //info.ItemsUpdated = dataHelper.FillItemsUpdated(userId, lastDT);
                _logger.Debug("Emby.Kodi.SyncQueue:  PopulateLibraryInfo:  Getting Folders Added To Info...");
                //info.FoldersAddedTo = dataHelper.FillFoldersAddedTo(userId, lastDT);
                info.FoldersAddedTo.Clear();
                _logger.Debug("Emby.Kodi.SyncQueue:  PopulateLibraryInfo:  Getting Folders Removed From Info...");
                //info.FoldersRemovedFrom = dataHelper.FillFoldersRemovedFrom(userId, lastDT);
                info.FoldersRemovedFrom.Clear();
                _logger.Debug("Emby.Kodi.SyncQueue:  PopulateLibraryInfo:  Getting User Data Changed Info...");
                //userDataChangedJson = dataHelper.FillUserDataChanged(userId, lastDT);
                Task<List<string>> t4 = dataHelper.FillUserDataChangedAsync(userId, lastDT);
                _logger.Debug("Emby.Kodi.SyncQueue:  PopulateLibraryInfo:  Parsing User Data Changed Info...");

                await Task.WhenAll(t1, t2, t3, t4);

                info.ItemsAdded = t1.Result;
                info.ItemsRemoved = t2.Result;
                info.ItemsUpdated = t3.Result;
                userDataChangedJson = t4.Result;

                foreach (var userData in userDataChangedJson)
                {
                    info.UserDataChanged.Add(_jsonSerializer.DeserializeFromString<UserItemDataDto>(userData));
                }

                var json = _jsonSerializer.SerializeToString(info.UserDataChanged).ToString();
                _logger.Debug(json);
                TimeSpan diffDate = DateTime.UtcNow - startTime;
                _logger.Info(String.Format("Emby.Kodi.SyncQueue: Request Finished Taking {0}", diffDate.ToString("c")));

                return info;                
            }
        }
    }
}
