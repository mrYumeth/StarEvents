using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarEvents.Data;
using System.Security.Claims;

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
                model.OrganizerId = Guid.Parse(userId);   // Link to logged in Organizer
                model.CreatedAt = DateTime.UtcNow;        // Make sure Event model has CreatedAt

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
            var organizerGuid = Guid.Parse(userId);

            var events = await _context.Events
                .Where(e => e.OrganizerId == organizerGuid)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

            return View(events);
        }
    }
}
