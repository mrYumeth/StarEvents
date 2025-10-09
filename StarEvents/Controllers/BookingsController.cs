using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarEvents.Data;
using StarEvents.Models;
using StarEvents.Models.Payments;
using System;
using System.Security.Claims;
using System.Text.Json; // Required for JSON serialization to use TempData safely
using System.Threading.Tasks;

namespace StarEvents.Controllers
{
    // A simple, internal class to hold temporary booking calculation data 
    // passed between the POST (Book) and GET (Payment) actions via TempData.
    public class BookingData
    {
        public int EventId { get; set; }
        public string EventTitle { get; set; } = string.Empty;
        public int TicketQuantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string CustomerId { get; set; } = string.Empty;
    }

    // Restricts access to users with the "Customer" role
    [Authorize(Roles = "Customer")]
    public class BookingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ----------------------------------------------------------------------
        // 1. Book (GET) - Displays the initial booking form for a specific event.
        // ----------------------------------------------------------------------
        // GET: /Bookings/Book/{id}
        public async Task<IActionResult> Book(int id)
        {
            // Fetch event details including the venue
            var eventEntity = await _context.Events
                .Include(e => e.Venue)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (eventEntity == null)
            {
                return NotFound();
            }

            return View(eventEntity);
        }

        // ----------------------------------------------------------------------
        // 2. Book (POST) - Calculates totals and redirects to payment.
        // ----------------------------------------------------------------------
        // POST: /Bookings/Book
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(int eventId, int ticketQuantity, string? promoCode)
        {
            // Basic validation
            if (ticketQuantity <= 0)
            {
                TempData["ErrorMessage"] = "Ticket quantity must be at least 1.";
                return RedirectToAction(nameof(Book), new { id = eventId });
            }

            var eventEntity = await _context.Events.FirstOrDefaultAsync(e => e.Id == eventId);
            if (eventEntity == null)
            {
                TempData["ErrorMessage"] = "Event not found.";
                return RedirectToAction("Index", "Home");
            }

            // 1. Calculate base totals
            decimal unitPrice = eventEntity.TicketPrice;
            decimal subTotal = unitPrice * ticketQuantity;
            decimal discountAmount = 0;

            // 2. Apply dummy promo code logic
            if (!string.IsNullOrEmpty(promoCode) && promoCode.ToUpper() == "STAREVENTS")
            {
                // Apply a fixed 10% discount
                discountAmount = subTotal * 0.10m;
                TempData["SuccessMessage"] = "Promo code applied! 10% discount added.";
            }

            decimal totalAmount = subTotal - discountAmount;

            // Get current customer ID
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 3. Prepare data for the Payment action
            var bookingData = new BookingData
            {
                EventId = eventId,
                EventTitle = eventEntity.Title,
                TicketQuantity = ticketQuantity,
                UnitPrice = unitPrice,
                DiscountAmount = discountAmount,
                TotalAmount = totalAmount,
                CustomerId = currentUserId
            };

            // Store data safely using JSON serialization in TempData
            TempData["BookingData"] = JsonSerializer.Serialize(bookingData);

            // 4. Redirect to the Payment action
            return RedirectToAction(nameof(Payment));
        }

        // ----------------------------------------------------------------------
        // 3. Payment (GET) - Displays the payment summary before transaction.
        // ----------------------------------------------------------------------
        // GET: /Bookings/Payment
        public async Task<IActionResult> Payment()
        {
            // Retrieve data from TempData
            if (TempData["BookingData"] is string bookingDataJson)
            {
                var bookingData = JsonSerializer.Deserialize<BookingData>(bookingDataJson);

                // Re-store TempData so it persists across redirects during the payment process
                TempData.Keep("BookingData");

                // Fetch event details needed for the view (like venue name)
                var eventEntity = await _context.Events
                    .Include(e => e.Venue)
                    .FirstOrDefaultAsync(e => e.Id == bookingData.EventId);

                ViewBag.BookingData = bookingData;
                ViewBag.Event = eventEntity;
                return View();
            }

            TempData["ErrorMessage"] = "Booking session expired or incomplete. Please book again.";
            return RedirectToAction("Index", "Home");
        }

        // ----------------------------------------------------------------------
        // 4. ProcessPayment (POST) - Saves the payment and booking records.
        // ----------------------------------------------------------------------
        // POST: /Bookings/ProcessPayment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPayment(string paymentMethod, string? cardLastFour)
        {
            // 1. Retrieve Booking Data from TempData
            if (TempData["BookingData"] is not string bookingDataJson)
            {
                TempData["ErrorMessage"] = "Booking session expired. Please start the booking process again.";
                return RedirectToAction("Index", "Home");
            }

            var bookingData = JsonSerializer.Deserialize<BookingData>(bookingDataJson);

            // We don't need to keep TempData anymore, as we are completing the transaction.

            // 2. Start Transaction Scope (using Entity Framework SaveChanges is often enough)
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // --- A. Create CustomerPayment Record ---
                var payment = new CustomerPayment // Correct model name
                {
                    Amount = bookingData.TotalAmount,
                    // In a real system, this comes from the gateway
                    TransactionId = "TXN-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                    PaymentMethod = paymentMethod,
                    PaymentDate = DateTime.UtcNow,
                    Status = "Completed",
                    CustomerId = bookingData.CustomerId,
                    CardLastFour = cardLastFour
                };
                // FIXED: Using the correct DbSet name 'Payments' as defined in ApplicationDbContext
                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                // --- B. Create Booking Record ---
                var booking = new Booking
                {
                    CustomerId = bookingData.CustomerId,
                    EventId = bookingData.EventId,
                    PaymentId = payment.Id, // Link Payment to Booking
                    TicketQuantity = bookingData.TicketQuantity,
                    UnitPrice = bookingData.UnitPrice,
                    DiscountAmount = bookingData.DiscountAmount,
                    TotalAmount = bookingData.TotalAmount,
                    Status = "Confirmed",
                    BookingDate = DateTime.UtcNow,
                    // You might generate a real QR code URL here later
                    QRCodeUrl = "/qrcodes/default.png"
                };
                _context.Bookings.Add(booking);

                // --- C. Update Available Tickets (optional but recommended) ---
                var eventToUpdate = await _context.Events.FirstOrDefaultAsync(e => e.Id == bookingData.EventId);
                if (eventToUpdate != null && eventToUpdate.AvailableTickets.HasValue)
                {
                    eventToUpdate.AvailableTickets -= bookingData.TicketQuantity;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = $"Booking #{booking.Id} confirmed! Payment successful.";
                // Redirect to a confirmation or my-bookings page
                return RedirectToAction("Confirmation", new { bookingId = booking.Id });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "Payment failed due to a system error. Please try again. " + ex.Message;
                // Re-store TempData so the user can re-try the payment immediately
                TempData.Keep("BookingData");
                return RedirectToAction(nameof(Payment));
            }
        }

        // ----------------------------------------------------------------------
        // 5. Confirmation (GET) - Placeholder for booking confirmation page.
        // ----------------------------------------------------------------------
        // GET: /Bookings/Confirmation/5
        [HttpGet]
        public async Task<IActionResult> Confirmation(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Event)
                .ThenInclude(e => e.Venue)
                .Include(b => b.Payment) // Include payment details
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
            {
                return NotFound();
            }

            return View(booking);
        }
    }
}
