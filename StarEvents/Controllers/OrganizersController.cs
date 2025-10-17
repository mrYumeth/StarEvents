using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity; 
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarEvents.Data;
using StarEvents.Models;
using StarEvents.ViewModels; 
using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace StarEvents.Controllers
// Manages all functionalities for event organizers.
{
    [Authorize(Roles = "Organizer")]
    public class OrganizersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly UserManager<ApplicationUser> _userManager;

        //The database context for data operations.
        public OrganizersController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _userManager = userManager; 
        }
        // Displays the main dashboard for the organizer.
        public async Task<IActionResult> Index()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var events = await _context.Events
                .Include(e => e.Bookings)
                .Where(e => e.OrganizerId == userIdString)
                .ToListAsync();

            ViewBag.TotalEvents = events.Count;
            ViewBag.TotalTicketsSold = events
                .SelectMany(e => e.Bookings)
                .Where(b => b.Status == "Confirmed" || b.Status == "Completed")
                .Sum(b => b.TicketQuantity);
            ViewBag.TotalRevenue = events
                .SelectMany(e => e.Bookings)
                .Where(b => b.Status == "Confirmed" || b.Status == "Completed")
                .Sum(b => b.TotalAmount);

            var user = await _userManager.GetUserAsync(User);
            ViewBag.OrganizerName = user?.FirstName ?? "Organizer";

            return View();
        }
        // Displays the form for creating a new event.
        [HttpGet]
        public IActionResult CreateEvent()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEvent(Event model)
        {
            ModelState.Remove("OrganizerId");
            ModelState.Remove("Organizer");
            ModelState.Remove("Bookings");

            if (ModelState.IsValid)
            {
                if (model.ImageFile != null)
                {
                    string wwwRootPath = _webHostEnvironment.WebRootPath;
                    string fileName = Path.GetFileNameWithoutExtension(model.ImageFile.FileName);
                    string extension = Path.GetExtension(model.ImageFile.FileName);
                    fileName = fileName + DateTime.Now.ToString("yymmssfff") + extension;
                    model.ImageUrl = "/images/" + fileName;
                    string path = Path.Combine(wwwRootPath, "images", fileName);
                    using (var fileStream = new FileStream(path, FileMode.Create))
                    {
                        await model.ImageFile.CopyToAsync(fileStream);
                    }
                }

                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                model.OrganizerId = currentUserId;
                model.CreatedAt = DateTime.UtcNow;

                if (string.IsNullOrEmpty(model.Status))
                {
                    model.Status = "Draft";
                }

                _context.Events.Add(model);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Event created successfully!";
                return RedirectToAction(nameof(MyEvents));
            }
            return View(model);
        }

        // Displays a list of all events created by the currently logged-in organizer.
        public async Task<IActionResult> MyEvents()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var events = await _context.Events
                .Where(e => e.OrganizerId == userIdString)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
            return View(events);
        }

        // Displays the form for editing an existing event.
        [HttpGet]
        public async Task<IActionResult> EditEvent(int id)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var eventToEdit = await _context.Events
                .FirstOrDefaultAsync(e => e.Id == id && e.OrganizerId == userIdString);
            if (eventToEdit == null)
            {
                return NotFound();
            }
            return View(eventToEdit);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEvent(int id, Event model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            ModelState.Remove("OrganizerId");
            ModelState.Remove("Organizer");
            ModelState.Remove("Bookings");

            if (ModelState.IsValid)
            {
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var existingEvent = await _context.Events.AsNoTracking()
                    .FirstOrDefaultAsync(e => e.Id == id && e.OrganizerId == userIdString);

                if (existingEvent == null)
                {
                    return NotFound();
                }

                if (model.ImageFile != null)
                {
                    if (!string.IsNullOrEmpty(existingEvent.ImageUrl))
                    {
                        var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, existingEvent.ImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }
                    // Logic to update the event, including handling a new image upload
                    string wwwRootPath = _webHostEnvironment.WebRootPath;
                    string fileName = Path.GetFileNameWithoutExtension(model.ImageFile.FileName);
                    string extension = Path.GetExtension(model.ImageFile.FileName);
                    fileName = fileName + DateTime.Now.ToString("yymmssfff") + extension;
                    model.ImageUrl = "/images/" + fileName;
                    string path = Path.Combine(wwwRootPath, "images", fileName);
                    using (var fileStream = new FileStream(path, FileMode.Create))
                    {
                        await model.ImageFile.CopyToAsync(fileStream);
                    }
                }

                model.UpdatedAt = DateTime.UtcNow;
                model.OrganizerId = existingEvent.OrganizerId;
                model.CreatedAt = existingEvent.CreatedAt;

                try
                {
                    _context.Update(model);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EventExists(model.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                TempData["SuccessMessage"] = "Event updated successfully!";
                return RedirectToAction(nameof(MyEvents));
            }
            return View(model);
        }
        // Delete event Logic
        [HttpGet]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var eventToDelete = await _context.Events
                .FirstOrDefaultAsync(e => e.Id == id && e.OrganizerId == userIdString);

            if (eventToDelete == null)
            {
                return NotFound();
            }

            return View(eventToDelete);
        }

        [HttpPost, ActionName("DeleteEvent")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEventConfirmed(int id)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var eventToDelete = await _context.Events
                .FirstOrDefaultAsync(e => e.Id == id && e.OrganizerId == userIdString);

            if (eventToDelete == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(eventToDelete.ImageUrl))
            {
                var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, eventToDelete.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
            }

            _context.Events.Remove(eventToDelete);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Event deleted successfully!";
            return RedirectToAction(nameof(MyEvents));
        }

        // Reports Logic for organizer
        public async Task<IActionResult> Reports()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var events = await _context.Events
                .Include(e => e.Bookings)
                .Where(e => e.OrganizerId == userIdString)
                .ToListAsync();

            ViewBag.TotalEvents = events.Count;
            ViewBag.TotalRevenue = events
                .SelectMany(e => e.Bookings)
                .Where(b => b.Status == "Confirmed" || b.Status == "Completed")
                .Sum(b => b.TotalAmount)
                .ToString("N2");
            ViewBag.TotalTicketsSold = events
                .SelectMany(e => e.Bookings)
                .Where(b => b.Status == "Confirmed" || b.Status == "Completed")
                .Sum(b => b.TicketQuantity);

            return View(events);
        }

        private bool EventExists(int id)
        {
            return _context.Events.Any(e => e.Id == id);
        }


        // PROFILE EDITING FOR ORGANIZERS 
        #region Profile Management

        // GET: Organizers/EditProfile
        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge(); // Or NotFound()
            }

            var userId = user.Id;
            var events = await _context.Events
                .Where(e => e.OrganizerId == userId)
                .Include(e => e.Bookings)
                .ToListAsync();

            ViewBag.TotalEvents = events.Count;
            ViewBag.TotalTicketsSold = events.SelectMany(e => e.Bookings).Where(b => b.Status == "Confirmed").Sum(b => b.TicketQuantity);
            ViewBag.TotalRevenue = events.SelectMany(e => e.Bookings).Where(b => b.Status == "Confirmed").Sum(b => b.TotalAmount);

            var profileViewModel = new ProfileViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                CreatedAt = user.CreatedAt
            };

            return View(profileViewModel);
        }

        // POST: Organizers/EditProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(OrganizerProfileUpdateModel model)
        {
            // Check if the submitted data is valid based on the new model's rules
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please ensure all required fields are filled out correctly.";
                return RedirectToAction(nameof(EditProfile));
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                // This should not happen for a logged-in user, but it's a good safeguard
                return NotFound("Unable to load user.");
            }

            // Update the user entity with the data from the model
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.PhoneNumber = model.PhoneNumber;

            // Persist the changes to the database
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Your profile has been updated successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to update profile. Please try again.";
                // Optionally, you can log the errors for debugging purposes
                // foreach (var error in result.Errors) { ... }
            }

            // Redirect back to the profile page to show the result of the operation
            return RedirectToAction(nameof(EditProfile));
        }

        // POST: Organizers/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword) || newPassword != confirmPassword)
            {
                TempData["ErrorMessage"] = "Please check your password fields.";
                return RedirectToAction(nameof(EditProfile));
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
                return RedirectToAction(nameof(EditProfile));
            }

            TempData["SuccessMessage"] = "Your password has been changed successfully.";
            return RedirectToAction(nameof(EditProfile));
        }

        #endregion
    }
}



