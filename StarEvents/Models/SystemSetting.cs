using System.ComponentModel.DataAnnotations;
using StarEvents.Data;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace StarEvents.Models
{
    public class SystemSetting
    {
        [Key]
        public int Id { get; set; } // Primary Key

        // General Settings
        public string SystemName { get; set; }
        public string ContactEmail { get; set; }
        public string SupportPhone { get; set; }

        // Booking Settings
        public int MaxTicketsPerBooking { get; set; }
        public int BookingCancellationHours { get; set; }
        public bool EnableQRCodeTickets { get; set; }

        // Payment Settings
        public bool AcceptCreditCards { get; set; }
        public bool AcceptPayPal { get; set; }
        public string Currency { get; set; }

        // Loyalty Program
        public bool EnableLoyaltyProgram { get; set; }
        public int PointsPer100LKR { get; set; }
        public int PointsExpiryDays { get; set; }

        // Email Notifications
        public bool EmailOnBookingConfirmation { get; set; }
        public bool EmailOnEventReminder { get; set; }
        public bool EmailForPromotions { get; set; }

        // Security Settings
        public bool RequireEmailVerification { get; set; }
        public int SessionTimeoutMinutes { get; set; }
        public int PasswordMinLength { get; set; }
    }
}