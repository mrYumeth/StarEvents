using System;

namespace StarEvents.Models
{
    public class DashboardActivityViewModel
    {
        /// This model structures data for recent activities, such as user registrations
        public string Description { get; set; }
        public DateTime Timestamp { get; set; }
        public string ActivityType { get; set; } 
    }
}




