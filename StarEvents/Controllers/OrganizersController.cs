using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarEvents.Data;
using System.Security.Claims;
using System.Linq; // Added for .OrderByDescending()
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

        // Dashboard
        public IActionResult Index()
        {
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
        public async Task<IActionResult> CreateEvent(StarEvents.Data.Event model)
        {
            if (ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // FIX 1: Assign the string userId directly to OrganizerId
                model.OrganizerId = userId;

                model.CreatedAt = DateTime.UtcNow;

                _context.Events.Add(model);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(MyEvents));
            }

            return View(model);
        }

        // MY EVENTS PAGE
        public async Task<IActionResult> MyEvents()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // FIX 2: Compare the string OrganizerId directly to the string userId
            var events = await _context.Events
                .Where(e => e.OrganizerId == userId) // Removed Guid.Parse()
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

            return View(events);
        }
    }
}
