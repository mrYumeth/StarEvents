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

        // 1. Index - Display all active events with search/filter functionality
        public async Task<IActionResult> Index(string? category, string? location, DateTime? dateFrom, string? keyword)
        {
            // Start with all active events
            var query = _context.Events
                .Where(e => e.IsActive && e.Status == "Active")
                .AsQueryable();

            // Apply filters based on query parameters

            // Filter by Category
            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(e => e.Category == category);
            }

            // FIX: Filter by the new 'Location' string property
            if (!string.IsNullOrEmpty(location))
            {
                query = query.Where(e => e.Location.Contains(location));
            }

            // Filter by Date
            if (dateFrom.HasValue)
            {
                query = query.Where(e => e.StartDate.Date >= dateFrom.Value.Date);
            }

            // Filter by Keyword
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
                    // FIX: Use the new 'Location' property
                    LocationName = e.Location,
                    PriceDisplay = $"LKR {e.TicketPrice:N2}",
                    ImageUrl = e.ImageUrl
                })
                .ToListAsync();

            ViewBag.CurrentCategory = category;
            ViewBag.CurrentLocation = location;
            ViewBag.CurrentDateFrom = dateFrom?.ToString("yyyy-MM-dd");
            ViewBag.CurrentKeyword = keyword;

            return View(events);
        }

        // 2. Details - Display detailed information about a specific event
        public async Task<IActionResult> Details(int id)
        {
            // FIX: Removed the invalid .Include(e => e.Venue)
            var eventEntity = await _context.Events
                .Include(e => e.Organizer)
                .Where(e => e.Id == id)
                .FirstOrDefaultAsync();

            if (eventEntity == null)
            {
                TempData["ErrorMessage"] = "Event not found.";
                return RedirectToAction(nameof(Index));
            }

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
                ImageUrl = eventEntity.ImageUrl,

                DateDisplay = eventEntity.StartDate.ToString("ddd, MMM d, yyyy h:mm tt") +
                              (eventEntity.EndDate.HasValue
                                  ? $" - {eventEntity.EndDate.Value.ToString("h:mm tt")}"
                                  : ""),

                // FIX: Use the new string properties for Venue and Location
                VenueName = eventEntity.VenueName,
                VenueAddress = $"{eventEntity.VenueName}, {eventEntity.Location}",
                VenueCity = eventEntity.Location,

                OrganizerName = $"{eventEntity.Organizer.FirstName} {eventEntity.Organizer.LastName}",

                AvailableTickets = eventEntity.AvailableTickets ?? 0,
                TicketPrice = $"LKR {eventEntity.TicketPrice:N2}"
            };

            return View(viewModel);
        }

        // Legacy search methods - no changes needed here
        public IActionResult Search()
        {
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult Search(SearchCriteriaModel criteria)
        {
            return RedirectToAction(nameof(Index), new
            {
                category = criteria.Category,
                location = criteria.Location,
                dateFrom = criteria.Date,
                keyword = ""
            });
        }
    }
}