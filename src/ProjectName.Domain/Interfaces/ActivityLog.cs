using System;
using System.Collections.Generic;
using System.Text;

namespace ProjectName.Domain.Interfaces
{
    public interface IWisdomLogger
    {
        Task LogActivity(string activityType, string summary, string details);
        Task<IEnumerable<ActivityLog>> GetRecentActivityLogs(int count = 100);
    }

    public class ActivityLog
    {
        public Guid Id { get; set; }
        public string ActivityType { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
