using System;
using System.Collections.Generic; // Add this using statement
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;
using StarEvents.Data;

namespace StarEvents.Models
{
    [Table("Events")]
    public class Event
    {
        // FIX: Add a constructor to initialize the Bookings collection
        public Event()
        {
            Bookings = new HashSet<Booking>();
        }

        [Key]
        public int Id { get; set; }

        [Required]
        public string OrganizerId { get; set; }

        [ForeignKey("OrganizerId")]
        public ApplicationUser Organizer { get; set; }

        [Required]
        [Display(Name = "Venue")]
        public string VenueName { get; set; }

        [Required]
        public string Location { get; set; }

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

        [NotMapped]
        [Display(Name = "Event Image")]
        public IFormFile? ImageFile { get; set; }

        [MaxLength(50)]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Draft";

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Updated At")]
        public DateTime? UpdatedAt { get; set; }

        public ICollection<Booking> Bookings { get; set; }
    }
}

