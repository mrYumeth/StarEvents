using System.ComponentModel.DataAnnotations;

namespace StarEvents.ViewModels
{
    public class ProfileViewModel
    {
        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }


        // Email will be shown as read-only.
        public string Email { get; set; }
    }
}