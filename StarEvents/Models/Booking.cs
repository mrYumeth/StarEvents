using Microsoft.AspNetCore.Identity;
using StarEvents.Models.Payments;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarEvents.Models
{
    [Table("Bookings")]
    public class Booking
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime BookingDate { get; set; } = DateTime.UtcNow;

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Pending";

        [Required]
        [Range(1, 100)]
        public int TicketQuantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; } = 0;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Required]
        [Display(Name = "Points Earned")]
        public int PointsEarned { get; set; } = 0;

        [MaxLength(500)]
        public string? QRCodeUrl { get; set; }

        // --- Foreign Keys and Navigation Properties ---

        [Required]
        public string CustomerId { get; set; }
        [ForeignKey("CustomerId")]
        public virtual ApplicationUser Customer { get; set; }

        [Required]
        public int EventId { get; set; }
        [ForeignKey("EventId")]
        public virtual Event Event { get; set; }

        // PaymentId is nullable because a booking might be created before it's paid
        public int? PaymentId { get; set; }
        [ForeignKey("PaymentId")]
        public virtual CustomerPayment? Payment { get; set; }
    }
}