using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StarEvents.Models;
using System.Threading.Tasks;

namespace StarEvents.Controllers
{
    public class HomeController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public HomeController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public async Task<IActionResult> Index()
        {
            // Check if the user is signed in
            if (_signInManager.IsSignedIn(User))
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    // If they are signed in, check their role and redirect to the correct dashboard
                    if (await _userManager.IsInRoleAsync(user, "Admin"))
                    {
                        return RedirectToAction("Dashboard", "Admin"); // Assuming Admin dashboard is at /Admin/Dashboard
                    }
                    if (await _userManager.IsInRoleAsync(user, "Organizer"))
                    {
                        return RedirectToAction("Dashboard", "Organizer"); // Assuming Organizer dashboard is at /Organizer/Dashboard
                    }
                    if (await _userManager.IsInRoleAsync(user, "Customer"))
                    {
                        return RedirectToAction("Dashboard", "Customer"); // Redirects to your /Customer/Dashboard
                    }
                }
            }

            // If the user is not signed in, show the public home page
            return View();
        }

        // You can keep your Privacy, Error, etc. actions here
        public IActionResult Privacy()
        {
            return View();
        }
    }
}