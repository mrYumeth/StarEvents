namespace StarEvents.Models
{
    public class ErrorViewModel
    {
        //A model for displaying error details.
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}


