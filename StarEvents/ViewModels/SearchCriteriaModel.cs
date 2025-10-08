using System;
using System.ComponentModel.DataAnnotations;

namespace StarEvents.ViewModels
{
    // This model is used to receive the form data from Search.cshtml.
    public class SearchCriteriaModel
    {
        // Search by Category (string input)
        public string Category { get; set; }

        // Search by Date (using nullable DateTime for optional input)
        public DateTime? Date { get; set; }

        // Search by Location (string input - will match the Venue/Location name)
        public string Location { get; set; }
    }
}