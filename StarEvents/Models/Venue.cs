using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarEvents.Models
{
    [Table("Venues")]
    public class Venue
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        [Display(Name = "Venue Name")]
        public string VenueName { get; set; }

        [Required]
        [MaxLength(500)]
        [Display(Name = "Address")]
        public string Address { get; set; }

        [Required]
        [MaxLength(100)]
        [Display(Name = "City")]
        public string City { get; set; }

        [Display(Name = "Capacity")]
        [Range(1, 100000, ErrorMessage = "Capacity must be between 1 and 100,000")]
        public int Capacity { get; set; }

        [MaxLength(20)]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [MaxLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        // Navigation property
        public ICollection<Event> Events { get; set; }
    }
}