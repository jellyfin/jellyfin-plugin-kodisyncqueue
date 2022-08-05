using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.KodiSyncQueue.ScheduledTasks
{
    public class RetentionTask : IScheduledTask
    {
        private readonly ILogger<RetentionTask> _logger;

        public RetentionTask(ILogger<RetentionTask> logger)
        {
            _logger = logger;
            _logger.LogInformation("Retention Task Scheduled!");
        }

        public string Name => "Remove Old Sync Data";

        public string Category => "KodiSyncQueue";

        public string Description => "If retention days > 0 then this will remove the old data to keep information flowing quickly";

        public string Key => "KodiSyncFireRetentionTask";

        public Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            // Is retDays 0.. If So Exit...
            if (!int.TryParse(KodiSyncQueuePlugin.Instance.Configuration.RetDays, out var retDays) || retDays == 0)
            {
                _logger.LogInformation("Retention deletion not possible if retention days is set to zero!");
                return Task.CompletedTask;
            }

            // Check Database
            var dt = DateTime.UtcNow.AddDays(-retDays);
            var dtl = (long)dt.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;

            KodiSyncQueuePlugin.Instance.DbRepo.DeleteOldData(dtl);

            return Task.CompletedTask;
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return new[]
            {
                new TaskTriggerInfo
                {
                    Type = TaskTriggerInfo.TriggerDaily,
                    TimeOfDayTicks = TimeSpan.FromMinutes(1).Ticks
                }
            };
        }
    }
}
