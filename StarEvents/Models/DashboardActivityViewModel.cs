using System;

namespace StarEvents.Models
{
    public class DashboardActivityViewModel
    {
        public string Description { get; set; }
        public DateTime Timestamp { get; set; }
        public string ActivityType { get; set; } // e.g., "User", "Booking", "Event"
    }
}