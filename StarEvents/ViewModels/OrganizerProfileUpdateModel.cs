using System.ComponentModel.DataAnnotations;

namespace StarEvents.ViewModels
{
    /// <summary>
    /// A dedicated model for handling organizer profile updates.
    /// It only contains the properties that are allowed to be changed.
    /// </summary>
    public class OrganizerProfileUpdateModel
    {
        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Phone]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }
    }
}