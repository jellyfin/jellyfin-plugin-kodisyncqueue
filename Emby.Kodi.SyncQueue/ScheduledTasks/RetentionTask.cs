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

        public FireRetentionTask(ILogManager logManager, ILogger logger, IJsonSerializer jsonSerializer, IUserManager userManager, 
            IUserDataManager userDataManager, IHttpClient httpClient, IServerApplicationHost appHost, IApplicationPaths applicationPaths)
        {
            _jsonSerializer = jsonSerializer;
            _userManager = userManager;
            _userDataManager = userDataManager;
            _logger = logger;
            _logManager = logManager;
            _applicationPaths = applicationPaths;

            _logger.Info("Emby.Kodi.SyncQueue.Task: Retention Task Scheduled!");
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
            if (!(Int32.TryParse(Plugin.Instance.Configuration.RetDays, out retDays))) {
                _logger.Info("Emby.Kodi.SyncQueue.Task: Retention Deletion Not Possible When Retention Days = 0!");
                return;
            }

            if (retDays == 0)
            {
                _logger.Info("Emby.Kodi.SyncQueue.Task: Retention Deletion Not Possible When Retention Days = 0!");
                return;
            }

            //Check Database
            using (var dataHelper = new DataHelper(_logger, _jsonSerializer)) 
            {
                string dataPath = _applicationPaths.DataPath;

                dataHelper.CheckCreateFiles(dataPath);
                dataHelper.OpenConnection();                

                //Time to do some work!
                TimeSpan dtDiff;
                DateTime startTime;
                DateTime endTime;
                DateTime totalStart;
                DateTime totalEnd;
                DateTime dtNow = DateTime.UtcNow;
                DateTime retDate = new DateTime(dtNow.Year, dtNow.Month, dtNow.Day, 0, 0, 0);
            
                retDays = retDays * -1;
            
                retDate = retDate.AddDays(retDays);                   
            
                _logger.Info(String.Format("Emby.Kodi.SyncQueue.Task: Scheduled Retention Deletion to \"Trim the Fat\" Has Begun! Using Retention Date: {0}", retDate.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)));
                totalStart = DateTime.UtcNow;


                List<String> tables = new List<String>();
                
                int recNum = 0;
                double curProg;
                tables = await dataHelper.RetentionTablesAsync();
                foreach (String tableName in tables)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {                            
                        startTime = DateTime.UtcNow;
                        recChanged = await dataHelper.RetentionFixerAsync(tableName, retDate);
                        endTime = DateTime.UtcNow;
                        recNum++;
                        curProg = (recNum * 100) / tables.Count;
                        dtDiff = endTime - startTime;
                        progress.Report(curProg);
                        _logger.Info(String.Format("Emby.Kodi.SyncQueue.Task: Deleted {0} Records from table '{1}' in: {2}", recChanged, tableName, dtDiff));
                    }
                    catch (Exception e)
                    {
                        _logger.Info(String.Format("Emby.Kodi.SyncQueue.Task: Error Occurred {0}!", e.Message));
                        throw;
                    }
                }
                totalEnd = DateTime.UtcNow;          
                
                cancellationToken.ThrowIfCancellationRequested();
                await dataHelper.CleanupDatabaseAsync();

                dtDiff = totalEnd - totalStart;
                _logger.Info(String.Format("Emby.Kodi.SyncQueue.Task: Retention Deletion Has Finished in {0}!", dtDiff));
            }
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
