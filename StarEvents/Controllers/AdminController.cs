using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarEvents.Data;
using StarEvents.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace StarEvents.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // ----------------------------------------------------------------------
        // Dashboard
        // ----------------------------------------------------------------------
        public async Task<IActionResult> Dashboard()
        {
            // Get statistics
            ViewBag.TotalUsers = await _userManager.Users.CountAsync();
            ViewBag.NewUsersThisMonth = await _userManager.Users
                .CountAsync(u => u.EmailConfirmed);

            ViewBag.TotalEvents = await _context.Events.CountAsync();
            ViewBag.ActiveEvents = await _context.Events
                .CountAsync(e => e.IsActive && e.Status == "Active");

            ViewBag.TotalBookings = await _context.Bookings.CountAsync();
            ViewBag.BookingsThisMonth = await _context.Bookings
                .CountAsync(b => b.BookingDate.Month == DateTime.Now.Month);

            var totalRevenue = await _context.Bookings
                .Where(b => b.Status == "Confirmed" || b.Status == "Completed")
                .SumAsync(b => b.TotalAmount);
            ViewBag.TotalRevenue = totalRevenue.ToString("N0");
            ViewBag.RevenueGrowth = "15"; // Placeholder

            ViewBag.PendingApprovals = await _context.Events
                .CountAsync(e => e.Status == "Draft");

            // This action now directly corresponds to the Dashboard.cshtml view file.
            return View();
        }

        // ----------------------------------------------------------------------
        // Manage Users
        // ----------------------------------------------------------------------
        public async Task<IActionResult> ManageUsers()
        {
            var users = await _userManager.Users.ToListAsync();
            var usersWithRoles = new List<object>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                usersWithRoles.Add(new
                {
                    User = user,
                    Roles = roles
                });
            }
            ViewBag.UsersWithRoles = usersWithRoles;
            return View(users);
        }

        // ----------------------------------------------------------------------
        // Manage Events
        // ----------------------------------------------------------------------
        public async Task<IActionResult> ManageEvents(string status = "all")
        {
            var query = _context.Events
                .Include(e => e.Organizer)
                .AsQueryable();

            if (status != "all")
            {
                query = query.Where(e => e.Status.ToLower() == status.ToLower());
            }

            var events = await query
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

            ViewBag.CurrentFilter = status;
            return View(events);
        }


        // ----------------------------------------------------------------------
        // Approve/Reject Event
        // ----------------------------------------------------------------------
        [HttpPost]
        public async Task<IActionResult> ApproveEvent(int id)
        {
            var eventToApprove = await _context.Events.FindAsync(id);
            if (eventToApprove == null)
            {
                return NotFound();
            }

            eventToApprove.Status = "Active";
            eventToApprove.IsActive = true;
            eventToApprove.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Event approved successfully!";
            return RedirectToAction(nameof(ManageEvents));
        }

        [HttpPost]
        public async Task<IActionResult> RejectEvent(int id, string reason)
        {
            var eventToReject = await _context.Events.FindAsync(id);
            if (eventToReject == null)
            {
                return NotFound();
            }

            eventToReject.Status = "Cancelled";
            eventToReject.IsActive = false;
            eventToReject.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Event rejected.";
            return RedirectToAction(nameof(ManageEvents));
        }

        // ----------------------------------------------------------------------
        // Delete Event
        // ----------------------------------------------------------------------
        [HttpPost]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var eventToDelete = await _context.Events.FindAsync(id);
            if (eventToDelete == null)
            {
                return NotFound();
            }

            _context.Events.Remove(eventToDelete);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Event deleted successfully!";
            return RedirectToAction(nameof(ManageEvents));
        }

        // ----------------------------------------------------------------------
        // Manage Venues
        // ----------------------------------------------------------------------
        public async Task<IActionResult> ManageVenues()
        {
            var venues = await _context.Venues.ToListAsync();
            return View(venues);
        }

        // ----------------------------------------------------------------------
        // Create Venue (GET)
        // ----------------------------------------------------------------------
        public IActionResult CreateVenue()
        {
            return View();
        }

        // ----------------------------------------------------------------------
        // Create Venue (POST)
        // ----------------------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateVenue(Venue venue)
        {
            if (ModelState.IsValid)
            {
                _context.Venues.Add(venue);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Venue created successfully!";
                return RedirectToAction(nameof(ManageVenues));
            }

            return View(venue);
        }

        // ----------------------------------------------------------------------
        // Edit Venue (GET)
        // ----------------------------------------------------------------------
        public async Task<IActionResult> EditVenue(int id)
        {
            var venue = await _context.Venues.FindAsync(id);
            if (venue == null)
            {
                return NotFound();
            }

            return View(venue);
        }

        // ----------------------------------------------------------------------
        // Edit Venue (POST)
        // ----------------------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditVenue(int id, Venue venue)
        {
            if (id != venue.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(venue);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Venue updated successfully!";
                    return RedirectToAction(nameof(ManageVenues));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VenueExists(venue.Id))
                    {
                        return NotFound();
                    }
                    throw;
                }
            }

            return View(venue);
        }

        // ----------------------------------------------------------------------
        // Delete Venue
        // ----------------------------------------------------------------------
        [HttpPost]
        public async Task<IActionResult> DeleteVenue(int id)
        {
            var venue = await _context.Venues.FindAsync(id);
            if (venue == null)
            {
                return NotFound();
            }

            _context.Venues.Remove(venue);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Venue deleted successfully!";
            return RedirectToAction(nameof(ManageVenues));
        }

        // ----------------------------------------------------------------------
        // Manage Bookings
        // ----------------------------------------------------------------------
        public async Task<IActionResult> ManageBookings()
        {
            var bookings = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Event)
                // FIX: Removed .ThenInclude(e => e.Venue)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            return View(bookings);
        }

        // ----------------------------------------------------------------------
        // Reports - Generate System Reports
        // ----------------------------------------------------------------------
        public async Task<IActionResult> Reports()
        {
            // Sales Summary
            var totalSales = await _context.Bookings
                .Where(b => b.Status == "Confirmed" || b.Status == "Completed")
                .SumAsync(b => b.TotalAmount);

            var totalTicketsSold = await _context.Bookings
                .Where(b => b.Status == "Confirmed" || b.Status == "Completed")
                .SumAsync(b => b.TicketQuantity);

            ViewBag.TotalSales = totalSales.ToString("N2");
            ViewBag.TotalTicketsSold = totalTicketsSold;
            ViewBag.AverageBookingValue = totalTicketsSold > 0
                ? (totalSales / totalTicketsSold).ToString("N2")
                : "0.00";

            // Top Events
            var topEvents = await _context.Events
                .Include(e => e.Bookings)
                .OrderByDescending(e => e.Bookings.Sum(b => b.TotalAmount))
                .Take(5)
                .Select(e => new
                {
                    EventName = e.Title,
                    TotalRevenue = e.Bookings
                        .Where(b => b.Status == "Confirmed" || b.Status == "Completed")
                        .Sum(b => b.TotalAmount),
                    TicketsSold = e.Bookings
                        .Where(b => b.Status == "Confirmed" || b.Status == "Completed")
                        .Sum(b => b.TicketQuantity)
                })
                .ToListAsync();

            ViewBag.TopEvents = topEvents;

            // Monthly Revenue
            var monthlyRevenue = await _context.Bookings
                .Where(b => b.Status == "Confirmed" || b.Status == "Completed")
                .GroupBy(b => new { b.BookingDate.Year, b.BookingDate.Month })
                .Select(g => new
                {
                    Month = $"{g.Key.Year}-{g.Key.Month:D2}",
                    Revenue = g.Sum(b => b.TotalAmount)
                })
                .OrderBy(x => x.Month)
                .ToListAsync();

            ViewBag.MonthlyRevenue = monthlyRevenue;

            // User Statistics
            ViewBag.TotalCustomers = await _userManager.Users
                .Where(u => u.LoyaltyPoints >= 0) // Customers have loyalty points
                .CountAsync();

            ViewBag.TotalOrganizers = await _context.Events
                .Select(e => e.OrganizerId)
                .Distinct()
                .CountAsync();

            return View();
        }

        // ----------------------------------------------------------------------
        // System Settings
        // ----------------------------------------------------------------------
        public IActionResult SystemSettings()
        {
            return View();
        }

        // ----------------------------------------------------------------------
        // Helper Methods
        // ----------------------------------------------------------------------
        private bool VenueExists(int id)
        {
            return _context.Venues.Any(e => e.Id == id);
        }
    }
}