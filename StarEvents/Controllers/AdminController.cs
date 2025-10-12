using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StarEvents.Data;
using StarEvents.Models;
using StarEvents.ViewModels;
using System.Text;
using System.Threading.Tasks;

namespace StarEvents.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // Dashboard
        public async Task<IActionResult> Index()
        {
            var vm = new AdminDashboardViewModel
            {
                TotalUsers = await _db.Users.CountAsync(),
                TotalEvents = await _db.Events.CountAsync(),
                TotalVenues = await _db.Venues.CountAsync(),
                TotalBookings = await _db.Bookings.CountAsync(),
                RecentEvents = await _db.Events.Include(e => e.Venue).OrderByDescending(e => e.CreatedAt).Take(5).ToListAsync(),
                RecentUsers = await _db.Users.OrderByDescending(u => u.Id).Take(5).ToListAsync()
            };
            return View(vm);
        }

        #region Events

        public async Task<IActionResult> Events()
        {
            var list = await _db.Events
                .Include(e => e.Venue)
                .Include(e => e.Bookings)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
            return View(list);
        }

        // Helper method to populate SelectLists for Venues and Organizers
        private async Task PopulateEventDropdowns(int? selectedVenueId = null, string selectedOrganizerId = null)
        {
            ViewBag.Venues = await _db.Venues
                .OrderBy(v => v.VenueName)
                .Select(v => new SelectListItem
                {
                    Value = v.Id.ToString(),
                    Text = v.VenueName,
                    Selected = v.Id == selectedVenueId
                })
                .ToListAsync();

            var organizers = await _userManager.GetUsersInRoleAsync("Organizer");
            ViewBag.Organizers = organizers
                .OrderBy(u => u.FirstName).ThenBy(u => u.LastName)
                .Select(u => new SelectListItem
                {
                    Value = u.Id,
                    Text = $"{u.FirstName} {u.LastName} ({u.Email})",
                    Selected = u.Id == selectedOrganizerId
                })
                .ToList();
        }

        [HttpGet]
        public async Task<IActionResult> CreateEvent()
        {
            await PopulateEventDropdowns();
            return View(new AdminEventViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEvent(AdminEventViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // FIX 1 (Part 1): Repopulate ViewBag for dropdowns on validation failure
                await PopulateEventDropdowns(model.VenueId, model.OrganizerId);
                return View(model);
            }

            var ev = new Event
            {
                Title = model.Title,
                Description = model.Description,
                Category = model.Category,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                Status = model.Status,
                VenueId = model.VenueId,
                OrganizerId = model.OrganizerId,
                TicketPrice = model.TicketPrice,
                AvailableTickets = model.AvailableTickets,
                CreatedAt = DateTime.UtcNow
            };

            _db.Events.Add(ev);
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Event created.";
            return RedirectToAction(nameof(Events));
        }

        [HttpGet]
        public async Task<IActionResult> EditEvent(int id)
        {
            var ev = await _db.Events.FindAsync(id);
            if (ev == null) return NotFound();

            var vm = new AdminEventViewModel
            {
                Id = ev.Id,
                Title = ev.Title,
                Description = ev.Description,
                Category = ev.Category,
                StartDate = ev.StartDate,
                EndDate = ev.EndDate,
                Status = ev.Status,
                VenueId = ev.VenueId,
                OrganizerId = ev.OrganizerId,
                TicketPrice = ev.TicketPrice,
                AvailableTickets = ev.AvailableTickets ?? 0,
                // Removed error-prone property assignments from ViewModel (original lines 419 & 428 location)
            };

            // FIX 2 (Part 1): Populate ViewBag for dropdowns
            await PopulateEventDropdowns(ev.VenueId, ev.OrganizerId);

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEvent(AdminEventViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // FIX 1 (Part 2): Repopulate ViewBag for dropdowns on validation failure
                await PopulateEventDropdowns(model.VenueId, model.OrganizerId);
                return View(model);
            }

            var ev = await _db.Events.FindAsync(model.Id);
            if (ev == null) return NotFound();

            ev.Title = model.Title;
            ev.Description = model.Description;
            ev.Category = model.Category;
            ev.StartDate = model.StartDate;
            ev.EndDate = model.EndDate;
            ev.Status = model.Status;
            ev.VenueId = model.VenueId;
            ev.OrganizerId = model.OrganizerId;
            ev.TicketPrice = model.TicketPrice;
            ev.AvailableTickets = model.AvailableTickets;

            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Event updated.";
            return RedirectToAction(nameof(Events));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var ev = await _db.Events.Include(e => e.Bookings).FirstOrDefaultAsync(e => e.Id == id);
            if (ev == null) return NotFound();

            if (ev.Bookings != null && ev.Bookings.Any())
            {
                TempData["ErrorMessage"] = "Cannot delete an event with bookings.";
                return RedirectToAction(nameof(Events));
            }

            _db.Events.Remove(ev);
            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = "Event deleted.";
            return RedirectToAction(nameof(Events));
        }

        #endregion

        #region Venues

        public async Task<IActionResult> Venues()
        {
            var list = await _db.Venues.OrderBy(v => v.VenueName).ToListAsync();
            foreach (var v in list)
            {
                v.EventCount = await _db.Events.CountAsync(e => e.VenueId == v.Id);
            }
            return View(list);
        }

        [HttpGet]
        public IActionResult CreateVenue() => View(new Venue());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateVenue(Venue model)
        {
            if (!ModelState.IsValid) return View(model);
            _db.Venues.Add(model);
            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = "Venue created.";
            return RedirectToAction(nameof(Venues));
        }

        [HttpGet]
        public async Task<IActionResult> EditVenue(int id)
        {
            var v = await _db.Venues.FindAsync(id);
            if (v == null) return NotFound();
            return View(v);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditVenue(Venue model)
        {
            if (!ModelState.IsValid) return View(model);
            _db.Venues.Update(model);
            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = "Venue updated.";
            return RedirectToAction(nameof(Venues));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVenue(int id)
        {
            var v = await _db.Venues.FindAsync(id);
            if (v == null) return NotFound();

            var hasEvents = await _db.Events.AnyAsync(e => e.VenueId == id);
            if (hasEvents)
            {
                TempData["ErrorMessage"] = "Cannot delete a venue with scheduled events.";
                return RedirectToAction(nameof(Venues));
            }

            _db.Venues.Remove(v);
            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = "Venue deleted.";
            return RedirectToAction(nameof(Venues));
        }

        #endregion

        #region Users

        public async Task<IActionResult> Users()
        {
            var users = await _db.Users.OrderBy(u => u.LastName).ThenBy(u => u.FirstName).ToListAsync();
            var vm = new List<AdminUserViewModel>();
            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                vm.Add(new AdminUserViewModel
                {
                    Id = u.Id,
                    FirstName = u.FirstName ?? "",
                    LastName = u.LastName ?? "",
                    Email = u.Email ?? "",
                    PhoneNumber = u.PhoneNumber,
                    LoyaltyPoints = u.LoyaltyPoints,
                    EmailConfirmed = u.EmailConfirmed,
                    Roles = roles.ToList(),
                    CreatedAt = DateTime.UtcNow
                });
            }
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserRole(string userId, string role)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(role)) return BadRequest();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            if (await _userManager.IsInRoleAsync(user, role))
                await _userManager.RemoveFromRoleAsync(user, role);
            else
                await _userManager.AddToRoleAsync(user, role);

            TempData["SuccessMessage"] = "User role updated.";
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return BadRequest();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var result = await _userManager.DeleteAsync(user);
            TempData["SuccessMessage"] = result.Succeeded ? "User deleted." : "Delete failed.";
            return RedirectToAction(nameof(Users));
        }

        #endregion

        #region Reports & Exports

        public async Task<IActionResult> Reports()
        {
            var bookings = _db.Bookings.AsQueryable();
            var vm = new AdminReportsViewModel
            {
                TotalRevenue = await bookings.SumAsync(b => (decimal?)b.TotalAmount) ?? 0m,
                TotalTicketsSold = await bookings.SumAsync(b => (int?)b.TicketQuantity) ?? 0,
                EventsByCategory = await _db.Events
                    .GroupBy(e => e.Category)
                    .Select(g => new CategoryStats { Category = g.Key ?? "Uncategorized", Count = g.Count() })
                    .ToListAsync(),
                RevenueByMonth = await bookings
                    .Where(b => b.BookingDate >= DateTime.UtcNow.AddMonths(-12))
                    .GroupBy(b => new { b.BookingDate.Year, b.BookingDate.Month })
                    .Select(g => new MonthlyRevenue { Year = g.Key.Year, Month = g.Key.Month, Revenue = g.Sum(x => x.TotalAmount), TicketsSold = g.Sum(x => x.TicketQuantity) })
                    .OrderBy(r => r.Year).ThenBy(r => r.Month).ToListAsync(),
                TopEvents = await _db.Events
                    .Select(e => new TopEventStats { EventTitle = e.Title, TicketsSold = e.Bookings.Sum(b => b.TicketQuantity), Revenue = e.Bookings.Sum(b => b.TotalAmount) })
                    .OrderByDescending(t => t.Revenue).Take(10).ToListAsync()
            };
            return View(vm);
        }

        [HttpGet]
        public async Task<FileResult> ExportReport(string reportType)
        {
            string csv = reportType switch
            {
                "users" => await GenerateUsersCsv(),
                "events" => await GenerateEventsCsv(),
                "bookings" => await GenerateBookingsCsv(),
                _ => string.Empty
            };

            if (string.IsNullOrEmpty(csv)) return File(new byte[0], "text/csv");

            var fileName = $"{reportType}_report_{DateTime.UtcNow:yyyyMMdd}.csv";
            return File(Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
        }

        private async Task<string> GenerateUsersCsv()
        {
            var list = await _db.Users.ToListAsync();
            var sb = new StringBuilder();
            sb.AppendLine("Id,FirstName,LastName,Email,Phone,LoyaltyPoints,EmailConfirmed");
            foreach (var u in list)
                sb.AppendLine($"{Escape(u.Id)},{Escape(u.FirstName)},{Escape(u.LastName)},{Escape(u.Email)},{Escape(u.PhoneNumber)},{u.LoyaltyPoints},{u.EmailConfirmed}");
            return sb.ToString();
        }

        private async Task<string> GenerateEventsCsv()
        {
            var list = await _db.Events.Include(e => e.Venue).Include(e => e.Bookings).ToListAsync();
            var sb = new StringBuilder();
            sb.AppendLine("Id,Title,Category,StartDate,Status,Venue,Price,AvailableTickets,BookingsCount");
            foreach (var e in list)
                sb.AppendLine($"{e.Id},{Escape(e.Title)},{Escape(e.Category)},{e.StartDate:yyyy-MM-dd},{Escape(e.Status)},{Escape(e.Venue?.VenueName)},{e.TicketPrice:F2},{e.AvailableTickets ?? 0},{e.Bookings?.Count ?? 0}");
            return sb.ToString();
        }

        private async Task<string> GenerateBookingsCsv()
        {
            var list = await _db.Bookings.Include(b => b.Customer).Include(b => b.Event).ToListAsync();
            var sb = new StringBuilder();
            sb.AppendLine("Id,Customer,Event,Quantity,TotalAmount,Status,BookingDate");
            foreach (var b in list)
                sb.AppendLine($"{b.Id},{Escape(b.Customer?.Email)},{Escape(b.Event?.Title)},{b.TicketQuantity},{b.TotalAmount:F2},{Escape(b.Status)},{b.BookingDate:yyyy-MM-dd}");
            return sb.ToString();
        }

        private string Escape(string? s) => string.IsNullOrEmpty(s) ? "" : $"\"{s.Replace("\"", "\"\"")}\"";

        #endregion
    }
}