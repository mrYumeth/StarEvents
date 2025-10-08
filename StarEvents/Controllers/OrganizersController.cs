using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarEvents.Data;
using System.Security.Claims;
using System; // Ensure System is included for Guid type, although we primarily use string now

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
                // FIX 1 & 2: Get userId as string and assign directly. No Guid conversion needed.
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                model.OrganizerId = currentUserId;  // Link to logged in Organizer (string)
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
            // FIX 3: Capture the userId string once here.
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // FIX 1: Use the captured string variable in the query
            var events = await _context.Events
                // Filter where the Event's OrganizerId (string) equals the current userIdString
                .Where(e => e.OrganizerId == userIdString)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

            return View(events);
        }
    }
}
