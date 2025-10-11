using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarEvents.Data;
using StarEvents.Models;
using StarEvents.ViewModels;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace StarEvents.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CustomerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CustomerController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(Dashboard));
        }

        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) { return Challenge(); }

            var allUserBookings = await _context.Bookings
                .Where(b => b.CustomerId == user.Id)
                .Include(b => b.Event).ThenInclude(e => e.Venue)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            var featuredEvents = await _context.Events
                .Where(e => e.IsActive && e.StartDate > DateTime.Now)
                .Include(e => e.Venue)
                .OrderBy(e => e.StartDate)
                .Take(3)
                .ToListAsync();

            var confirmedBookings = allUserBookings.Where(b => b.Status == "Confirmed" || b.Status == "Completed").ToList();

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

        // --- UPDATED Profile GET Action ---
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            // Calculate stats for the profile sidebar
            var confirmedBookings = await _context.Bookings
                .Where(b => b.CustomerId == user.Id && (b.Status == "Confirmed" || b.Status == "Completed"))
                .Include(b => b.Event)
                .ToListAsync();

            ViewBag.TotalBookings = confirmedBookings.Count;
            ViewBag.UpcomingEvents = confirmedBookings.Count(b => b.Event.StartDate > DateTime.Now);
            ViewBag.TotalSpent = confirmedBookings.Sum(b => b.TotalAmount).ToString("N2");

            return View(user);
        }

        // POST: /Customer/Profile (This action is correct as you wrote it)
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

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.PhoneNumber = model.PhoneNumber;

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

        // --- NEW ChangePassword POST Action ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            if (newPassword != confirmPassword)
            {
                TempData["ErrorMessage"] = "New password and confirmation password do not match.";
                return RedirectToAction(nameof(Profile));
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            var changePasswordResult = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);

            if (changePasswordResult.Succeeded)
            {
                TempData["SuccessMessage"] = "Your password has been changed successfully.";
            }
            else
            {
                var errorDescription = string.Join(", ", changePasswordResult.Errors.Select(e => e.Description));
                TempData["ErrorMessage"] = $"Error changing password: {errorDescription}";
            }

            return RedirectToAction(nameof(Profile));
        }
    }
}