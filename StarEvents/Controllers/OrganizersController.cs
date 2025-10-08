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
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                model.OrganizerId = Guid.Parse(userId);   // Link to logged in Organizer
                model.CreatedAt = DateTime.UtcNow;        // Make sure Event model has CreatedAt

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

            // FIX 2: Compare the string OrganizerId directly to the string userId
            var events = await _context.Events
                .Where(e => e.OrganizerId == organizerGuid)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

            return View(events);
        }
    }
}
