using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using StarEvents.Data; // Ensure this is the namespace where ApplicationUser is defined

namespace StarEvents.Models
{
    [Table("Events")]
    public class Event
    {
        [Key]
        public int Id { get; set; }

        [Required]
        // FIX: Change Guid to string to match the default IdentityUser Id type
        public string OrganizerId { get; set; } 
        // Navigation Property - assumes ApplicationUser is defined elsewhere
        public ApplicationUser Organizer { get; set; } 

        // Note: The ApplicationUser class definition must be moved outside of this file.
        
        [Required]
        public int VenueId { get; set; }

        [Required]
        [MaxLength(250)]
        public string Title { get; set; }

        public string Description { get; set; }

        [MaxLength(100)]
        public string Category { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } = "Draft";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}