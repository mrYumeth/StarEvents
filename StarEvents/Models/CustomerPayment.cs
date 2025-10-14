using StarEvents.Data;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace StarEvents.Models.Payments
{
    /// Represents a financial transaction related to a booking.

    public class CustomerPayment 
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Amount { get; set; }

        // The unique ID returned by the payment gateway 
        [Required]
        [StringLength(100)]
        public string TransactionId { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string PaymentMethod { get; set; } = string.Empty;

        [Required]
        public DateTime PaymentDate { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = string.Empty;

        // Customer ID saved to identify payment
        [Required]
        public string CustomerId { get; set; } = string.Empty;
        public ApplicationUser Customer { get; set; } = default!;

        // Masked card details for reference
        [StringLength(4)]
        public string? CardLastFour { get; set; }

        // Navigation property to link back to the bookings
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();

        // Time Tracking
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
