using Microsoft.Extensions.Caching.Memory;
using ProjectName.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProjectName.Infrastructure.Logging
{
    public class WisdomLogger : IWisdomLogger
    {
        private readonly IMemoryCache _cache;
        private readonly string _logsKey = "ActivityLogs";

        public WisdomLogger(IMemoryCache cache)
        {
            _cache = cache;

            // Initialize logs collection if it doesn't exist
            if (!_cache.TryGetValue(_logsKey, out List<ActivityLog>? logs))
            {
                _cache.Set(_logsKey, new List<ActivityLog>());
            }
        }

        public async Task LogActivity(string activityType, string summary, string details)
        {
            var logs = _cache.Get<List<ActivityLog>>(_logsKey) ?? new List<ActivityLog>();

            logs.Add(new ActivityLog
            {
                Id = Guid.NewGuid(),
                ActivityType = activityType,
                Summary = summary,
                Details = details,
                Timestamp = DateTime.UtcNow
            });

            // Trim the logs if they get too large
            if (logs.Count > 1000)
            {
                logs = logs.OrderByDescending(l => l.Timestamp).Take(1000).ToList();
            }

            _cache.Set(_logsKey, logs);

            await Task.CompletedTask;
        }

        public async Task<IEnumerable<ActivityLog>> GetRecentActivityLogs(int count = 100)
        {
            var logs = _cache.Get<List<ActivityLog>>(_logsKey) ?? new List<ActivityLog>();

            return await Task.FromResult(logs.OrderByDescending(l => l.Timestamp).Take(count));
        }
    }
}
