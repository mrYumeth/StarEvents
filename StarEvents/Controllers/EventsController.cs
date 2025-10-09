using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarEvents.Data;
using StarEvents.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace StarEvents.Controllers
{
    // The EventsController handles public-facing event browsing, searching, and viewing details.
    // Users can browse events without logging in.
    public class EventsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EventsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ----------------------------------------------------------------------
        // 1. Index - Display all active events with search/filter functionality
        // ----------------------------------------------------------------------
        // GET: /Events or /Events/Index
        // Supports query parameters: category, location, dateFrom, keyword
        public async Task<IActionResult> Index(string? category, string? location, DateTime? dateFrom, string? keyword)
        {
            // Start with all active events
            var query = _context.Events
                .Where(e => e.IsActive && e.Status == "Active")
                .Include(e => e.Venue)
                .AsQueryable();

            // Apply filters based on query parameters

            // Filter by Category
            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(e => e.Category == category);
            }

            // Filter by Location (City)
            if (!string.IsNullOrEmpty(location))
            {
                query = query.Where(e => e.Venue.City == location);
            }

            // Filter by Date (events on or after the specified date)
            if (dateFrom.HasValue)
            {
                query = query.Where(e => e.StartDate.Date >= dateFrom.Value.Date);
            }

            // Filter by Keyword (search in title or description)
            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(e =>
                    e.Title.Contains(keyword) ||
                    e.Description.Contains(keyword));
            }

            // Execute query and map to ViewModel
            var events = await query
                .OrderBy(e => e.StartDate)
                .Select(e => new EventViewModel
                {
                    Id = e.Id,
                    Title = e.Title,
                    Category = e.Category,
                    DateRange = e.StartDate.ToString("MMM d, yyyy"),
                    LocationName = e.Venue.City,
                    PriceDisplay = $"LKR {e.TicketPrice:N2}"
                })
                .ToListAsync();

            // Store current filter values in ViewBag for preserving filter state
            ViewBag.CurrentCategory = category;
            ViewBag.CurrentLocation = location;
            ViewBag.CurrentDateFrom = dateFrom?.ToString("yyyy-MM-dd");
            ViewBag.CurrentKeyword = keyword;

            return View(events);
        }

        // ----------------------------------------------------------------------
        // 2. Details - Display detailed information about a specific event
        // ----------------------------------------------------------------------
        // GET: /Events/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var eventEntity = await _context.Events
                .Include(e => e.Venue)
                .Include(e => e.Organizer)
                .Where(e => e.Id == id)
                .FirstOrDefaultAsync();

            if (eventEntity == null)
            {
                TempData["ErrorMessage"] = "Event not found.";
                return RedirectToAction(nameof(Index));
            }

            // Check if event is still active
            if (!eventEntity.IsActive)
            {
                TempData["ErrorMessage"] = "This event is no longer available.";
                return RedirectToAction(nameof(Index));
            }

            // Map to ViewModel
            var viewModel = new EventDetailsViewModel
            {
                Id = eventEntity.Id,
                Title = eventEntity.Title,
                Description = eventEntity.Description,
                Category = eventEntity.Category,
                StartDate = eventEntity.StartDate,

                DateDisplay = eventEntity.StartDate.ToString("ddd, MMM d, yyyy h:mm tt") +
                              (eventEntity.EndDate.HasValue
                                ? $" - {eventEntity.EndDate.Value.ToString("h:mm tt")}"
                                : ""),

                VenueName = eventEntity.Venue.VenueName,
                VenueAddress = $"{eventEntity.Venue.Address}, {eventEntity.Venue.City}",
                VenueCity = eventEntity.Venue.City,

                OrganizerName = $"{eventEntity.Organizer.FirstName} {eventEntity.Organizer.LastName}",

                AvailableTickets = eventEntity.AvailableTickets ?? 0,
                TicketPrice = $"LKR {eventEntity.TicketPrice:N2}"
            };

            return View(viewModel);
        }

        // ----------------------------------------------------------------------
        // 3. Search (Legacy) - Kept for backward compatibility if needed
        // ----------------------------------------------------------------------
        // GET: /Events/Search
        public async Task<IActionResult> Search()
        {
            // Redirect to Index with no filters (shows all events)
            return RedirectToAction(nameof(Index));
        }

        // ----------------------------------------------------------------------
        // 4. Search (POST) - Handles legacy search form submissions
        // ----------------------------------------------------------------------
        // POST: /Events/Search
        [HttpPost]
        public async Task<IActionResult> Search(SearchCriteriaModel criteria)
        {
            // Redirect to Index with query parameters
            return RedirectToAction(nameof(Index), new
            {
                category = criteria.Category,
                location = criteria.Location,
                dateFrom = criteria.Date,
                keyword = "" // SearchCriteriaModel doesn't have keyword, but Index supports it
            });
        }
    }
}