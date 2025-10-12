using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace StarEvents.Models
{
    [Table("Events")]
    public class Event
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string OrganizerId { get; set; }

        // Navigation Property to Organizer
        [ForeignKey("OrganizerId")]
        public ApplicationUser Organizer { get; set; }

        [Required]
        public int VenueId { get; set; }

        // Navigation Property to Venue
        [ForeignKey("VenueId")]
        public Venue Venue { get; set; }

        [Required]
        [MaxLength(250)]
        [Display(Name = "Event Name")]
        public string Title { get; set; }

        [Display(Name = "Description")]
        public string Description { get; set; }

        [MaxLength(100)]
        [Display(Name = "Category")]
        public string Category { get; set; }

        [Required]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; }

        [Display(Name = "End Date")]
        public DateTime? EndDate { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Ticket Price")]
        public decimal TicketPrice { get; set; }

        [Display(Name = "Available Tickets")]
        public int? AvailableTickets { get; set; }

        [MaxLength(500)]
        [Display(Name = "Image URL")]
        public string? ImageUrl { get; set; }

        [NotMapped] // This attribute prevents EF Core from trying to save the file to the database
        public IFormFile ImageFile { get; set; }

        [MaxLength(50)]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Draft"; // Draft, Active, Cancelled, Completed

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Updated At")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation Property for Bookings
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}