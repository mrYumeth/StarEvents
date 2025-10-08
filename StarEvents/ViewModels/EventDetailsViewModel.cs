using System;
using System.ComponentModel.DataAnnotations;

namespace StarEvents.ViewModels
{
    public class EventDetailsViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public string OrganizerName { get; set; } // e.g., "Live Nation"

        // Date and Time Details
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        // This is a property to hold the nicely formatted date/time string
        public string DateDisplay { get; set; }

        // Venue Details
        public string VenueName { get; set; }
        public string VenueAddress { get; set; }
        public string VenueCity { get; set; }

        // Ticket Details (Mock/Placeholder for now, required for Task 5/6)
        public int AvailableTickets { get; set; }
        public string TicketPrice { get; set; }
    }
}