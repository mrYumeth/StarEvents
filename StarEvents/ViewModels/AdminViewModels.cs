using System.ComponentModel.DataAnnotations;
using StarEvents.Models;

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

        [Required(ErrorMessage = "Event title is required")]
        [StringLength(250, ErrorMessage = "Title cannot exceed 250 characters")]
        [Display(Name = "Event Title")]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Description")]
        public string? Description { get; set; }

        [StringLength(100, ErrorMessage = "Category cannot exceed 100 characters")]
        [Display(Name = "Category")]
        public string? Category { get; set; }

        [Required(ErrorMessage = "Start date is required")]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; } = DateTime.Now.AddDays(1);

        [Display(Name = "End Date")]
        public DateTime? EndDate { get; set; }

        [Required(ErrorMessage = "Status is required")]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Draft";

        [Required(ErrorMessage = "Venue is required")]
        [Display(Name = "Venue")]
        public int VenueId { get; set; }

        [Required(ErrorMessage = "Organizer is required")]
        [Display(Name = "Organizer")]
        public string OrganizerId { get; set; } = string.Empty;

        // Navigation properties for dropdowns
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
        public DateTime CreatedAt { get; set; }

        [Display(Name = "Full Name")]
        public string FullName => $"{FirstName} {LastName}";
    }

    public class AdminReportsViewModel
    {
        public decimal TotalRevenue { get; set; }
        public int TotalTicketsSold { get; set; }
        public List<CategoryStats> EventsByCategory { get; set; } = new();
        public List<MonthlyRevenue> RevenueByMonth { get; set; } = new();
        public List<TopEventStats> TopEvents { get; set; } = new();
    }

    public class CategoryStats
    {
        public string Category { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class MonthlyRevenue
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal Revenue { get; set; }
        public int TicketsSold { get; set; }

        [Display(Name = "Month")]
        public string MonthName => new DateTime(Year, Month, 1).ToString("MMMM yyyy");
    }

    public class TopEventStats
    {
        public string EventTitle { get; set; } = string.Empty;
        public int TicketsSold { get; set; }
        public decimal Revenue { get; set; }
    }
}
