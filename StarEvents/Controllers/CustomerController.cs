using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarEvents.Data;
using StarEvents.Models;
using StarEvents.ViewModels;
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
        // In Controllers/CustomerController.cs

        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge(); // Not found or not logged in
            }

            // --- Data Fetching ---
            // Get all of the user's relevant bookings in one query
            var allUserBookings = await _context.Bookings
                .Where(b => b.CustomerId == user.Id)
                .Include(b => b.Event)
                    .ThenInclude(e => e.Venue)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            // Get the top 3 upcoming events for the "Featured" section
            var featuredEvents = await _context.Events
                .Where(e => e.IsActive && e.StartDate > DateTime.Now)
                .Include(e => e.Venue)
                .OrderBy(e => e.StartDate)
                .Take(3)
                .ToListAsync();

            // --- Logic Fixes ---
            var confirmedBookings = allUserBookings.Where(b => b.Status == "Confirmed" || b.Status == "Completed").ToList();

            // --- Populate the ViewModel ---
            var dashboardViewModel = new CustomerDashboardViewModel
            {
                // Use the user's FirstName for a personal touch
                UserName = user.FirstName,

                // Calculate stats based on confirmed/completed bookings
                TotalBookings = confirmedBookings.Count(),
                UpcomingEventsCount = confirmedBookings.Count(b => b.Event.StartDate > DateTime.Now),
                TotalSpent = confirmedBookings.Sum(b => b.TotalAmount),

                // FIX: Get current loyalty points directly from the user object
                LoyaltyPoints = user.LoyaltyPoints,

                // FIX: Get the 5 most recent *confirmed* bookings for the list
                RecentBookings = allUserBookings.Where(b => b.Status == "Confirmed").Take(5).ToList(),

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

            return View(dashboardViewModel);
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