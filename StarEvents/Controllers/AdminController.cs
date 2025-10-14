using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StarEvents.Data;
using StarEvents.Models;
using StarEvents.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace StarEvents.Controllers
/// Manages all administrative functionalities for the StarEvents application.

{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        //The database context for data access.

        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IWebHostEnvironment _webHostEnvironment; 

        public AdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IWebHostEnvironment webHostEnvironment) 
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _webHostEnvironment = webHostEnvironment; 
        }

        // Dashboard
        public async Task<IActionResult> Dashboard()
        {
            // Statistics Cards 
            var today = DateTime.Now;
            var startOfCurrentMonth = new DateTime(today.Year, today.Month, 1);
            var startOfPreviousMonth = startOfCurrentMonth.AddMonths(-1);
            var endOfPreviousMonth = startOfCurrentMonth.AddDays(-1);

            // Fetches the most recent activities to display on the dashboard
            ViewBag.TotalUsers = await _userManager.Users.CountAsync();
            ViewBag.NewUsersThisMonth = await _userManager.Users.CountAsync(u => u.CreatedAt >= startOfCurrentMonth);
            ViewBag.TotalEvents = await _context.Events.CountAsync();
            ViewBag.ActiveEvents = await _context.Events.CountAsync(e => e.IsActive && e.Status == "Active");
            ViewBag.TotalBookings = await _context.Bookings.CountAsync();
            ViewBag.BookingsThisMonth = await _context.Bookings.CountAsync(b => b.BookingDate >= startOfCurrentMonth);

            var confirmedBookings = _context.Bookings.Where(b => b.Status == "Confirmed" || b.Status == "Completed");
            var totalRevenue = await confirmedBookings.SumAsync(b => b.TotalAmount);
            ViewBag.TotalRevenue = totalRevenue.ToString("N0");

            // DYNAMIC REVENUE GROWTH CALCULATION 
            var currentMonthRevenue = await confirmedBookings
                .Where(b => b.BookingDate >= startOfCurrentMonth)
                .SumAsync(b => b.TotalAmount);

            var previousMonthRevenue = await confirmedBookings
                .Where(b => b.BookingDate >= startOfPreviousMonth && b.BookingDate <= endOfPreviousMonth)
                .SumAsync(b => b.TotalAmount);

            if (previousMonthRevenue > 0)
            {
                var growthPercentage = ((currentMonthRevenue - previousMonthRevenue) / previousMonthRevenue) * 100;
                ViewBag.RevenueGrowth = growthPercentage.ToString("F1"); 
            }
            else if (currentMonthRevenue > 0)
            {
                ViewBag.RevenueGrowth = "100.0"; 
            }
            else
            {
                ViewBag.RevenueGrowth = "0.0"; 
            }

            // System Alerts 
            ViewBag.PendingApprovals = await _context.Events.CountAsync(e => e.Status == "Draft");
            ViewBag.LowStockEventsCount = await _context.Events.CountAsync(e => e.IsActive && e.AvailableTickets > 0 && e.AvailableTickets < 10);
            ViewBag.LastBackupDays = 2;

            // Recent Activity Feed 
            var recentUsers = await _context.Users
                .OrderByDescending(u => u.CreatedAt)
                .Take(5)
                .Select(u => new DashboardActivityViewModel
                {
                    Description = $"New user registered: {u.Email}",
                    Timestamp = u.CreatedAt,
                    ActivityType = "User"
                }).ToListAsync();

            var recentBookings = await _context.Bookings
                .Include(b => b.Event)
                .OrderByDescending(b => b.BookingDate)
                .Take(5)
                .Select(b => new DashboardActivityViewModel
                {
                    Description = $"New booking for '{b.Event.Title}'",
                    Timestamp = b.BookingDate,
                    ActivityType = "Booking"
                }).ToListAsync();

            var recentEvents = await _context.Events
                .OrderByDescending(e => e.CreatedAt)
                .Take(5)
                .Select(e => new DashboardActivityViewModel
                {
                    Description = $"Event '{e.Title}' was created",
                    Timestamp = e.CreatedAt,
                    ActivityType = "Event"
                }).ToListAsync();

            var allActivities = recentUsers.Concat(recentBookings).Concat(recentEvents)
                                           .OrderByDescending(a => a.Timestamp)
                                           .Take(5)
                                           .ToList();

            ViewBag.RecentActivities = allActivities;


            // Revenue Chart Data 
            var monthlyRevenueData = await _context.Bookings
                .Where(b => b.BookingDate.Year == DateTime.Now.Year && (b.Status == "Confirmed" || b.Status == "Completed"))
                .GroupBy(b => b.BookingDate.Month)
                .Select(g => new {
                    Month = g.Key,
                    Revenue = g.Sum(b => b.TotalAmount)
                })
                .ToDictionaryAsync(x => x.Month, x => x.Revenue);

            var chartLabels = new List<string>();
            var chartData = new List<decimal>();

            for (int i = 1; i <= 12; i++)
            {
                chartLabels.Add(new DateTime(DateTime.Now.Year, i, 1).ToString("MMM"));
                chartData.Add(monthlyRevenueData.ContainsKey(i) ? monthlyRevenueData[i] : 0);
            }

            ViewBag.ChartLabels = JsonSerializer.Serialize(chartLabels);
            ViewBag.ChartData = JsonSerializer.Serialize(chartData);


            return View();
        }

        #region User Management

        // Manage Users (List)
        public async Task<IActionResult> ManageUsers()
        {
            var users = await _userManager.Users.ToListAsync();
            var usersWithRoles = new List<object>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                usersWithRoles.Add(new { User = user, Roles = roles });
            }

            //  Pass the list of all roles to the view for the "Add User" dropdown.
            ViewBag.AllRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();

            return View(usersWithRoles);
        }

        // POST: Admin - CreateUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(CreateUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if a user with this email already exists
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    TempData["ErrorMessage"] = "A user with this email already exists.";
                    return RedirectToAction(nameof(ManageUsers));
                }

                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    EmailConfirmed = true 
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Add the user to the selected role
                    await _userManager.AddToRoleAsync(user, model.Role);
                    TempData["SuccessMessage"] = "User created successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to create user. Please check the details and try again.";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Invalid data submitted. Please correct the errors and try again.";
            }

            return RedirectToAction(nameof(ManageUsers));
        }

        // GET User Details (for Modal)
        [HttpGet]
        public async Task<IActionResult> GetUserDetails(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            return PartialView("_UserDetailsPartial", user);
        }

        // POST Update User Details
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUser(ApplicationUser model)
        {
            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return NotFound();

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Email = model.Email;
            user.UserName = model.Email; 
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

        // POST: Admin - DeleteUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(ManageUsers));
            }

            // Prevent deleting the last admin 
            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                var admins = await _userManager.GetUsersInRoleAsync("Admin");
                if (admins.Count <= 1)
                {
                    TempData["ErrorMessage"] = "Cannot delete the only administrator account.";
                    return RedirectToAction(nameof(ManageUsers));
                }
            }

            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "User deleted successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Error deleting user.";
            }

            return RedirectToAction(nameof(ManageUsers));
        }

        // POST: Admin - ResetUserPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetUserPassword(string userId, string newPassword)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(newPassword))
            {
                TempData["ErrorMessage"] = "User ID and new password are required.";
                return RedirectToAction(nameof(ManageUsers));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(ManageUsers));
            }

            // Generate a password reset token and use it to reset the password
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"Password for {user.Email} has been reset successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to reset password.";
            }

            return RedirectToAction(nameof(ManageUsers));
        }

        // POST Change User Role
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

        // Manage Events (List)
        // GET: Admin - ManageEvents
        public async Task<IActionResult> ManageEvents(string status = "all")
        {
            var query = _context.Events.Include(e => e.Organizer).AsQueryable();
            if (status != "all" && !string.IsNullOrEmpty(status))
            {
                query = query.Where(e => e.Status.ToLower() == status.ToLower());
            }
            var events = await query.OrderByDescending(e => e.CreatedAt).ToListAsync();
            ViewBag.CurrentFilter = status;
            return View(events);
        }

        // CREATE EVENT ACTIONS 
        // GET: Admin - CreateEvent
        [HttpGet]
        public IActionResult CreateEvent()
        {
            return View();
        }

        // POST: Admin - CreateEvent
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEvent(Event eventModel)
        {
            ModelState.Remove("OrganizerId");
            ModelState.Remove("Organizer");

            if (ModelState.IsValid)
            {
                // Handle Image Upload
                if (eventModel.ImageFile != null)
                {
                    string wwwRootPath = _webHostEnvironment.WebRootPath;
                    string fileName = Path.GetFileNameWithoutExtension(eventModel.ImageFile.FileName);
                    string extension = Path.GetExtension(eventModel.ImageFile.FileName);
                    fileName = fileName + DateTime.Now.ToString("yymmssfff") + extension;
                    eventModel.ImageUrl = "/images/events/" + fileName;
                    string path = Path.Combine(wwwRootPath, "images/events", fileName);
                    using (var fileStream = new FileStream(path, FileMode.Create))
                    {
                        await eventModel.ImageFile.CopyToAsync(fileStream);
                    }
                }

                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Challenge();
                }

                // Now you can assign the OrganizerId
                eventModel.OrganizerId = currentUser.Id;
                eventModel.CreatedAt = DateTime.UtcNow;
                eventModel.UpdatedAt = DateTime.UtcNow;
                eventModel.Status = "Active";
                eventModel.IsActive = true;

                _context.Add(eventModel);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Event created successfully!";
                return RedirectToAction(nameof(ManageEvents));
            }

            return View(eventModel);
        }

        // EDIT EVENT ACTIONS 
        // GET: Admin - EditEvent
        [HttpGet]
        public async Task<IActionResult> EditEvent(int id)
        {
            var eventToEdit = await _context.Events.FindAsync(id);
            if (eventToEdit == null) return NotFound();
            return View(eventToEdit);
        }

        // POST: Admin - EditEvent
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

                    // Handle optional new image upload
                    if (eventModel.ImageFile != null)
                    {
                        string wwwRootPath = _webHostEnvironment.WebRootPath;
                        string fileName = Path.GetFileNameWithoutExtension(eventModel.ImageFile.FileName);
                        string extension = Path.GetExtension(eventModel.ImageFile.FileName);
                        fileName = fileName + DateTime.Now.ToString("yymmssfff") + extension;
                        eventToUpdate.ImageUrl = "/images/events/" + fileName; 
                        string path = Path.Combine(wwwRootPath, "images/events", fileName);
                        using (var fileStream = new FileStream(path, FileMode.Create))
                        {
                            await eventModel.ImageFile.CopyToAsync(fileStream);
                        }
                    }

                    // Update properties
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
            return View(eventModel);
        }

        // POST: Admin - DeleteEvent
        [HttpPost]
        [ValidateAntiForgeryToken] 
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var eventToDelete = await _context.Events.FindAsync(id);
            if (eventToDelete == null)
            {
                return NotFound();
            }

            _context.Events.Remove(eventToDelete);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Event deleted successfully!";
            return RedirectToAction(nameof(ManageEvents));
        }
        #endregion
        #region Other Management sections...


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

            // --- 2. NEW: User Report Data ---
            var allUsers = await _userManager.Users.ToListAsync();
            var userReportData = new
            {
                TotalUsers = allUsers.Count,
                AdminCount = (await _userManager.GetUsersInRoleAsync("Admin")).Count,
                OrganizerCount = (await _userManager.GetUsersInRoleAsync("Organizer")).Count,
                CustomerCount = (await _userManager.GetUsersInRoleAsync("Customer")).Count,
                RecentUsers = allUsers.OrderByDescending(u => u.CreatedAt).Take(5).ToList()
            };
            ViewBag.UserReport = userReportData;


            // --- 3. NEW: Event Report Data ---
            var allEvents = await _context.Events.ToListAsync();
            var eventReportData = new
            {
                TotalEvents = allEvents.Count,
                ActiveEvents = allEvents.Count(e => e.Status == "Active"),
                DraftEvents = allEvents.Count(e => e.Status == "Draft"),
                CancelledEvents = allEvents.Count(e => e.Status == "Cancelled"),
                // An event is considered 'Completed' if its end date is in the past.
                CompletedEvents = allEvents.Count(e => e.EndDate < DateTime.Now)
            };
            ViewBag.EventReport = eventReportData;


            return View();
        }
        #endregion


        // System Settings (GET) 
        [HttpGet]
        public async Task<IActionResult> SystemSettings()
        {
            var settings = await _context.SystemSettings.FirstOrDefaultAsync();

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

        // System Settings (POST) 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSettings(SystemSetting model)
        {
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

        // Manage Bookings - UPDATED
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
                // Add to include all bookings on that day
                query = query.Where(b => b.BookingDate < toDate.Value.AddDays(1));
            }

            var bookings = await query.OrderByDescending(b => b.BookingDate).ToListAsync();

            return View(bookings);
        }
        

    }
}


