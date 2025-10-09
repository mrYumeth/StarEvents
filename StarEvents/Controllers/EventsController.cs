using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarEvents.Data;
using StarEvents.ViewModels;
using System.Linq;
using System.Threading.Tasks;

// We do not require authorization on this controller as users should be able to browse events while logged out.
namespace StarEvents.Controllers
{
    // The EventsController handles public-facing event browsing and searching.
    // Users are directed straight to the Bookings controller for event details and booking.
    public class EventsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EventsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ----------------------------------------------------------------------
        // 1. Index - Display all active events
        // ----------------------------------------------------------------------
        // GET: /Events or /Events/Index
        public async Task<IActionResult> Index()
        {
            var events = await _context.Events
                .Where(e => e.IsActive && e.Status == "Active")
                .Include(e => e.Venue)
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

            return View(events);
        }

        // ----------------------------------------------------------------------
        // 2. Search (GET) - Displays the initial search page.
        // ----------------------------------------------------------------------
        // GET: /Events/Search
        public async Task<IActionResult> Search()
        {
            // When the page loads initially (GET), display all active events.
            var defaultEvents = await _context.Events
                .Where(e => e.IsActive && e.Status == "Active")
                .Include(e => e.Venue)
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

            return View(defaultEvents);
        }

        // ----------------------------------------------------------------------
        // 3. Search (POST) - Handles the form submission with search criteria.
        // ----------------------------------------------------------------------
        // POST: /Events/Search
        [HttpPost]
        public async Task<IActionResult> Search(SearchCriteriaModel criteria)
        {
            // Start with all active events
            var eventsQuery = _context.Events
                .Where(e => e.IsActive && e.Status == "Active")
                .Include(e => e.Venue)
                .AsQueryable();

            // Filter by Category
            if (!string.IsNullOrEmpty(criteria.Category) && criteria.Category != "All")
            {
                eventsQuery = eventsQuery.Where(e => e.Category == criteria.Category);
            }

            // Filter by Location
            if (!string.IsNullOrEmpty(criteria.Location) && criteria.Location != "Anywhere")
            {
                eventsQuery = eventsQuery.Where(e =>
                    EF.Functions.Like(e.Venue.City, $"%{criteria.Location}%"));
            }

            // Filter by Date
            if (criteria.Date.HasValue)
            {
                var searchDate = criteria.Date.Value.Date;
                eventsQuery = eventsQuery.Where(e => e.StartDate.Date == searchDate);
            }

            var searchResults = await eventsQuery
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

            return View(searchResults);
        }

        // ----------------------------------------------------------------------
        // 4. Event Details (REMOVED - Users now go directly to Bookings/Book/{id})
        // ----------------------------------------------------------------------
        // The Details action was removed as per the requested simplified flow.
        // Navigation should now link from Index and Search results directly to Bookings/Book/{id}.
    }
}
