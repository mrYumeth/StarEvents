using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarEvents.Data;
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

        // ============================
        // ORGANIZER DASHBOARD
        // ============================
        public IActionResult Index()
        {
            return View();
        }

        // ============================
        // CREATE EVENT (GET)
        // ============================
        [HttpGet]
        public IActionResult CreateEvent()
        {
            return View();
        }

        // ============================
        // CREATE EVENT (POST)
        // ============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEvent(Event model)
        {
            // ✅ Assign organizer ID and Organizer object before validating
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            model.OrganizerId = userId;

            // Optional: attach Organizer object if needed
            model.Organizer = await _context.Users.FindAsync(userId);

            // Clear Organizer-related model state errors (if already added)
            ModelState.Remove("OrganizerId");
            ModelState.Remove("Organizer");

            if (ModelState.IsValid)
            {
                // ✅ Venue handling
                if (model.Venue != null && !string.IsNullOrWhiteSpace(model.Venue.VenueName))
                {
                    var venue = new Venue
                    {
                        VenueName = model.Venue.VenueName
                    };

                    _context.Venues.Add(venue);
                    await _context.SaveChangesAsync();
                    model.VenueId = venue.Id;
                }

                model.CreatedAt = DateTime.UtcNow;

                _context.Events.Add(model);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(MyEvents));
            }

            return View(model);
        }


        // ============================
        // MY EVENTS PAGE
        // ============================
        public async Task<IActionResult> MyEvents()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var events = await _context.Events
                .Include(e => e.Venue)
                .Where(e => e.OrganizerId == userId)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

            return View(events);
        }
    }
}
