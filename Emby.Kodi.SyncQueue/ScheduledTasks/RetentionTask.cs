using System.Globalization;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Common.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
//using CommonIO;
using Emby.Kodi.SyncQueue;
using Emby.Kodi.SyncQueue.Helpers;

namespace Emby.Kodi.SyncQueue.ScheduledTasks
{
    public class FireRetentionTask : IScheduledTask
    {
        //private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IUserManager _userManager;
        private readonly ILogger _logger;
        private readonly ILogManager _logManager;
        private readonly IUserDataManager _userDataManager;
        private readonly IApplicationPaths _applicationPaths;
        private DataHelper dataHelper;
        

        public FireRetentionTask(ILogManager logManager, ILogger logger, IJsonSerializer jsonSerializer, IUserManager userManager, IUserDataManager userDataManager, IHttpClient httpClient, IServerApplicationHost appHost, IApplicationPaths applicationPaths)
        {
            _jsonSerializer = jsonSerializer;
            _userManager = userManager;
            _userDataManager = userDataManager;
            _logger = logger;
            _logManager = logManager;
            _applicationPaths = applicationPaths;

            _logger.Info("Emby.Kodi.SyncQueue.Task: Has Started!");
        }

        public IEnumerable<ITaskTrigger> GetDefaultTriggers()
        {

            //
            return new ITaskTrigger[]
            {
                new DailyTrigger{ TimeOfDay = TimeSpan.FromMinutes(1) }
            };
        }

        public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            
            //Is retDays 0.. If So Exit...
            int retDays;
            int recChanged;
            TimeSpan dtDiff;
            DateTime startTime;
            DateTime endTime;
            DateTime totalStart;
            DateTime totalEnd;

            if (!(Int32.TryParse(Plugin.Instance.Configuration.RetDays, out retDays))) {
                _logger.Info("Emby.Kodi.SyncQueue.Task: Retention Deletion Not Possible When Retention Days = 0!");
                return;
            }
                   
            
            _logger.Info("Emby.Kodi.SyncQueue.Task: Scheduled Retention Deletion to \"Trim the Fat\" Has Begun!");
            totalStart = DateTime.UtcNow;
            dataHelper = new DataHelper(_logger, _jsonSerializer);
            string dataPath = _applicationPaths.DataPath;
            bool result;

            result = dataHelper.CheckCreateFiles(dataPath);

            if (result)
                result = dataHelper.OpenConnection();
            if (result)
                result = dataHelper.CreateUserTable("UserInfoChangedQueue", "UICQUnique");

            if (!result)
            {
                throw new ApplicationException("Emby.Kodi.SyncQueue:  Could Not Be Loaded Due To Previous Error!");
            }
           
            retDays = retDays * -1;
            DateTime dtNow = System.DateTime.UtcNow;
            DateTime retDate = new DateTime(dtNow.Year, dtNow.Month, dtNow.Day, 0, 0, 0);
            retDate = retDate.AddDays(retDays);

            List<String> tables = new List<String>();
            int recNum = 0;
            double curProg;

            tables = await dataHelper.RetentionTables();
            foreach (String tableName in tables){
                startTime = DateTime.UtcNow;
                recChanged = await dataHelper.RetentionFixer(tableName, retDate);
                endTime = DateTime.UtcNow;
                recNum++;
                curProg = (recNum * 100)/tables.Count;
                dtDiff = endTime - startTime;
                progress.Report(curProg);
                _logger.Info(String.Format("Emby.Kodi.SyncQueue.Task: Deleted {0} Records from table '{1}' in: {2}", recChanged, tableName, dtDiff));

            }
            totalEnd = DateTime.UtcNow;
            dtDiff = totalEnd - totalStart;
            _logger.Info(String.Format("Emby.Kodi.SyncQueue.Tast: Retention Deletion Has Finished in {0}!", dtDiff));
        }

        public string Name
        {
            get { return "Remove Old Sync Data"; }
        }

        public string Category
        {
            get
            {
                return "Emby.Kodi.SyncQueue";
            }
        }

        public string Description
        {
            get
            {
                return
                    "If Retention Days > 0 then this will remove the old data to keep information flowing quickly";
            }
        }
    }

}
