using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StarEvents.Data;
using StarEvents.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace StarEvents.Controllers
{
    public class EventsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EventsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? category, string? location, DateTime? dateFrom, string? keyword)
        {
            // --- FIX: Fetch data for dropdowns ---
            var categories = await _context.Events
                .Where(e => e.IsActive && e.Status == "Active" && !string.IsNullOrEmpty(e.Category))
                .Select(e => e.Category)
                .Distinct()
                .ToListAsync();

            var locations = await _context.Events
                .Where(e => e.IsActive && e.Status == "Active" && !string.IsNullOrEmpty(e.Location))
                .Select(e => e.Location)
                .Distinct()
                .ToListAsync();

            ViewBag.Categories = new SelectList(categories);
            ViewBag.Locations = new SelectList(locations);
            // --- END FIX ---

            var query = _context.Events
                .Where(e => e.IsActive && e.Status == "Active")
                .AsQueryable();

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(e => e.Category == category);
            }

            if (!string.IsNullOrEmpty(location))
            {
                query = query.Where(e => e.Location == location);
            }

            // --- FIX: Correct date filtering logic ---
            if (dateFrom.HasValue)
            {
                // Compare the date part only, ignoring the time
                query = query.Where(e => e.StartDate.Date >= dateFrom.Value.Date);
            }
            // --- END FIX ---

            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(e =>
                    e.Title.Contains(keyword) ||
                    e.Description.Contains(keyword));
            }

            var events = await query
                .OrderBy(e => e.StartDate)
                .Select(e => new EventViewModel
                {
                    Id = e.Id,
                    Title = e.Title,
                    Category = e.Category,
                    DateRange = e.StartDate.ToString("MMM d, yyyy"),
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

        public async Task<IActionResult> Details(int id)
        {
            var eventEntity = await _context.Events
                .Include(e => e.Organizer)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (eventEntity == null || !eventEntity.IsActive)
            {
                TempData["ErrorMessage"] = "Event not found or is no longer available.";
                return RedirectToAction(nameof(Index));
            }

            var viewModel = new EventDetailsViewModel
            {
                Id = eventEntity.Id,
                Title = eventEntity.Title,
                Description = eventEntity.Description,
                Category = eventEntity.Category,
                StartDate = eventEntity.StartDate,
                EndDate = eventEntity.EndDate, // Now a non-nullable DateTime
                ImageUrl = eventEntity.ImageUrl,

                // --- THIS LOGIC IS CORRECTED ---
                DateDisplay = eventEntity.StartDate.ToString("ddd, MMM d, yyyy h:mm tt") +
                              (eventEntity.EndDate > eventEntity.StartDate // <-- THIS LINE CHANGES
                                  ? $" - {eventEntity.EndDate.ToString("h:mm tt")}" // <-- THIS LINE CHANGES
                                  : ""),

                VenueName = eventEntity.VenueName,
                VenueAddress = $"{eventEntity.VenueName}, {eventEntity.Location}",
                VenueCity = eventEntity.Location,
                OrganizerName = $"{eventEntity.Organizer.FirstName} {eventEntity.Organizer.LastName}",
                AvailableTickets = eventEntity.AvailableTickets ??0,
                TicketPrice = $"LKR {eventEntity.TicketPrice:N2}"
            };

            return View(viewModel);
        }
    }
}
