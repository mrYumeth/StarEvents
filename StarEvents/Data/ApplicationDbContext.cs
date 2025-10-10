using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StarEvents.Models;
using StarEvents.Models.Payments;
using System;
using System.Collections.Generic; // Added missing using for ICollection
using System.Linq;
using System.Threading.Tasks;

namespace StarEvents.Data
{
    

    // 2) Application DbContext using IdentityDbContext
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Venue> Venues { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        // DbSet is named 'Payments' and uses the correct model 'CustomerPayment'
        public DbSet<CustomerPayment> Payments { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure relationships
            builder.Entity<Event>()
                .HasOne(e => e.Organizer)
                .WithMany(u => u.OrganizedEvents)
                .HasForeignKey(e => e.OrganizerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Event>()
                .HasOne(e => e.Venue)
                .WithMany(v => v.Events)
                .HasForeignKey(e => e.VenueId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Booking>()
                .HasOne(b => b.Customer)
                .WithMany(u => u.Bookings)
                .HasForeignKey(b => b.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Booking>()
                .HasOne(b => b.Event)
                .WithMany(e => e.Bookings)
                .HasForeignKey(b => b.EventId)
                .OnDelete(DeleteBehavior.Restrict);

            // NEW: Payment to ApplicationUser (Customer)
            // FIXED: Using CustomerPayment model name
            builder.Entity<CustomerPayment>()
                .HasOne(p => p.Customer)
                .WithMany(u => u.Payments)
                .HasForeignKey(p => p.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // NEW: Booking to Payment
            // Note: The Payment model has been inferred to be CustomerPayment for consistency.
            builder.Entity<Booking>()
                .HasOne(b => b.Payment)
                .WithMany(p => p.Bookings)
                .HasForeignKey(b => b.PaymentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure decimal precision
            builder.Entity<Event>()
                .Property(e => e.TicketPrice)
                .HasColumnType("decimal(18,2)");

            builder.Entity<Booking>()
                .Property(b => b.UnitPrice)
                .HasColumnType("decimal(18,2)");

            builder.Entity<Booking>()
                .Property(b => b.DiscountAmount)
                .HasColumnType("decimal(18,2)");

            builder.Entity<Booking>()
                .Property(b => b.TotalAmount)
                .HasColumnType("decimal(18,2)");
        }
    }

    // 3) Database initializer for seeding roles, users, and core data
    public static class DbInitializer
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // --- Seed Roles ---
            var roles = new[] { "Admin", "Organizer", "Customer" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // --- Seed Admin User ---
            var adminEmail = "admin@starevents.com";
            var adminPassword = "Admin@12345";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "System",
                    LastName = "Admin",
                    LoyaltyPoints = 0,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // --- Seed Organizer User ---
            var organizerEmail = "organizer@starevents.com";
            var organizerUser = await userManager.FindByEmailAsync(organizerEmail);
            if (organizerUser == null)
            {
                organizerUser = new ApplicationUser
                {
                    UserName = organizerEmail,
                    Email = organizerEmail,
                    FirstName = "Live",
                    LastName = "Nation",
                    LoyaltyPoints = 0,
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(organizerUser, "Pass123!");
                await userManager.AddToRoleAsync(organizerUser, "Organizer");
            }

            // --- Seed Customer User ---
            var customerEmail = "customer@starevents.com";
            var customerUser = await userManager.FindByEmailAsync(customerEmail);
            if (customerUser == null)
            {
                customerUser = new ApplicationUser
                {
                    UserName = customerEmail,
                    Email = customerEmail,
                    FirstName = "John",
                    LastName = "Doe",
                    LoyaltyPoints = 50,
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(customerUser, "Pass123!");
                await userManager.AddToRoleAsync(customerUser, "Customer");
            }

            // --- Seed Venues ---
            if (!context.Venues.Any())
            {
                context.Venues.AddRange(
                    new Venue
                    {
                        VenueName = "Colombo Arena",
                        Address = "123 Galle Road",
                        City = "Colombo",
                        Capacity = 15000,
                        IsActive = true
                    },
                    new Venue
                    {
                        VenueName = "Kandy City Hall",
                        Address = "456 Temple Street",
                        City = "Kandy",
                        Capacity = 5000,
                        IsActive = true
                    }
                );
                await context.SaveChangesAsync();
            }

            // --- Seed Events ---
            if (organizerUser != null && context.Venues.Any() && !context.Events.Any())
            {
                var venue = context.Venues.First();

                context.Events.AddRange(
                    new Event
                    {
                        OrganizerId = organizerUser.Id,
                        VenueId = venue.Id,
                        Title = "Star Events Grand Concert 2026",
                        Description = "The biggest music event of the year, featuring international stars and local talent.",
                        Category = "Music",
                        StartDate = DateTime.UtcNow.AddDays(90).Date.AddHours(19),
                        EndDate = DateTime.UtcNow.AddDays(90).Date.AddHours(22),
                        Status = "Active",
                        IsActive = true,
                        TicketPrice = 5000.00m,
                        AvailableTickets = 10000,
                        ImageUrl = "/images/concert.jpg",
                        CreatedAt = DateTime.UtcNow
                    },
                    new Event
                    {
                        OrganizerId = organizerUser.Id,
                        VenueId = venue.Id,
                        Title = "Comedy Night Live",
                        Description = "An evening of laughter with top comedians from around the world.",
                        Category = "Comedy",
                        StartDate = DateTime.UtcNow.AddDays(30).Date.AddHours(20),
                        EndDate = DateTime.UtcNow.AddDays(30).Date.AddHours(23),
                        Status = "Active",
                        IsActive = true,
                        TicketPrice = 2500.00m,
                        AvailableTickets = 3000,
                        ImageUrl = "/images/comedy.jpg",
                        CreatedAt = DateTime.UtcNow
                    }
                );
                await context.SaveChangesAsync();
            }

            // --- Seed Sample Booking ---
            if (customerUser != null && context.Events.Any() && !context.Bookings.Any())
            {
                var firstEvent = context.Events.First();
                var totalAmount = firstEvent.TicketPrice * 2;

                // 1. Create Payment Record (required before Booking can be created)
                // FIXED: Using correct CustomerPayment model name
                var samplePayment = new CustomerPayment
                {
                    Amount = totalAmount,
                    PaymentMethod = "CreditCard",
                    TransactionId = "TXN-" + Guid.NewGuid().ToString().Substring(0, 8),
                    Status = "Confirmed",
                    PaymentDate = DateTime.UtcNow.AddDays(-5),
                    CustomerId = customerUser.Id,
                    CardLastFour = "1234" // Dummy data
                };
                context.Payments.Add(samplePayment); // Correct DbSet name
                await context.SaveChangesAsync();

                // 2. Create Booking Record, linking to the new Payment
                context.Bookings.Add(new Booking
                {
                    CustomerId = customerUser.Id,
                    EventId = firstEvent.Id,
                    TicketQuantity = 2,
                    UnitPrice = firstEvent.TicketPrice,
                    DiscountAmount = 0,
                    TotalAmount = totalAmount,
                    Status = "Confirmed",
                    PaymentId = samplePayment.Id, // Linked Payment ID
                    BookingDate = DateTime.UtcNow.AddDays(-5),
                    // Removed PaymentDate, PaymentMethod, TransactionId
                    PointsEarned = 100,
                    CreatedAt = DateTime.UtcNow.AddDays(-5)
                });
                await context.SaveChangesAsync();
            }
        }
    }
}
