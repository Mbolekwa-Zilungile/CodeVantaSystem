using Code_Vanta_Systems.Data;
using Code_Vanta_Systems.Helpers;
using Code_Vanta_Systems.Models;
using Code_Vanta_Systems.Models.Enums;
using Code_Vanta_Systems.Services;
using Code_Vanta_Systems.ViewModels;
using Microsoft.AspNetCore.Authorization;
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
        public async Task<IActionResult> Login()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    if (await _userManager.IsInRoleAsync(user, Roles.Admin))
                        return RedirectToAction("Employees", "Admin");

                    if (await _userManager.IsInRoleAsync(user, Roles.Customer))
                    {
                        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == user.Id);
                        if (customer != null)
                        {
                            if (customer.Status == CustomerStatus.Pending)
                                return RedirectToAction("PendingPage", "CustomerDashboard");
                            if (customer.Status == CustomerStatus.Active)
                                return RedirectToAction("Dashboard", "Customer");

                            return RedirectToAction("Rejected", "CustomerDashboard");
                        }
                        return RedirectToAction("Rejected", "CustomerDashboard");
                    }

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

                    // fallback
                    return RedirectToAction("Index", "Home");
                }
            }

            return View();
        }
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
                // Admin
                if (await _userManager.IsInRoleAsync(user, Roles.Admin))
                    return RedirectToAction("Employees", "Admin");

                // Customer
                if (await _userManager.IsInRoleAsync(user, Roles.Customer))
                {
                    // Check customer status
                    var customer = await _context.Customers
                        .FirstOrDefaultAsync(c => c.UserId == user.Id);

                    if (customer != null && customer.Status == CustomerStatus.Pending)
                    {
                        // Pending customers go to PendingPage to upload document
                        return RedirectToAction("PendingPage", "Customer");
                    }
                    if (customer != null && customer.Status == CustomerStatus.Active)
                    {
                        // Pending customers go to PendingPage to upload document
                        return RedirectToAction("Dashboard", "Customer");
                    }
                    if (customer != null && customer.Status == CustomerStatus.Rejected)
                    {
                        // Pending customers go to PendingPage to upload document
                        return RedirectToAction("Rejected", "Customer");
                    }
                    // Active customers go to dashboard
                    return RedirectToAction("Register", "AccountController");
                }

                // Customer Liaison
                if (await _userManager.IsInRoleAsync(user, Roles.CustomerLiaison))
                    return RedirectToAction("Index", "CustomerLiaison");

                if (await _userManager.IsInRoleAsync(user, Roles.InventoryLiaison))
                {
                    // ✅ Redirect to the Index page of InventoryLiaisonController
                    return RedirectToAction("Index", "InventoryLiaison");
                }
                // Service Technician
                if (await _userManager.IsInRoleAsync(user, Roles.ServiceTechnician))
                    return RedirectToAction("Index", "ServiceTechnician");

                // Fault Technician
                if (await _userManager.IsInRoleAsync(user, Roles.FaultTechnician))
                    return RedirectToAction("Index", "FaultTechnician");

                // Maintenance Technician
                if (await _userManager.IsInRoleAsync(user, Roles.MaintenanceTechnician))
                    return RedirectToAction("Index", "MaintenanceTechnician");

                // Purchasing Manager
                if (await _userManager.IsInRoleAsync(user, Roles.PurchasingManager))
                    return RedirectToAction("Index", "PurchasingManager");
                if (await _userManager.IsInRoleAsync(user, Roles.Supplier))
                    return RedirectToAction("Index", "Supplier");

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
                    UserId = user.Id,
                    Status = CustomerStatus.Pending
                };

                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();

                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("PendingPage", "Customer");
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

        // GET: Account/ChangePasswordForUser/{userId}
        [Authorize(Roles = "Admin")] // Only Admins should be able to reset user passwords
        public async Task<IActionResult> ChangePasswordForUser(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Employees", "Admin"); // Redirect back to employee list
            }

            var model = new ChangePasswordForUserViewModel
            {
                UserId = user.Id,
                UserEmail = user.Email
            };

            return View(model);
        }

        // POST: Account/ChangePasswordForUser
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePasswordForUser(ChangePasswordForUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "User not found.");
                return View(model);
            }

            // 1. Generate a valid password reset token
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            // 2. Use the token to change the password directly
            var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"Password for {user.Email} has been successfully reset.";
                return RedirectToAction("Edit", "Admin", new { id = _context.Employees.FirstOrDefault(e => e.ApplicationUserId == user.Id)?.Id });
                // NOTE: The redirect action attempts to return to the Admin/Edit view for the employee
            }

            // Handle errors if password reset fails
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            // Re-set email display name and return view on error
            model.UserEmail = user.Email;
            return View(model);
        }
    }
}