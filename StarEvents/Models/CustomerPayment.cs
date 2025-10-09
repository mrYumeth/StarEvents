using StarEvents.Data;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace StarEvents.Models.Payments
{
    /// <summary>
    /// Represents a financial transaction related to a booking.
    /// In a real system, this data would come directly from the Payment Gateway.
    /// </summary>
    public class CustomerPayment // *** FIX: Class name changed to PaymentModel ***
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Amount { get; set; }

        // The unique ID returned by the payment gateway (e.g., Stripe, PayPal)
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

        // For auditing: which customer made the payment
        [Required]
        public string CustomerId { get; set; } = string.Empty;
        public ApplicationUser Customer { get; set; } = default!;

        // Optional: Masked card details for reference
        [StringLength(4)]
        public string? CardLastFour { get; set; }

        // Navigation property to link back to the booking(s)
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();

        // Time Tracking
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // *** FIX: Added missing UpdatedAt property ***
        public DateTime? UpdatedAt { get; set; }
    }
}
