using StarEvents.Models;
using System.Collections.Generic;

namespace StarEvents.ViewModels
{
    public class CustomerDashboardViewModel
    {
        public string UserName { get; set; }
        public int TotalBookings { get; set; }
        public int UpcomingEventsCount { get; set; }
        public int LoyaltyPoints { get; set; }
        public decimal TotalSpent { get; set; }
        public List<Booking> RecentBookings { get; set; }
        public List<Event> FeaturedEvents { get; set; }
    }
}