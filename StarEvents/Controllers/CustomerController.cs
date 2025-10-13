using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarEvents.Data;
using StarEvents.Models;
using StarEvents.ViewModels; // Make sure you have this using statement
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

        // GET: /Customer or /Customer/Index
        public IActionResult Index()
        {
            // Redirect to the main Dashboard page
            return RedirectToAction(nameof(Dashboard));
        }

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
                .Include(b => b.Event)
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

        // GET: /Customer/Profile (UPDATED)
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            // Calculate stats for the sidebar
            ViewBag.TotalBookings = await _context.Bookings
                .CountAsync(b => b.CustomerId == user.Id);

            ViewBag.UpcomingEvents = await _context.Bookings
                .CountAsync(b => b.CustomerId == user.Id && b.Event.StartDate > DateTime.Now);

            ViewBag.TotalSpent = await _context.Bookings
                .Where(b => b.CustomerId == user.Id && (b.Status == "Confirmed" || b.Status == "Completed"))
                .SumAsync(b => b.TotalAmount);

            // Populate the view model with ALL required user data
            var profileViewModel = new ProfileViewModel
            {
                // Form fields
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,

                // Display fields
                Email = user.Email,
                LoyaltyPoints = user.LoyaltyPoints, // ADDED
                CreatedAt = user.CreatedAt // ADDED
            };

            return View(profileViewModel);
        }

        // POST: /Customer/Profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileUpdateModel model) // <-- USES THE NEW MODEL
        {
            // This check will now pass because Email is not part of the model.
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please ensure all required fields are filled correctly.";
                // If validation fails, redirect back to the GET action to reload all data.
                return RedirectToAction(nameof(Profile));
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("Unable to load user.");
            }

            // Update the user object with the values from the new model.
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.PhoneNumber = model.PhoneNumber;

            // Use the UserManager to persist these changes to the database.
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Your profile has been updated successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to update profile. Please try again.";
            }

            return RedirectToAction(nameof(Profile));
        }

        // --- NEW: Added the missing ChangePassword action ---
        // POST: /Customer/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
            {
                TempData["ErrorMessage"] = "All password fields are required.";
                return RedirectToAction(nameof(Profile));
            }

            if (newPassword != confirmPassword)
            {
                TempData["ErrorMessage"] = "New password and confirmation password do not match.";
                return RedirectToAction(nameof(Profile));
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("Unable to load user.");
            }

            var changePasswordResult = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);

            if (!changePasswordResult.Succeeded)
            {
                TempData["ErrorMessage"] = "Error: Could not change password. Please check your current password.";
                return RedirectToAction(nameof(Profile));
            }

            TempData["SuccessMessage"] = "Your password has been changed successfully.";
            return RedirectToAction(nameof(Profile));
        }
    }
}