using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StarEvents.Data;
using StarEvents.Models; // ADDED: Import Models namespace
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace StarEvents.Controllers
{
    [Authorize(Roles = "Organizer")]
    public class OrganizersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrganizersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ----------------------------------------------------------------------
        // Dashboard / Index
        // ----------------------------------------------------------------------
        public IActionResult Index()
        {
            return RedirectToAction(nameof(MyEvents));
        }

        // ----------------------------------------------------------------------
        // CREATE EVENT (GET)
        // ----------------------------------------------------------------------
        [HttpGet]
        public IActionResult CreateEvent()
        {
            // Populate venues dropdown for the form
            ViewBag.Venues = new SelectList(
                _context.Venues.Where(v => v.IsActive),
                "Id",
                "VenueName"
            );

            return View();
        }

        // ----------------------------------------------------------------------
        // CREATE EVENT (POST)
        // ----------------------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEvent(Event model)
        {
            if (ModelState.IsValid)
            {
                // Get the current logged-in organizer's ID
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                model.OrganizerId = currentUserId;
                model.CreatedAt = DateTime.UtcNow;
                model.UpdatedAt = DateTime.UtcNow;

                // If Status is not set, default to Draft
                if (string.IsNullOrEmpty(model.Status))
                {
                    model.Status = "Draft";
                }

                _context.Events.Add(model);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Event created successfully!";
                return RedirectToAction(nameof(MyEvents));
            }

            // If validation failed, repopulate the venues dropdown
            ViewBag.Venues = new SelectList(
                _context.Venues.Where(v => v.IsActive),
                "Id",
                "VenueName",
                model.VenueId
            );

            return View(model);
        }

        // ----------------------------------------------------------------------
        // MY EVENTS PAGE
        // ----------------------------------------------------------------------
        public async Task<IActionResult> MyEvents()
        {
            // Get current organizer's ID
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Fetch all events created by this organizer
            var events = await _context.Events
                .Include(e => e.Venue) // Include venue information
                .Where(e => e.OrganizerId == userIdString)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

            return View(events);
        }

        // ----------------------------------------------------------------------
        // EDIT EVENT (GET)
        // ----------------------------------------------------------------------
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

            // Populate venues dropdown
            ViewBag.Venues = new SelectList(
                _context.Venues.Where(v => v.IsActive),
                "Id",
                "VenueName",
                eventToEdit.VenueId
            );

            return View(eventToEdit);
        }

        // ----------------------------------------------------------------------
        // EDIT EVENT (POST)
        // ----------------------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEvent(int id, Event model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Ensure the event belongs to the current organizer
            var existingEvent = await _context.Events
                .FirstOrDefaultAsync(e => e.Id == id && e.OrganizerId == userIdString);

            if (existingEvent == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Update properties
                    existingEvent.Title = model.Title;
                    existingEvent.Description = model.Description;
                    existingEvent.Category = model.Category;
                    existingEvent.VenueId = model.VenueId;
                    existingEvent.StartDate = model.StartDate;
                    existingEvent.EndDate = model.EndDate;
                    existingEvent.TicketPrice = model.TicketPrice;
                    existingEvent.AvailableTickets = model.AvailableTickets;
                    existingEvent.ImageUrl = model.ImageUrl;
                    existingEvent.Status = model.Status;
                    existingEvent.IsActive = model.IsActive;
                    existingEvent.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Event updated successfully!";
                    return RedirectToAction(nameof(MyEvents));
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
            }

            // If validation failed, repopulate the venues dropdown
            ViewBag.Venues = new SelectList(
                _context.Venues.Where(v => v.IsActive),
                "Id",
                "VenueName",
                model.VenueId
            );

            return View(model);
        }

        // ----------------------------------------------------------------------
        // DELETE EVENT (GET)
        // ----------------------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var eventToDelete = await _context.Events
                .Include(e => e.Venue)
                .FirstOrDefaultAsync(e => e.Id == id && e.OrganizerId == userIdString);

            if (eventToDelete == null)
            {
                return NotFound();
            }

            return View(eventToDelete);
        }

        // ----------------------------------------------------------------------
        // DELETE EVENT (POST)
        // ----------------------------------------------------------------------
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

        // ----------------------------------------------------------------------
        // REPORTS - View ticket sales and revenue
        // ----------------------------------------------------------------------
        public async Task<IActionResult> Reports()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Get all events by this organizer with their bookings
            var events = await _context.Events
                .Include(e => e.Venue)
                .Include(e => e.Bookings)
                .Where(e => e.OrganizerId == userIdString)
                .ToListAsync();

            // Calculate statistics
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

        // ----------------------------------------------------------------------
        // Helper method to check if event exists
        // ----------------------------------------------------------------------
        private bool EventExists(int id)
        {
            return _context.Events.Any(e => e.Id == id);
        }
    }
}