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

        public async Task<IActionResult> ManageVenues()
        {
            var venues = await _context.Venues.ToListAsync();
            return View(venues);
        }


        // ----------------------------------------------------------------------
        // Reports - Generate System Reports - UPDATED
        // ----------------------------------------------------------------------
        public async Task<IActionResult> Reports()
        {
            var confirmedBookings = _context.Bookings.Where(b => b.Status == "Confirmed" || b.Status == "Completed");

            // --- Summary Card Data ---
            ViewBag.TotalSales = (await confirmedBookings.SumAsync(b => b.TotalAmount)).ToString("N0");
            ViewBag.TotalTicketsSold = await confirmedBookings.SumAsync(b => b.TicketQuantity);
            var totalSalesDecimal = await confirmedBookings.SumAsync(b => b.TotalAmount);
            var totalTicketsDecimal = await confirmedBookings.SumAsync(b => b.TicketQuantity);
            ViewBag.AverageBookingValue = totalTicketsDecimal > 0 ? (totalSalesDecimal / totalTicketsDecimal).ToString("N2") : "0.00";
            ViewBag.TotalCustomers = (await _userManager.GetUsersInRoleAsync("Customer")).Count;
            ViewBag.TotalOrganizers = (await _userManager.GetUsersInRoleAsync("Organizer")).Count;

            // --- FIX: Add a new ViewBag property for the true total user count ---
            ViewBag.TotalUsers = await _userManager.Users.CountAsync();


            // --- Top 5 Events by Revenue ---
            ViewBag.TopEvents = await _context.Events
                .Include(e => e.Bookings) // Ensure bookings are loaded
                .OrderByDescending(e => e.Bookings.Where(b => b.Status == "Confirmed" || b.Status == "Completed").Sum(b => b.TotalAmount))
                .Take(5)
                .Select(e => new {
                    EventName = e.Title,
                    TicketsSold = e.Bookings.Where(b => b.Status == "Confirmed" || b.Status == "Completed").Sum(b => b.TicketQuantity),
                    TotalRevenue = e.Bookings.Where(b => b.Status == "Confirmed" || b.Status == "Completed").Sum(b => b.TotalAmount)
                })
                .ToListAsync();

            // --- Monthly Revenue for Chart ---
            var monthlyRevenueData = await _context.Bookings
                .Where(b => b.BookingDate.Year == DateTime.Now.Year && (b.Status == "Confirmed" || b.Status == "Completed"))
                .GroupBy(b => b.BookingDate.Month)
                .Select(g => new { monthInt = g.Key, revenue = g.Sum(b => b.TotalAmount) })
                .OrderBy(x => x.monthInt)
                .ToListAsync();

            var monthlyRevenueForChart = new List<object>();
            for (int i = 1; i <= 12; i++)
            {
                var monthData = monthlyRevenueData.FirstOrDefault(m => m.monthInt == i);
                monthlyRevenueForChart.Add(new
                {
                    month = new DateTime(DateTime.Now.Year, i, 1).ToString("MMM"),
                    revenue = monthData?.revenue ?? 0
                });
            }
            ViewBag.MonthlyRevenue = monthlyRevenueForChart;

            // --- Sales by Category ---
            ViewBag.SalesByCategory = await _context.Bookings
                .Where(b => b.Status == "Confirmed" || b.Status == "Completed")
                .Include(b => b.Event)
                .GroupBy(b => b.Event.Category)
                .Select(g => new { category = g.Key ?? "Uncategorized", sales = g.Sum(b => b.TotalAmount) })
                .Where(g => g.sales > 0)
                .ToListAsync();

            // --- Booking Status Distribution ---
            ViewBag.BookingStatusDistribution = await _context.Bookings
                .GroupBy(b => b.Status)
                .Select(g => new { status = g.Key, count = g.Count() })
                .ToListAsync();


            return View();
        }
        #endregion


        // ----------------------------------------------------------------------
        // System Settings (GET) - UPDATED
        // ----------------------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> SystemSettings()
        {
            // There should only ever be one row of settings.
            // We use FirstOrDefault to get it, or null if the table is empty.
            var settings = await _context.SystemSettings.FirstOrDefaultAsync();

            // If no settings exist yet, create a new object with default values.
            if (settings == null)
            {
                settings = new SystemSetting
                {
                    SystemName = "StarEvents",
                    ContactEmail = "support@starevents.com",
                    MaxTicketsPerBooking = 10,
                    BookingCancellationHours = 48,
                    EnableQRCodeTickets = true,
                    AcceptCreditCards = true,
                    AcceptPayPal = true,
                    Currency = "LKR",
                    EnableLoyaltyProgram = true,
                    PointsPer100LKR = 1,
                    PointsExpiryDays = 365,
                    EmailOnBookingConfirmation = true,
                    EmailOnEventReminder = true,
                    RequireEmailVerification = true,
                    SessionTimeoutMinutes = 30,
                    PasswordMinLength = 6
                };
            }

            return View(settings);
        }

        // ----------------------------------------------------------------------
        // System Settings (POST) - NEW ACTION
        // ----------------------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSettings(SystemSetting model)
        {
            // This custom model binding is needed to correctly handle unchecked checkboxes,
            // which are not sent in the form data. We map form values to the model properties.
            var settingsToUpdate = await _context.SystemSettings.FirstOrDefaultAsync();

            if (settingsToUpdate == null)
            {
                // If settings don't exist, create a new record
                settingsToUpdate = new SystemSetting();
                _context.SystemSettings.Add(settingsToUpdate);
            }

            // Map values from the submitted form (model) to the database entity
            settingsToUpdate.SystemName = model.SystemName;
            settingsToUpdate.ContactEmail = model.ContactEmail;
            settingsToUpdate.SupportPhone = model.SupportPhone;
            settingsToUpdate.MaxTicketsPerBooking = model.MaxTicketsPerBooking;
            settingsToUpdate.BookingCancellationHours = model.BookingCancellationHours;
            settingsToUpdate.EnableQRCodeTickets = Request.Form.ContainsKey("EnableQRCodeTickets");
            settingsToUpdate.AcceptCreditCards = Request.Form.ContainsKey("AcceptCreditCards");
            settingsToUpdate.AcceptPayPal = Request.Form.ContainsKey("AcceptPayPal");
            settingsToUpdate.Currency = model.Currency;
            settingsToUpdate.EnableLoyaltyProgram = Request.Form.ContainsKey("EnableLoyaltyProgram");
            settingsToUpdate.PointsPer100LKR = model.PointsPer100LKR;
            settingsToUpdate.PointsExpiryDays = model.PointsExpiryDays;
            settingsToUpdate.EmailOnBookingConfirmation = Request.Form.ContainsKey("EmailOnBookingConfirmation");
            settingsToUpdate.EmailOnEventReminder = Request.Form.ContainsKey("EmailOnEventReminder");
            settingsToUpdate.EmailForPromotions = Request.Form.ContainsKey("EmailForPromotions");
            settingsToUpdate.RequireEmailVerification = Request.Form.ContainsKey("RequireEmailVerification");
            settingsToUpdate.SessionTimeoutMinutes = model.SessionTimeoutMinutes;
            settingsToUpdate.PasswordMinLength = model.PasswordMinLength;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "System settings updated successfully!";
            return RedirectToAction("Dashboard");
        }

        // ----------------------------------------------------------------------
        // Manage Bookings - UPDATED
        // ----------------------------------------------------------------------
        public async Task<IActionResult> ManageBookings(string status, string customerEmail, DateTime? fromDate, DateTime? toDate)
        {
            // Start with the base query and include all necessary related data
            var query = _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Event)
                .Include(b => b.Payment) 
                .AsQueryable();

            // Apply filters from the view
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(b => b.Status == status);
            }

            if (!string.IsNullOrEmpty(customerEmail))
            {
                query = query.Where(b => b.Customer.Email.Contains(customerEmail));
            }

            if (fromDate.HasValue)
            {
                query = query.Where(b => b.BookingDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                // Add 1 day to the 'toDate' to include all bookings on that day
                query = query.Where(b => b.BookingDate < toDate.Value.AddDays(1));
            }

            // Execute the final query
            var bookings = await query.OrderByDescending(b => b.BookingDate).ToListAsync();

            return View(bookings);
        }
    }
}

