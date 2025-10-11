using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting; // FIX: Added this using statement
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarEvents.Data;
using StarEvents.Models;
using System;
using System.IO; // FIX: Added this using statement
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

        public OrganizersController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // Dashboard / Index
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

            return View();
        }

        // CREATE EVENT (GET)
        [HttpGet]
        public IActionResult CreateEvent()
        {
            return View();
        }

        // CREATE EVENT (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEvent(Event model)
        {
            ModelState.Remove("OrganizerId");
            ModelState.Remove("Organizer");

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
                model.UpdatedAt = DateTime.UtcNow;

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

        // MY EVENTS PAGE
        public async Task<IActionResult> MyEvents()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var events = await _context.Events
                .Where(e => e.OrganizerId == userIdString)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
            return View(events);
        }

        // EDIT EVENT (GET)
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

        // EDIT EVENT (POST)
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

            if (ModelState.IsValid)
            {
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var existingEvent = await _context.Events
                    .FirstOrDefaultAsync(e => e.Id == id && e.OrganizerId == userIdString);

                if (existingEvent == null)
                {
                    return NotFound();
                }

                existingEvent.Title = model.Title;
                existingEvent.Description = model.Description;
                existingEvent.Category = model.Category;
                existingEvent.VenueName = model.VenueName;
                existingEvent.Location = model.Location;
                existingEvent.StartDate = model.StartDate;
                existingEvent.EndDate = model.EndDate;
                existingEvent.TicketPrice = model.TicketPrice;
                existingEvent.AvailableTickets = model.AvailableTickets;
                existingEvent.Status = model.Status;
                existingEvent.IsActive = model.IsActive;
                existingEvent.UpdatedAt = DateTime.UtcNow;

                if (model.ImageFile != null)
                {
                    existingEvent.ImageUrl = model.ImageUrl;
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Event updated successfully!";
                return RedirectToAction(nameof(MyEvents));
            }
            return View(model);
        }

        // DELETE EVENT (GET)
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

        // DELETE EVENT (POST)
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

            _context.Events.Remove(eventToDelete);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Event deleted successfully!";
            return RedirectToAction(nameof(MyEvents));
        }

        // REPORTS
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
    }
}

