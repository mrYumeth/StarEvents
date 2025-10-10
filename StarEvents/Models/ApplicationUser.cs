using Microsoft.AspNetCore.Identity;
using StarEvents.Models.Payments;
using System.Collections.Generic;

namespace StarEvents.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Custom properties for the user
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public int LoyaltyPoints { get; set; } = 0;

        // Navigation properties for relationships
        public virtual ICollection<Booking>? Bookings { get; set; }
        public virtual ICollection<CustomerPayment>? Payments { get; set; }
        public virtual ICollection<Event>? OrganizedEvents { get; set; }
    }
}