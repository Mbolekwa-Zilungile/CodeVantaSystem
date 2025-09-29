using Code_Vanta_Systems.Data;
using Code_Vanta_Systems.Helpers;
using Code_Vanta_Systems.Models;
using Code_Vanta_Systems.Models.Enums;
using Code_Vanta_Systems.Services;
using Code_Vanta_Systems.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Code_Vanta_Systems.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly DropdownService _dropdownService;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            DropdownService dropdownService)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _dropdownService = dropdownService;
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModels model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "User with this email does not exist. Please register first.");
                return View(model);
            }

            var roles = await _userManager.GetRolesAsync(user);
            Console.WriteLine($"🔍 Roles for {user.Email}: {string.Join(", ", roles)}");

            var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, false);
            if (result.Succeeded)
            {
                if (await _userManager.IsInRoleAsync(user, Roles.Admin))
                    return RedirectToAction("Employees", "Admin");

                if (await _userManager.IsInRoleAsync(user, Roles.Customer))
                    return RedirectToAction("Index", "CustomerDashboard"); // ✅ Fixed

                if (await _userManager.IsInRoleAsync(user, Roles.CustomerLiaison))
                    return RedirectToAction("Index", "CustomerLiaison");

                if (await _userManager.IsInRoleAsync(user, Roles.InventoryLiaison))
                    return RedirectToAction("Index", "InventoryLiaison");

                if (await _userManager.IsInRoleAsync(user, Roles.ServiceTechnician))
                    return RedirectToAction("Index", "ServiceTechnician");

                if (await _userManager.IsInRoleAsync(user, Roles.FaultTechnician))
                    return RedirectToAction("Index", "FaultTechnician");

                if (await _userManager.IsInRoleAsync(user, Roles.MaintenanceTechnician))
                    return RedirectToAction("Index", "MaintenanceTechnician");

                if (await _userManager.IsInRoleAsync(user, Roles.PurchasingManager))
                    return RedirectToAction("Index", "PurchasingManager");

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt");
            return View(model);
        }

        [HttpGet]

        public IActionResult Register()
        {
            var model = new RegisterViewModel
            {
                RegionOptions = _dropdownService.GetRegionOptions(),
                LocationOptions = new List<SelectListItem>()
            };

            return View(model);
        }


        [HttpPost]

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.RegionOptions = _dropdownService.GetRegionOptions();
                model.LocationOptions = new List<SelectListItem>();
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.Name,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, Roles.Customer);

                // ✅ Convert SelectedRegion (int) to Region enum
                var regionEnum = (Region)model.SelectedRegion;

                // ✅ Validate selected location belongs to selected region
                var location = await _context.Locations
                    .FirstOrDefaultAsync(l => l.Id == model.SelectedLocationId && l.Province == regionEnum);

                if (location == null)
                {
                    ModelState.AddModelError("", "Selected location is invalid.");
                    model.RegionOptions = _dropdownService.GetRegionOptions();
                    model.LocationOptions = new List<SelectListItem>();
                    return View(model);
                }

                var customer = new Customer
                {
                    ContactName = model.Name,
                    BusinessName = model.BusinessName,
                    BusinessType = model.BusinessType,
                    Phone = model.Phone,
                    Email = model.Email,
                    PhysicalAddress = model.PhysicalAddress,
                    LocationId = location.Id,
                    UserId = user.Id
                };

                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();

                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "CustomerDashboard");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            model.RegionOptions = _dropdownService.GetRegionOptions();
            model.LocationOptions = new List<SelectListItem>();
            return View(model);
        }

        [HttpGet]
        public JsonResult GetLocationsByRegion(int region)
        {
            var regionEnum = (Region)region; // Correctly casts the integer (e.g., 1) to the Region enum

            var locations = _context.Locations
                .Where(l => l.Province == regionEnum) // Correctly filters by the enum value
                .Select(l => new SelectListItem
                {
                    Value = l.Id.ToString(),
                    Text = l.Description
                })
                .ToList();

            return Json(locations);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}
