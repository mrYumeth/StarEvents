using StarEvents.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace StarEvents.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalEvents { get; set; }
        public int TotalVenues { get; set; }
        public int TotalBookings { get; set; }
        public List<Event> RecentEvents { get; set; } = new();
        public List<ApplicationUser> RecentUsers { get; set; } = new();
    }

    public class AdminEventViewModel
    {
        public int Id { get; set; }

        [Required, StringLength(250)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [StringLength(100)]
        public string? Category { get; set; }

        [Required]
        public DateTime StartDate { get; set; } = DateTime.UtcNow.AddDays(1);

        public DateTime? EndDate { get; set; }

        [Required]
        public string Status { get; set; } = "Draft";

        [Required]
        public int VenueId { get; set; }

        [Required]
        public string OrganizerId { get; set; } = string.Empty;

        [Range(0, double.MaxValue)]
        public decimal TicketPrice { get; set; } = 0m;

        [Range(0, int.MaxValue)]
        public int AvailableTickets { get; set; } = 0;

        public List<Venue> Venues { get; set; } = new();
        public List<ApplicationUser> Organizers { get; set; } = new();
    }

    public class AdminUserViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public int LoyaltyPoints { get; set; }
        public bool EmailConfirmed { get; set; }
        public List<string> Roles { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class AdminReportsViewModel
    {
        public decimal TotalRevenue { get; set; }
        public int TotalTicketsSold { get; set; }
        public List<CategoryStats> EventsByCategory { get; set; } = new();
        public List<MonthlyRevenue> RevenueByMonth { get; set; } = new();
        public List<TopEventStats> TopEvents { get; set; } = new();
    }

    public class CategoryStats { public string Category { get; set; } = ""; public int Count { get; set; } }
    public class MonthlyRevenue { public int Year { get; set; } public int Month { get; set; } public decimal Revenue { get; set; } public int TicketsSold { get; set; } }
    public class TopEventStats { public string EventTitle { get; set; } = ""; public int TicketsSold { get; set; } public decimal Revenue { get; set; } }
}
