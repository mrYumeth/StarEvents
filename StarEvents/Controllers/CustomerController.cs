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
        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge(); // Not found or not logged in
            }

            // --- Data Fetching ---
            var allUserBookings = await _context.Bookings
                .Where(b => b.CustomerId == user.Id)
                .Include(b => b.Event) // Correctly include just the event
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            var featuredEvents = await _context.Events
                .Where(e => e.IsActive && e.StartDate > DateTime.Now)
                .OrderBy(e => e.StartDate)
                .Take(3)
                .ToListAsync();

            // --- Logic ---
            var confirmedBookings = allUserBookings.Where(b => b.Status == "Confirmed" || b.Status == "Completed").ToList();

            // --- Populate the ViewModel ---
            var dashboardViewModel = new CustomerDashboardViewModel
            {
                UserName = user.FirstName,
                TotalBookings = confirmedBookings.Count(),
                UpcomingEventsCount = confirmedBookings.Count(b => b.Event.StartDate > DateTime.Now),
                TotalSpent = confirmedBookings.Sum(b => b.TotalAmount),
                LoyaltyPoints = user.LoyaltyPoints,
                RecentBookings = allUserBookings.Where(b => b.Status == "Confirmed").Take(5).ToList(),
                FeaturedEvents = featuredEvents
            };

            return View(dashboardViewModel);
        }

        // ----------------------------------------------------------------------
        // 2. Profile - Loads the profile editing view.
        // ----------------------------------------------------------------------
        // ----------------------------------------------------------------------
        // GET: /Customer/Profile
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var profileViewModel = new ProfileViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email
            };

            return View(profileViewModel);
        }

        // POST: /Customer/Profile (Final Corrected Version)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // If validation fails, repopulate the email before returning the view
                var currentUserForEmail = await _userManager.GetUserAsync(User);
                if (currentUserForEmail != null)
                {
                    model.Email = currentUserForEmail.Email;
                }
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("Unable to load user.");
            }

            // Update the properties of the user object
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;

            // --- DATABASE SAVE FIX ---
            // Use the UserManager to update the user.
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                // As an extra measure, explicitly save changes on the context.
                // While UpdateAsync often handles this, this ensures it happens.
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Profile updated successfully!";
                return RedirectToAction(nameof(Profile));
            }

            // If the update fails, add errors to the model state
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            // Repopulate the email as it's not part of the form submission
            model.Email = user.Email;

            return View(model);
        }
    }
}