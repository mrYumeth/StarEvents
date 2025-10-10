using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StarEvents.Data;
using StarEvents.Models;
using StarEvents.Models.Payments;
using StarEvents.ViewModels;
using System;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace StarEvents.Controllers
{
    public class BookingData
    {
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public int TicketQuantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string CustomerId { get; set; } = string.Empty;

        public string EventDate { get; set; } = string.Empty;

        public string VenueName { get; set; } = string.Empty;

        public int PointsToEarn { get; set; }
    }

    [Authorize(Roles = "Customer")]
    public class BookingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Bookings/Book/{id}
        public async Task<IActionResult> Book(int id)
        {
            var eventEntity = await _context.Events
        .Include(e => e.Venue)
        .FirstOrDefaultAsync(e => e.Id == id && e.IsActive);

            if (eventEntity == null)
            {
                TempData["ErrorMessage"] = "Event not found.";
                return RedirectToAction("Index", "Home");
            }

            if (eventEntity == null)
            {
                TempData["ErrorMessage"] = "The event you're trying to book is not available.";
                return RedirectToAction("Index", "Events");
            }

            if (eventEntity.AvailableTickets <= 0)
            {
                TempData["ErrorMessage"] = "This event is sold out.";
                return RedirectToAction("Details", "Events", new { id = id });
            }

            var viewModel = new EventDetailsViewModel
            {
                Id = eventEntity.Id,
                Title = eventEntity.Title,
                Category = eventEntity.Category,
                DateDisplay = eventEntity.StartDate.ToString("ddd, MMM d, yyyy"),
                VenueCity = eventEntity.Venue.City,
                TicketPrice = $"LKR {eventEntity.TicketPrice:N2}",
                AvailableTickets = eventEntity.AvailableTickets ?? 0
            };

            return View(viewModel);
        }

        // POST: /Bookings/Book
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(int eventId, int quantity, string? promoCode)
        {
            if (quantity <= 0)
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
            
            // --- CRITICAL FIX: ADDED TICKET AVAILABILITY VALIDATION ---
            if (eventEntity.AvailableTickets < quantity)
            {
                TempData["ErrorMessage"] = $"Sorry, only {eventEntity.AvailableTickets} tickets are available for this event.";
                return RedirectToAction(nameof(Book), new { id = eventId });
            }

            decimal unitPrice = eventEntity.TicketPrice;
            decimal subTotal = unitPrice * quantity;
            decimal discountAmount = 0;

            if (!string.IsNullOrEmpty(promoCode) && promoCode.ToUpper() == "STAREVENTS")
            {
                discountAmount = subTotal * 0.10m;
                TempData["SuccessMessage"] = "Promo code applied! 10% discount added.";
            }

            decimal totalAmount = subTotal - discountAmount;
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);


            int pointsToEarn = (int)Math.Floor(totalAmount / 100); // 1 point per 100 LKR
            var bookingData = new BookingData
            {
                EventId = eventId,
                EventName = eventEntity.Title,
                EventDate = eventEntity.StartDate.ToString("ddd, MMM d, yyyy"),
                VenueName = eventEntity.Venue?.VenueName ?? "TBC", // <-- ADD THIS LINE
                TicketQuantity = quantity,
                UnitPrice = unitPrice,
                DiscountAmount = discountAmount,
                TotalAmount = totalAmount,
                CustomerId = currentUserId,
                PointsToEarn = pointsToEarn
            };

            TempData["BookingData"] = JsonSerializer.Serialize(bookingData);
            return RedirectToAction(nameof(Payment));
        }

        // GET: /Bookings/Payment
        public async Task<IActionResult> Payment()
        {
            if (TempData["BookingData"] is string bookingDataJson)
            {
                var bookingData = JsonSerializer.Deserialize<BookingData>(bookingDataJson);
                TempData.Keep("BookingData");

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

        // POST: /Bookings/ProcessPayment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPayment(string paymentMethod, string? cardLastFour)
        {
            if (TempData["BookingData"] is not string bookingDataJson)
            {
                TempData["ErrorMessage"] = "Booking session expired. Please start the booking process again.";
                return RedirectToAction("Index", "Home");
            }

            var bookingData = JsonSerializer.Deserialize<BookingData>(bookingDataJson);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var payment = new CustomerPayment
                {
                    Amount = bookingData.TotalAmount,
                    TransactionId = "TXN-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                    PaymentMethod = paymentMethod,
                    PaymentDate = DateTime.UtcNow,
                    Status = "Completed",
                    CustomerId = bookingData.CustomerId,
                    CardLastFour = cardLastFour
                };
                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                var booking = new Booking
                {
                    CustomerId = bookingData.CustomerId,
                    EventId = bookingData.EventId,
                    PaymentId = payment.Id,
                    TicketQuantity = bookingData.TicketQuantity,
                    UnitPrice = bookingData.UnitPrice,
                    DiscountAmount = bookingData.DiscountAmount,
                    TotalAmount = bookingData.TotalAmount,
                    Status = "Confirmed",
                    BookingDate = DateTime.UtcNow,
                    QRCodeUrl = "/qrcodes/default.png",
                    PointsEarned = bookingData.PointsToEarn
                };
                _context.Bookings.Add(booking);

                var eventToUpdate = await _context.Events.FirstOrDefaultAsync(e => e.Id == bookingData.EventId);
                if (eventToUpdate != null && eventToUpdate.AvailableTickets.HasValue)
                {
                    eventToUpdate.AvailableTickets -= bookingData.TicketQuantity;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = $"Booking #{booking.Id} confirmed! Payment successful.";
                return RedirectToAction("Confirmation", new { bookingId = booking.Id });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "Payment failed due to a system error. Please try again. " + ex.Message;
                TempData.Keep("BookingData");
                return RedirectToAction(nameof(Payment));
            }
        }

        // GET: /Bookings/Confirmation/5
        [HttpGet]
        public async Task<IActionResult> Confirmation(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Event)
                .ThenInclude(e => e.Venue)
                .Include(b => b.Payment)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
            {
                return NotFound();
            }

            return View(booking);
        }
    }
}