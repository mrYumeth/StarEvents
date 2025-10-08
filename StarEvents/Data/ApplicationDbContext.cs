using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace StarEvents.Data
{
    // 1) Extend Identity user with profile fields
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int LoyaltyPoints { get; set; }
    }

    // 2) Minimal domain models required for now (expand later)
    public class Venue
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public int Capacity { get; set; }
    }

    public class Event
    {
        public int Id { get; set; }

        // Organizer FK
        [Required]
        public string OrganizerId { get; set; }
        public ApplicationUser Organizer { get; set; }

        // Venue FK
        public int VenueId { get; set; }
        public Venue Venue { get; set; } // Navigation property

        [Required, MaxLength(250)]
        public string Title { get; set; }

        public string Description { get; set; }

        [MaxLength(100)]
        public string Category { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } = "Draft";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property for bookings
        public ICollection<Booking> Bookings { get; set; }
    }

    // --- NEW MODEL FOR BOOKING ---
    public class Booking
    {
        public int Id { get; set; }

        // Foreign Key to the Customer (ApplicationUser)
        [Required]
        public string CustomerId { get; set; }
        public ApplicationUser Customer { get; set; }

        // Foreign Key to the Event
        public int EventId { get; set; }
        public Event Event { get; set; }

        [Required]
        public int TicketQuantity { get; set; }

        [Required]
        public decimal TotalPrice { get; set; }

        [MaxLength(50)]
        public string PaymentStatus { get; set; } = "Pending";

        public DateTime BookingDate { get; set; } = DateTime.UtcNow;
    }


    // 3) Application DbContext using IdentityDbContext
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Venue> Venues { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Booking> Bookings { get; set; } // --- NEW DBSET ---

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Additional constraints/indices can be added here later.
        }
    }

    // 4) Database initializer for seeding roles, users, and core data
    public static class DbInitializer
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // --- 4.1: Seed Roles and Admin User (Existing Logic) ---
            var roles = new[] { "Admin", "Organizer", "Customer" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Create default admin
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

            // --- 4.2: Seed Organizer and Event Data (Existing Logic) ---

            // 1. Create a dedicated Organizer user
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

            // 2. Seed Venue Data
            if (!context.Venues.Any())
            {
                context.Venues.Add(new Venue
                {
                    Name = "Colombo Arena",
                    Address = "123 Galle Road",
                    City = "Colombo",
                    Capacity = 15000
                });
                await context.SaveChangesAsync();
            }

            // 3. Seed Event Data (Requires Venue and Organizer to exist)
            if (organizerUser != null && context.Venues.Any() && !context.Events.Any())
            {
                var venue = context.Venues.First();

                context.Events.Add(new Event
                {
                    OrganizerId = organizerUser.Id, // FK is now a string!
                    VenueId = venue.Id,
                    Title = "Star Events Grand Concert 2026",
                    Description = "The biggest music event of the year, featuring international stars and local talent.",
                    Category = "Music",
                    StartDate = DateTime.UtcNow.AddDays(90).Date.AddHours(19),
                    EndDate = DateTime.UtcNow.AddDays(90).Date.AddHours(22),
                    Status = "Published",
                    CreatedAt = DateTime.UtcNow
                });
                await context.SaveChangesAsync();
            }
        }
    }
}
