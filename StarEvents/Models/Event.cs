using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarEvents.Models
{
    [Table("Events")]
    public class Event
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public Guid OrganizerId { get; set; }

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
