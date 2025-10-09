using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StarEvents.Data;
using StarEvents.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace StarEvents.Controllers
{
    [Authorize(Roles = "Customer")] // Ensure only logged-in customers can access
    public class CustomerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CustomerController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ----------------------------------------------------------------------
        // 0. Index - Redirects to the main Dashboard page.
        // ----------------------------------------------------------------------
        // GET: /Customer/Index (or just /Customer)
        public IActionResult Index()
        {
            // Redirect to the new Dashboard action
            return RedirectToAction(nameof(Dashboard));
        }

        // ----------------------------------------------------------------------
        // 1. Dashboard - The main customer landing page after login.
        // ----------------------------------------------------------------------
        // GET: /Customer/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            // Get the current logged-in user
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            // Get customer's bookings
            var bookings = await _context.Bookings
                .Include(b => b.Event)
                    .ThenInclude(e => e.Venue)
                .Where(b => b.CustomerId == user.Id)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            // Calculate statistics
            ViewBag.TotalBookings = bookings.Count;
            ViewBag.UpcomingEvents = bookings.Count(b => b.Event.StartDate > DateTime.Now);

            // Get loyalty points (assuming ApplicationUser has LoyaltyPoints property)
            // If not, you can calculate it from bookings: bookings.Sum(b => b.PointsEarned)
            ViewBag.LoyaltyPoints = bookings.Sum(b => b.PointsEarned);

            // Calculate total spent
            ViewBag.TotalSpent = bookings
                .Where(b => b.Status == "Confirmed" || b.Status == "Completed")
                .Sum(b => b.TotalAmount)
                .ToString("N2");

            // Get recent bookings (last 5)
            var recentBookings = bookings.Take(5).Select(b => new
            {
                BookingId = b.Id,
                EventName = b.Event.Title,
                EventDate = b.Event.StartDate,
                TicketQuantity = b.TicketQuantity,
                TotalAmount = b.TotalAmount.ToString("N2"),
                Status = b.Status
            }).ToList();

            ViewBag.RecentBookings = recentBookings;

            // Get featured upcoming events (top 3 events sorted by date)
            var upcomingEvents = await _context.Events
                .Include(e => e.Venue)
                .Where(e => e.StartDate > DateTime.Now && e.IsActive)
                .OrderBy(e => e.StartDate)
                .Take(3)
                .Select(e => new
                {
                    EventId = e.Id,
                    EventName = e.Title,
                    EventDate = e.StartDate,
                    Location = e.Venue != null ? e.Venue.VenueName : "TBA",
                    Price = e.TicketPrice.ToString("N2"),
                    ImageUrl = string.IsNullOrEmpty(e.ImageUrl) ? "/images/default-event.jpg" : e.ImageUrl
                })
                .ToListAsync();

            // Pass the upcoming events to the view
            return View(upcomingEvents);
        }

        // ----------------------------------------------------------------------
        // 2. Profile - Placeholder for viewing/editing customer profile.
        // ----------------------------------------------------------------------
        // GET: /Customer/Profile
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            // Profile viewing/editing logic will go here.
            return View(user);
        }

        // ----------------------------------------------------------------------
        // 3. Update Profile - Handle profile updates
        // ----------------------------------------------------------------------
        // POST: /Customer/Profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ApplicationUser model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            // Update only allowed fields
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.PhoneNumber = model.PhoneNumber;
            // Add other fields as needed

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Profile updated successfully!";
                return RedirectToAction(nameof(Profile));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }
    }
}