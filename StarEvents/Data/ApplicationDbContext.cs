using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StarEvents.Models;
using StarEvents.Models.Payments;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace StarEvents.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Venue> Venues { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Booking> Bookings { get; set; }
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

            builder.Entity<CustomerPayment>()
                .HasOne(p => p.Customer)
                .WithMany(u => u.Payments)
                .HasForeignKey(p => p.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- FIX: Corrected relationship from one-to-many to one-to-one ---
            builder.Entity<Booking>()
                .HasOne(b => b.Payment)
                .WithOne() // A booking has one payment.
                .HasForeignKey<Booking>(b => b.PaymentId)
                .IsRequired(false) // PaymentId is nullable
                .OnDelete(DeleteBehavior.Restrict);

            // Configure decimal precision
            builder.Entity<Event>().Property(e => e.TicketPrice).HasColumnType("decimal(18,2)");
            builder.Entity<Booking>().Property(b => b.UnitPrice).HasColumnType("decimal(18,2)");
            builder.Entity<Booking>().Property(b => b.DiscountAmount).HasColumnType("decimal(18,2)");
            builder.Entity<Booking>().Property(b => b.TotalAmount).HasColumnType("decimal(18,2)");
        }
    }

    public static class DbInitializer
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var roles = new[] { "Admin", "Organizer", "Customer" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            var adminUser = await userManager.FindByEmailAsync("admin@starevents.com");
            if (adminUser == null)
            {
                adminUser = new ApplicationUser { UserName = "admin@starevents.com", Email = "admin@starevents.com", FirstName = "System", LastName = "Admin", EmailConfirmed = true };
                await userManager.CreateAsync(adminUser, "Admin@12345");
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }

            var organizerUser = await userManager.FindByEmailAsync("organizer@starevents.com");
            if (organizerUser == null)
            {
                organizerUser = new ApplicationUser { UserName = "organizer@starevents.com", Email = "organizer@starevents.com", FirstName = "Live", LastName = "Nation", EmailConfirmed = true };
                await userManager.CreateAsync(organizerUser, "Pass123!");
                await userManager.AddToRoleAsync(organizerUser, "Organizer");
            }

            var customerUser = await userManager.FindByEmailAsync("test@gmail.com");
            if (customerUser == null)
            {
                customerUser = new ApplicationUser { UserName = "test@gmail.com", Email = "test@gmail.com", FirstName = "John", LastName = "Doe", LoyaltyPoints = 50, EmailConfirmed = true };
                await userManager.CreateAsync(customerUser, "Test@123#");
                await userManager.AddToRoleAsync(customerUser, "Customer");
            }

            if (!context.Venues.Any())
            {
                context.Venues.AddRange(
                    new Venue { VenueName = "Colombo Arena", Address = "123 Galle Road", City = "Colombo", Capacity = 15000, IsActive = true },
                    new Venue { VenueName = "Kandy City Hall", Address = "456 Temple Street", City = "Kandy", Capacity = 5000, IsActive = true }
                );
                await context.SaveChangesAsync();
            }

            if (!context.Events.Any())
            {
                var venue = context.Venues.First();
                context.Events.AddRange(
                    new Event { OrganizerId = organizerUser.Id, VenueId = venue.Id, Title = "Star Events Grand Concert 2026", Description = "The biggest music event of the year.", Category = "Music", StartDate = DateTime.UtcNow.AddDays(90), Status = "Active", IsActive = true, TicketPrice = 5000.00m, AvailableTickets = 10000, ImageUrl = "/images/concert.jpg", CreatedAt = DateTime.UtcNow },
                    new Event { OrganizerId = organizerUser.Id, VenueId = venue.Id, Title = "Comedy Night Live", Description = "An evening of laughter.", Category = "Comedy", StartDate = DateTime.UtcNow.AddDays(30), Status = "Active", IsActive = true, TicketPrice = 2500.00m, AvailableTickets = 3000, ImageUrl = "/images/comedy.jpg", CreatedAt = DateTime.UtcNow }
                );
                await context.SaveChangesAsync();
            }

            if (!context.Bookings.Any())
            {
                var firstEvent = context.Events.First();
                var totalAmount = firstEvent.TicketPrice * 2;

                var samplePayment = new CustomerPayment
                {
                    Amount = totalAmount,
                    PaymentMethod = "CreditCard",
                    TransactionId = "TXN-DEMO123",
                    Status = "Confirmed",
                    PaymentDate = DateTime.UtcNow.AddDays(-5),
                    CustomerId = customerUser.Id,
                    CardLastFour = "1234"
                };
                context.Payments.Add(samplePayment);
                await context.SaveChangesAsync();

                context.Bookings.Add(new Booking
                {
                    CustomerId = customerUser.Id,
                    EventId = firstEvent.Id,
                    TicketQuantity = 2,
                    UnitPrice = firstEvent.TicketPrice,
                    DiscountAmount = 0,
                    TotalAmount = totalAmount,
                    Status = "Confirmed",
                    PaymentId = samplePayment.Id,
                    BookingDate = DateTime.UtcNow.AddDays(-5),
                    PointsEarned = 100,
                });
                await context.SaveChangesAsync();
            }
        }
    }
}