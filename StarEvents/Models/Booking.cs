using StarEvents.Data; // Required for ApplicationUser
using StarEvents.Models.Payments;
using System;
using System.Collections.Generic;
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
        [Display(Name = "Customer")]
        public string CustomerId { get; set; } = string.Empty; // Initialized

        // Navigation Property to Customer
        [ForeignKey("CustomerId")]
        public ApplicationUser Customer { get; set; } = default!; // Initialized

        [Required]
        [Display(Name = "Event")]
        public int EventId { get; set; }

        // Navigation Property to Event
        [ForeignKey("EventId")]
        public Event Event { get; set; } = default!; // Initialized

        // --- START NEW PAYMENT LINK ---
        [Required]
        public int PaymentId { get; set; }

        // Navigation Property to Payment
        // FIX: Changed from ClientPayment to CustomerPayment to match model usage in BookingsController.
        [ForeignKey("PaymentId")]
        public CustomerPayment Payment { get; set; } = default!; // Initialized
        // --- END NEW PAYMENT LINK ---

        [Required]
        [Display(Name = "Ticket Quantity")]
        [Range(1, 100, ErrorMessage = "Ticket quantity must be between 1 and 100")]
        public int TicketQuantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Unit Price")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Discount Amount")]
        public decimal DiscountAmount { get; set; } = 0;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Total Amount")]
        public decimal TotalAmount { get; set; }

        [Required]
        [MaxLength(50)]
        [Display(Name = "Booking Status")]
        public string Status { get; set; } = "Pending"; // Pending, Confirmed, Cancelled, Completed

        [MaxLength(500)]
        [Display(Name = "QR Code")]
        public string? QRCodeUrl { get; set; }

        [Display(Name = "Booking Date")]
        public DateTime BookingDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Cancellation Date")]
        public DateTime? CancellationDate { get; set; }

        [MaxLength(500)]
        [Display(Name = "Cancellation Reason")]
        public string? CancellationReason { get; set; }

        [MaxLength(500)]
        [Display(Name = "Special Requests")]
        public string? SpecialRequests { get; set; }

        [Display(Name = "Points Earned")]
        public int PointsEarned { get; set; } = 0;

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Updated At")]
        public DateTime? UpdatedAt { get; set; }
    }
}
