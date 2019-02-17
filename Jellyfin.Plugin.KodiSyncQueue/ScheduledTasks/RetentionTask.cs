using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.KodiSyncQueue.Data;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.KodiSyncQueue.ScheduledTasks
{
    public class FireRetentionTask : IScheduledTask
    {
        private readonly ILogger _logger;

        public FireRetentionTask(ILogger logger)
        {
            _logger = logger;
            _logger.LogInformation("Retention Task Scheduled!");
        }

        public string Key => "KodiSyncFireRetentionTask";

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return new[] {

                new TaskTriggerInfo
                {
                    Type = TaskTriggerInfo.TriggerDaily,
                    TimeOfDayTicks = TimeSpan.FromMinutes(1).Ticks
                }
            };
        }

        public Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            //Is retDays 0.. If So Exit...
            if (!int.TryParse(Plugin.Instance.Configuration.RetDays, out var retDays) || retDays == 0) {
                _logger.LogInformation("Retention Deletion Not Possible When Retention Days = 0!");
                return Task.CompletedTask;
            }

            //Check Database
            var dt = DateTime.UtcNow.AddDays(-retDays);
            var dtl = (long)dt.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;

            Plugin.Instance.DbRepo.DeleteOldData(dtl);
            
            return Task.CompletedTask;
        }

        public string Name => "Remove Old Sync Data";
        public string Category => "Jellyfin.Plugin.KodiSyncQueue";
        public string Description => "If Retention Days > 0 then this will remove the old data to keep information flowing quickly";
    }

}
