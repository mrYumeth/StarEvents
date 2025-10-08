namespace StarEvents.ViewModels
{
    // This model is used to display event summaries on the search page.
    public class EventViewModel
    {
        public int Id { get; set; } // Used for linking to the Details page
        public string Title { get; set; }
        public string Category { get; set; }

        // We combine StartDate and EndDate into one displayable string for simplicity
        public string DateRange { get; set; }

        // This will come from a separate Venue model or be handled in the service layer
        public string LocationName { get; set; }

        // Placeholder for displaying ticket price information
        public string PriceDisplay { get; set; }
    }
}