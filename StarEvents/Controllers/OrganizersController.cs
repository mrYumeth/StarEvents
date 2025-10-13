using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity; // <-- Add this using statement
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarEvents.Data;
using StarEvents.Models;
using StarEvents.ViewModels; // <-- Add this using statement
using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace StarEvents.Controllers
{
    [Authorize(Roles = "Organizer")]
    public class OrganizersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        // --- ADD THIS LINE ---
        private readonly UserManager<ApplicationUser> _userManager;

        // --- UPDATE THE CONSTRUCTOR ---
        public OrganizersController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _userManager = userManager; // Assign the user manager
        }

        // ... (Your existing code from Index() to EventExists() remains unchanged here) ...

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

        public async Task<IActionResult> MyEvents()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var events = await _context.Events
                .Where(e => e.OrganizerId == userIdString)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
            return View(events);
        }

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


        // --- ADD THIS NEW SECTION FOR PROFILE EDITING ---
        #region Profile Management

        [HttpGet]
        public async Task<IActionResult> EditProfile()
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(ProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var userForEmail = await _userManager.GetUserAsync(User);
                if (userForEmail != null) model.Email = userForEmail.Email;
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("Unable to load user.");
            }

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Profile updated successfully!";
                return RedirectToAction(nameof(EditProfile));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            model.Email = user.Email;
            return View(model);
        }

        #endregion
    }
}