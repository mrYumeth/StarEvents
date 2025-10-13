using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StarEvents.Data;
using StarEvents.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StarEvents.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // ----------------------------------------------------------------------
        // Dashboard
        // ----------------------------------------------------------------------
        public async Task<IActionResult> Dashboard()
        {
            ViewBag.TotalUsers = await _userManager.Users.CountAsync();
            ViewBag.NewUsersThisMonth = await _userManager.Users.CountAsync(u => u.EmailConfirmed); // Placeholder
            ViewBag.TotalEvents = await _context.Events.CountAsync();
            ViewBag.ActiveEvents = await _context.Events.CountAsync(e => e.IsActive && e.Status == "Active");
            ViewBag.TotalBookings = await _context.Bookings.CountAsync();
            ViewBag.BookingsThisMonth = await _context.Bookings.CountAsync(b => b.BookingDate.Month == DateTime.Now.Month);
            var totalRevenue = await _context.Bookings.Where(b => b.Status == "Confirmed" || b.Status == "Completed").SumAsync(b => b.TotalAmount);
            ViewBag.TotalRevenue = totalRevenue.ToString("N0");
            ViewBag.RevenueGrowth = "15"; // Placeholder
            ViewBag.PendingApprovals = await _context.Events.CountAsync(e => e.Status == "Draft");
            return View();
        }

        #region User Management

        // ----------------------------------------------------------------------
        // Manage Users (List)
        // ----------------------------------------------------------------------
        public async Task<IActionResult> ManageUsers()
        {
            var users = await _userManager.Users.ToListAsync();
            var usersWithRoles = new List<object>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                usersWithRoles.Add(new { User = user, Roles = roles });
            }
            return View(usersWithRoles);
        }

        // ----------------------------------------------------------------------
        // GET User Details (for Modal)
        // ----------------------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> GetUserDetails(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            return PartialView("_UserDetailsPartial", user);
        }

        // ----------------------------------------------------------------------
        // POST Update User Details
        // ----------------------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUser(ApplicationUser model)
        {
            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return NotFound();

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Email = model.Email;
            user.PhoneNumber = model.PhoneNumber;
            user.EmailConfirmed = model.EmailConfirmed;
            user.LoyaltyPoints = model.LoyaltyPoints;

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "User details updated successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to update user details.";
            }
            return RedirectToAction(nameof(ManageUsers));
        }

        // ----------------------------------------------------------------------
        // POST Change User Role
        // ----------------------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeUserRole(string userId, string newRole)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || string.IsNullOrEmpty(newRole))
            {
                TempData["ErrorMessage"] = "User not found or role not specified.";
                return RedirectToAction(nameof(ManageUsers));
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Contains("Admin"))
            {
                var admins = await _userManager.GetUsersInRoleAsync("Admin");
                if (admins.Count <= 1 && currentRoles.First() == "Admin")
                {
                    TempData["ErrorMessage"] = "Cannot change the role of the only administrator.";
                    return RedirectToAction(nameof(ManageUsers));
                }
            }

            var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded)
            {
                TempData["ErrorMessage"] = "Failed to remove user from current roles.";
                return RedirectToAction(nameof(ManageUsers));
            }

            var addResult = await _userManager.AddToRoleAsync(user, newRole);
            if (addResult.Succeeded)
            {
                TempData["SuccessMessage"] = "User role updated successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to add user to the new role.";
            }
            return RedirectToAction(nameof(ManageUsers));
        }

        #endregion

        #region Event Management

        // ----------------------------------------------------------------------
        // Manage Events (List)
        // ----------------------------------------------------------------------
        public async Task<IActionResult> ManageEvents(string status = "all")
        {
            var query = _context.Events.Include(e => e.Organizer).AsQueryable();
            if (status != "all")
            {
                query = query.Where(e => e.Status.ToLower() == status.ToLower());
            }
            var events = await query.OrderByDescending(e => e.CreatedAt).ToListAsync();
            ViewBag.CurrentFilter = status;
            return View(events);
        }

        // ----------------------------------------------------------------------
        // Approve/Reject Event
        // ----------------------------------------------------------------------
        [HttpPost]
        public async Task<IActionResult> ApproveEvent(int id)
        {
            var eventToApprove = await _context.Events.FindAsync(id);
            if (eventToApprove == null) return NotFound();
            eventToApprove.Status = "Active";
            eventToApprove.IsActive = true;
            eventToApprove.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Event approved successfully!";
            return RedirectToAction(nameof(ManageEvents));
        }

        [HttpPost]
        public async Task<IActionResult> RejectEvent(int id, string reason)
        {
            var eventToReject = await _context.Events.FindAsync(id);
            if (eventToReject == null) return NotFound();
            eventToReject.Status = "Cancelled";
            eventToReject.IsActive = false;
            eventToReject.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Event rejected.";
            return RedirectToAction(nameof(ManageEvents));
        }

        // ----------------------------------------------------------------------
        // Edit Event (GET)
        // ----------------------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> EditEvent(int id)
        {
            var eventToEdit = await _context.Events.FindAsync(id);
            if (eventToEdit == null) return NotFound();
            ViewBag.Venues = new SelectList(await _context.Venues.ToListAsync(), "VenueName", "VenueName", eventToEdit.VenueName);
            return View(eventToEdit);
        }

        // ----------------------------------------------------------------------
        // Edit Event (POST)
        // ----------------------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEvent(int id, Event eventModel)
        {
            if (id != eventModel.Id) return NotFound();

            ModelState.Remove("Organizer");

            if (ModelState.IsValid)
            {
                try
                {
                    var eventToUpdate = await _context.Events.FindAsync(id);
                    if (eventToUpdate == null) return NotFound();

                    eventToUpdate.Title = eventModel.Title;
                    eventToUpdate.Description = eventModel.Description;
                    eventToUpdate.Category = eventModel.Category;
                    eventToUpdate.StartDate = eventModel.StartDate;
                    eventToUpdate.EndDate = eventModel.EndDate;
                    eventToUpdate.TicketPrice = eventModel.TicketPrice;
                    eventToUpdate.AvailableTickets = eventModel.AvailableTickets;
                    eventToUpdate.VenueName = eventModel.VenueName;
                    eventToUpdate.Location = eventModel.Location;
                    eventToUpdate.Status = eventModel.Status;
                    eventToUpdate.IsActive = eventModel.IsActive;
                    eventToUpdate.UpdatedAt = DateTime.UtcNow;

                    _context.Update(eventToUpdate);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Event updated successfully!";
                    return RedirectToAction(nameof(ManageEvents));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Events.Any(e => e.Id == eventModel.Id)) return NotFound();
                    else throw;
                }
            }
            ViewBag.Venues = new SelectList(await _context.Venues.ToListAsync(), "VenueName", "VenueName", eventModel.VenueName);
            return View(eventModel);
        }

        // ----------------------------------------------------------------------
        // Delete Event
        // ----------------------------------------------------------------------
        [HttpPost]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var eventToDelete = await _context.Events.FindAsync(id);
            if (eventToDelete == null) return NotFound();
            _context.Events.Remove(eventToDelete);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Event deleted successfully!";
            return RedirectToAction(nameof(ManageEvents));
        }

        #endregion

        #region Other Management sections...
        // ... (ManageVenues, ManageBookings, Reports, etc. would go here) ...
        public async Task<IActionResult> ManageVenues()
        {
            var venues = await _context.Venues.ToListAsync();
            return View(venues);
        }

        public async Task<IActionResult> ManageBookings()
        {
            var bookings = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Event)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();
            return View(bookings);
        }

        public IActionResult Reports()
        {
            return View();
        }

        public IActionResult SystemSettings()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateVenue(Venue venue)
        {
            if (ModelState.IsValid)
            {
                _context.Venues.Add(venue);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Venue created successfully!";
                return RedirectToAction(nameof(ManageVenues));
            }
            return View(venue);
        }

        public IActionResult CreateVenue()
        {
            return View();
        }

        public async Task<IActionResult> EditVenue(int id)
        {
            var venue = await _context.Venues.FindAsync(id);
            if (venue == null)
            {
                return NotFound();
            }
            return View(venue);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditVenue(int id, Venue venue)
        {
            if (id != venue.Id)
            {
                return NotFound();
            }
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(venue);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Venue updated successfully!";
                    return RedirectToAction(nameof(ManageVenues));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Venues.Any(v => v.Id == id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return View(venue);
        }

        private bool VenueExists(int id)
        {
            return _context.Venues.Any(e => e.Id == id);
        }

        #endregion
    }
}

