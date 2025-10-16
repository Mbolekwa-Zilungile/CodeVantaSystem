using Code_Vanta_Systems.Constants;
using Code_Vanta_Systems.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Code_Vanta_Systems.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(ILogger<HomeController> logger, UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            // Redirect logged-in users to their dashboard based on role
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    if (await _userManager.IsInRoleAsync(user, Roles.Admin))
                        return RedirectToAction("Employees", "Admin");

                    if (await _userManager.IsInRoleAsync(user, Roles.Customer))
                        return RedirectToAction("Index", "Customer");

                    if (await _userManager.IsInRoleAsync(user, Roles.CustomerLiaison))
                        return RedirectToAction("Index", "CustomerLiaison");

                    if (await _userManager.IsInRoleAsync(user, Roles.InventoryLiaison))
                        return RedirectToAction("Dashboard", "InventoryLiaison");

                    if (await _userManager.IsInRoleAsync(user, Roles.ServiceTechnician))
                        return RedirectToAction("Index", "ServiceTechnician");

                    if (await _userManager.IsInRoleAsync(user, Roles.FaultTechnician))
                        return RedirectToAction("Dashboard", "FaultTechnician");

                    if (await _userManager.IsInRoleAsync(user, Roles.MaintenanceTechnician))
                        return RedirectToAction("Index", "MaintenanceTechnician");

                    if (await _userManager.IsInRoleAsync(user, Roles.PurchasingManager))
                        return RedirectToAction("Index", "PurchasingManager");
                }
            }

            // If not logged in, show landing page
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult Services()
        {
            return View();
        }
    }
}
