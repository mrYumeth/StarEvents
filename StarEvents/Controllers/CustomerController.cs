using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarEvents.Data;
using StarEvents.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace StarEvents.Controllers
{
    public class CustomerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        // Constructor for Dependency Injection (DI)
        public CustomerController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ----------------------------------------------------------------------
        // 0. Index - Customer Overview/Landing Page (Redirects to Search)
        // ----------------------------------------------------------------------
        // GET: /Customer/Index (or just /Customer)
        public IActionResult Index()
        {
            // We use the Search page as the main customer landing/overview page.
            return RedirectToAction(nameof(Search));
        }

        // ----------------------------------------------------------------------
        // 1. Search (GET) - Displays the initial search page.
        // ----------------------------------------------------------------------

        // GET: /Customer/Search
        public async Task<IActionResult> Search()
        {
            // When the page loads initially (GET), display all published events.
            var defaultEvents = await _context.Events
                .Where(e => e.Status == "Published")
                .Include(e => e.Venue)
                .OrderBy(e => e.StartDate)
                .Select(e => new EventViewModel
                {
                    Id = e.Id,
                    Title = e.Title,
                    DateRange = e.StartDate.ToString("MMM d, yyyy"),
                    LocationName = e.Venue.City, // Use City for the main location display
                    PriceDisplay = "Price Varies" // Placeholder
                })
                .ToListAsync();

            return View(defaultEvents);
        }

        // ----------------------------------------------------------------------
        // 2. Search (POST) - Handles the form submission with search criteria.
        // ----------------------------------------------------------------------

        // POST: /Customer/Search
        [HttpPost]
        public async Task<IActionResult> Search(SearchCriteriaModel criteria) // Receives the data from the form
        {
            // Start with all published events
            var eventsQuery = _context.Events
                .Where(e => e.Status == "Published")
                .Include(e => e.Venue) // Need Venue data for location filtering
                .AsQueryable();

            // 1. Filter by Category (if criteria is provided)
            if (!string.IsNullOrEmpty(criteria.Category) && criteria.Category != "All")
            {
                eventsQuery = eventsQuery.Where(e => e.Category == criteria.Category);
            }

            // 2. Filter by Location (if criteria is provided)
            // Note: We are filtering by the Venue's City property.
            if (!string.IsNullOrEmpty(criteria.Location) && criteria.Location != "Anywhere")
            {
                // We use EF.Functions.Like for a flexible search, or you can use .Contains()
                eventsQuery = eventsQuery.Where(e =>
                    EF.Functions.Like(e.Venue.City, $"%{criteria.Location}%"));
            }

            // Execute the query and map the results to the ViewModel
            var searchResults = await eventsQuery
                .OrderBy(e => e.StartDate)
                .Select(e => new EventViewModel
                {
                    Id = e.Id,
                    Title = e.Title,
                    // Simple date formatting, modify as needed
                    DateRange = e.StartDate.ToString("MMM d, yyyy"),
                    LocationName = e.Venue.City,
                    PriceDisplay = "Price Varies" // Placeholder 
                })
                .ToListAsync();

            // Return the search results list back to the same Search.cshtml view.
            return View(searchResults);
        }

        // ----------------------------------------------------------------------
        // 3. Event Details - (Unchanged and working)
        // ----------------------------------------------------------------------

        // GET: /Customer/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var eventEntity = await _context.Events
                .Include(e => e.Venue)
                .Include(e => e.Organizer)
                .Where(e => e.Id == id)
                .FirstOrDefaultAsync();

            if (eventEntity == null)
            {
                return NotFound();
            }

            var viewModel = new EventDetailsViewModel
            {
                Id = eventEntity.Id,
                Title = eventEntity.Title,
                Description = eventEntity.Description,
                Category = eventEntity.Category,
                StartDate = eventEntity.StartDate,

                DateDisplay = eventEntity.StartDate.ToString("ddd, MMM d, yyyy h:mm tt") +
                              (eventEntity.EndDate.HasValue ? $" - {eventEntity.EndDate.Value.ToString("h:mm tt")}" : ""),

                VenueName = eventEntity.Venue.Name, // Confirmed correct usage of Venue.Name
                VenueAddress = $"{eventEntity.Venue.Address}, {eventEntity.Venue.City}",
                VenueCity = eventEntity.Venue.City,

                OrganizerName = $"{eventEntity.Organizer.FirstName} {eventEntity.Organizer.LastName}",

                AvailableTickets = 500,
                TicketPrice = "LKR 3000 - LKR 10000"
            };

            return View(viewModel);
        }
    }
}
