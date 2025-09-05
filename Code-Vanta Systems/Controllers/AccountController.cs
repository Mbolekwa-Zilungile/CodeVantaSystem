using Code_Vanta_Systems.Data;
using Code_Vanta_Systems.Models;
using Code_Vanta_Systems.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Identity.Client;
using Code_Vanta_Systems.Models.Enums;

namespace Code_Vanta_Systems.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly ApplicationDbContext context;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            this.signInManager = signInManager;
            this.userManager = userManager;
            this.roleManager = roleManager;
            this.context = context; // 👈 Assign context in constructor
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModels model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "User with this email does not exist. Please register first.");
                return View(model);
            }

            var result = await signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, false);

            if (result.Succeeded)
            {
                if (await userManager.IsInRoleAsync(user, "Admin"))
                {
                    return RedirectToAction("Index", "Admin"); //action, controller
                }
                else if (await userManager.IsInRoleAsync(user, "Customer"))
                {
                    return RedirectToAction("Index", "Customer");
                }
                else if (await userManager.IsInRoleAsync(user, "Customer Liaison"))
                {
                    return RedirectToAction("Index", "CustomerLiaison");
                }
                else if (await userManager.IsInRoleAsync(user, "Inventory Liaison"))
                {
                    return RedirectToAction("Index", "InventoryLiaison");
                }
                else if (await userManager.IsInRoleAsync(user, "Service Technician"))
                {
                    return RedirectToAction("Index", "ServiceTechnician");
                }
                else if (await userManager.IsInRoleAsync(user, "Fault Technician"))
                {
                    return RedirectToAction("Index", "FaultTechnician");
                }
                else if (await userManager.IsInRoleAsync(user, "Maintenance Technician"))
                {
                    return RedirectToAction("Index", "MaintenanceTechnician");
                }
                else if (await userManager.IsInRoleAsync(user, "Purchasing Manager"))
                {
                    return RedirectToAction("Index", "PurchasingManager");
                }
                // Fallback for any other roles or new roles
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
                RegionOptions = Enum.GetValues(typeof(Region))
                    .Cast<Region>()
                    .Select(r => new SelectListItem
                    {
                        Value = r.ToString(),
                        Text = r.ToString()
                    })
                    .ToList()
            };

            return View(model);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.RegionOptions = Enum.GetValues(typeof(Region))
                    .Cast<Region>()
                    .Select(r => new SelectListItem
                    {
                        Value = r.ToString(),
                        Text = r.ToString()
                    })
                    .ToList();

                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.Name
            };

            var result = await userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);

                model.RegionOptions = Enum.GetValues(typeof(Region))
                    .Cast<Region>()
                    .Select(r => new SelectListItem
                    {
                        Value = r.ToString(),
                        Text = r.ToString()
                    })
                    .ToList();

                return View(model);
            }

            await userManager.AddToRoleAsync(user, "Customer");

            var customer = new Customer
            {
                BusinessName = model.BusinessName,
                ContactName = model.Name,
                Phone = model.Phone,
                Email = model.Email,
                LocationId = (int)model.SelectedRegion, // Or map to a Location table if needed
                UserId = user.Id
            };

            context.Customers.Add(customer);
            await context.SaveChangesAsync();

            await signInManager.SignInAsync(user, isPersistent: false);
            TempData["SuccessMessage"] = "Registration successful! Welcome.";

            return RedirectToAction("Login", "Account");
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

    }
}