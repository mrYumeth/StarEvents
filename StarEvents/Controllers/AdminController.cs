using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarEvents.Data;
using StarEvents.Models;
using StarEvents.ViewModels;
using System.Text.Json;

namespace StarEvents.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // Admin Dashboard - Main landing page
        public async Task<IActionResult> Index()
        {
            var dashboardViewModel = new AdminDashboardViewModel
            {
                TotalUsers = await _context.Users.CountAsync(),
                TotalEvents = await _context.Events.CountAsync(),
                TotalVenues = await _context.Venues.CountAsync(),
                TotalBookings = await _context.Bookings.CountAsync(),
                RecentEvents = await _context.Events
                    .Include(e => e.Organizer)
                    .Include(e => e.Venue)
                    .OrderByDescending(e => e.CreatedAt)
                    .Take(5)
                    .ToListAsync(),
                RecentUsers = await _context.Users
                    .OrderByDescending(u => u.Id)
                    .Take(5)
                    .ToListAsync()
            };

            return View(dashboardViewModel);
        }

        #region Event Management

        public async Task<IActionResult> Events()
        {
            var events = await _context.Events
                .Include(e => e.Organizer)
                .Include(e => e.Venue)
                .Include(e => e.Bookings)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

            return View(events);
        }

        [HttpGet]
        public async Task<IActionResult> CreateEvent()
        {
            var viewModel = new AdminEventViewModel
            {
                Venues = await _context.Venues.ToListAsync(),
                Organizers = (await _userManager.GetUsersInRoleAsync("Organizer")).ToList()
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEvent(AdminEventViewModel model)
        {
            if (ModelState.IsValid)
            {
                var eventEntity = new StarEvents.Models.Event
                {
                    Title = model.Title,
                    Description = model.Description,
                    Category = model.Category,
                    StartDate = model.StartDate,
                    EndDate = model.EndDate,
                    Status = model.Status,
                    VenueId = model.VenueId,
                    OrganizerId = model.OrganizerId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Events.Add(eventEntity);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Event created successfully!";
                return RedirectToAction(nameof(Events));
            }

            model.Venues = await _context.Venues.ToListAsync();
            model.Organizers = (await _userManager.GetUsersInRoleAsync("Organizer")).ToList();
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EditEvent(int id)
        {
            var eventEntity = await _context.Events.FindAsync(id);
            if (eventEntity == null)
            {
                return NotFound();
            }

            var viewModel = new AdminEventViewModel
            {
                Id = eventEntity.Id,
                Title = eventEntity.Title,
                Description = eventEntity.Description,
                Category = eventEntity.Category,
                StartDate = eventEntity.StartDate,
                EndDate = eventEntity.EndDate,
                Status = eventEntity.Status,
                VenueId = eventEntity.VenueId,
                OrganizerId = eventEntity.OrganizerId,
                Venues = await _context.Venues.ToListAsync(),
                Organizers = (await _userManager.GetUsersInRoleAsync("Organizer")).ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEvent(AdminEventViewModel model)
        {
            if (ModelState.IsValid)
            {
                var eventEntity = await _context.Events.FindAsync(model.Id);
                if (eventEntity == null)
                {
                    return NotFound();
                }

                eventEntity.Title = model.Title;
                eventEntity.Description = model.Description;
                eventEntity.Category = model.Category;
                eventEntity.StartDate = model.StartDate;
                eventEntity.EndDate = model.EndDate;
                eventEntity.Status = model.Status;
                eventEntity.VenueId = model.VenueId;
                eventEntity.OrganizerId = model.OrganizerId;

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Event updated successfully!";
                return RedirectToAction(nameof(Events));
            }

            model.Venues = await _context.Venues.ToListAsync();
            model.Organizers = (await _userManager.GetUsersInRoleAsync("Organizer")).ToList();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var eventEntity = await _context.Events.FindAsync(id);
            if (eventEntity != null)
            {
                _context.Events.Remove(eventEntity);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Event deleted successfully!";
            }
            return RedirectToAction(nameof(Events));
        }

        #endregion

        #region Venue Management

        public async Task<IActionResult> Venues()
        {
            var venues = await _context.Venues
                .OrderBy(v => v.VenueName)
                .ToListAsync();

            // Get event counts for each venue
            foreach (var venue in venues)
            {
                venue.EventCount = await _context.Events.CountAsync(e => e.VenueId == venue.Id);
            }

            return View(venues);
        }

        [HttpGet]
        public IActionResult CreateVenue()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateVenue(Venue model)
        {
            if (ModelState.IsValid)
            {
                _context.Venues.Add(model);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Venue created successfully!";
                return RedirectToAction(nameof(Venues));
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EditVenue(int id)
        {
            var venue = await _context.Venues.FindAsync(id);
            if (venue == null)
            {
                return NotFound();
            }

            return View(venue);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditVenue(Venue model)
        {
            if (ModelState.IsValid)
            {
                _context.Venues.Update(model);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Venue updated successfully!";
                return RedirectToAction(nameof(Venues));
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVenue(int id)
        {
            var venue = await _context.Venues.FindAsync(id);
            if (venue != null)
            {
                _context.Venues.Remove(venue);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Venue deleted successfully!";
            }
            return RedirectToAction(nameof(Venues));
        }

        #endregion

        #region User Management

        public async Task<IActionResult> Users()
        {
            var users = await _context.Users
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ToListAsync();

            var userViewModels = new List<AdminUserViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userViewModels.Add(new AdminUserViewModel
                {
                    Id = user.Id,
                    FirstName = user.FirstName ?? "",
                    LastName = user.LastName ?? "",
                    Email = user.Email ?? "",
                    PhoneNumber = user.PhoneNumber,
                    LoyaltyPoints = user.LoyaltyPoints,
                    EmailConfirmed = user.EmailConfirmed,
                    Roles = roles.ToList(),
                    CreatedAt = DateTime.UtcNow // Use current time as fallback
                });
            }

            return View(userViewModels);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserRole(string userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var isInRole = await _userManager.IsInRoleAsync(user, role);
            if (isInRole)
            {
                await _userManager.RemoveFromRoleAsync(user, role);
                TempData["SuccessMessage"] = $"User removed from {role} role.";
            }
            else
            {
                await _userManager.AddToRoleAsync(user, role);
                TempData["SuccessMessage"] = $"User added to {role} role.";
            }

            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = "User deleted successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to delete user.";
                }
            }
            return RedirectToAction(nameof(Users));
        }

        #endregion

        #region Reports

        public async Task<IActionResult> Reports()
        {
            var reportViewModel = new AdminReportsViewModel
            {
                TotalRevenue = await _context.Bookings.SumAsync(b => b.TotalAmount),
                TotalTicketsSold = await _context.Bookings.SumAsync(b => b.TicketQuantity),
                EventsByCategory = await _context.Events
                    .GroupBy(e => e.Category)
                    .Select(g => new CategoryStats { Category = g.Key ?? "Uncategorized", Count = g.Count() })
                    .ToListAsync(),
                RevenueByMonth = await _context.Bookings
                    .Where(b => b.BookingDate >= DateTime.UtcNow.AddMonths(-12))
                    .GroupBy(b => new { b.BookingDate.Year, b.BookingDate.Month })
                    .Select(g => new MonthlyRevenue 
                    { 
                        Year = g.Key.Year, 
                        Month = g.Key.Month, 
                        Revenue = g.Sum(b => b.TotalAmount),
                        TicketsSold = g.Sum(b => b.TicketQuantity)
                    })
                    .OrderBy(r => r.Year)
                    .ThenBy(r => r.Month)
                    .ToListAsync(),
                TopEvents = await _context.Events
                    .Include(e => e.Bookings)
                    .Select(e => new TopEventStats
                    {
                        EventTitle = e.Title,
                        TicketsSold = e.Bookings.Sum(b => b.TicketQuantity),
                        Revenue = e.Bookings.Sum(b => b.TotalAmount)
                    })
                    .OrderByDescending(e => e.Revenue)
                    .Take(10)
                    .ToListAsync()
            };

            return View(reportViewModel);
        }

        [HttpGet]
        public async Task<IActionResult> ExportReport(string reportType)
        {
            // Simple CSV export implementation
            var csvContent = reportType switch
            {
                "users" => await GenerateUsersCsv(),
                "events" => await GenerateEventsCsv(),
                "bookings" => await GenerateBookingsCsv(),
                _ => ""
            };

            if (string.IsNullOrEmpty(csvContent))
            {
                return NotFound();
            }

            var fileName = $"{reportType}_report_{DateTime.UtcNow:yyyyMMdd}.csv";
            return File(System.Text.Encoding.UTF8.GetBytes(csvContent), "text/csv", fileName);
        }

        #endregion

        #region Helper Methods

        private async Task<string> GenerateUsersCsv()
        {
            var users = await _context.Users.ToListAsync();
            var csv = "Id,FirstName,LastName,Email,PhoneNumber,LoyaltyPoints,EmailConfirmed\n";
            
            foreach (var user in users)
            {
                csv += $"{user.Id},{user.FirstName},{user.LastName},{user.Email},{user.PhoneNumber},{user.LoyaltyPoints},{user.EmailConfirmed}\n";
            }
            
            return csv;
        }

        private async Task<string> GenerateEventsCsv()
        {
            var events = await _context.Events
                .Include(e => e.Organizer)
                .Include(e => e.Venue)
                .ToListAsync();
            
            var csv = "Id,Title,Category,StartDate,Status,Organizer,Venue\n";
            
            foreach (var eventEntity in events)
            {
                csv += $"{eventEntity.Id},{eventEntity.Title},{eventEntity.Category},{eventEntity.StartDate:yyyy-MM-dd},{eventEntity.Status},{eventEntity.Organizer?.Email},{eventEntity.Venue?.VenueName}\n";
            }
            
            return csv;
        }

        private async Task<string> GenerateBookingsCsv()
        {
            var bookings = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Event)
                .ToListAsync();
            
            var csv = "Id,Customer,Event,Quantity,TotalAmount,Status,BookingDate\n";
            
            foreach (var booking in bookings)
            {
                csv += $"{booking.Id},{booking.Customer?.Email},{booking.Event?.Title},{booking.TicketQuantity},{booking.TotalAmount},{booking.Status},{booking.BookingDate:yyyy-MM-dd}\n";
            }
            
            return csv;
        }

        #endregion

        #region API Endpoints

        [HttpGet]
        public async Task<IActionResult> GetDashboardStats()
        {
            var stats = new
            {
                totalUsers = await _context.Users.CountAsync(),
                totalEvents = await _context.Events.CountAsync(),
                totalVenues = await _context.Venues.CountAsync(),
                totalBookings = await _context.Bookings.CountAsync()
            };

            return Json(stats);
        }

        #endregion
    }
}