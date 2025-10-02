using Code_Vanta_Systems.Data;
using Code_Vanta_Systems.Models;
using Code_Vanta_Systems.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Code_Vanta_Systems.Controllers
{
    [Authorize(Roles = "Purchasing Manager")]
    public class PurchasingManagerController : Controller
    {

        private readonly UserManager<ApplicationUser> userManager;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly ApplicationDbContext context;

        public PurchasingManagerController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            this.userManager = userManager;
            this.roleManager = roleManager;
            this.context = context;
        }

        public async Task<IActionResult> Index()
        {
            var activeSuppliers = await context.Suppliers
            .ToListAsync();
            ViewBag.ActiveSupplierCount = activeSuppliers.Count;
            return View();
        }
        public async Task<IActionResult> Suppliers()
        {
            var activeSuppliers = await context.Suppliers.Include(s => s.User)
            .Where(s => s.IsActive)
            .ToListAsync();
            return View(activeSuppliers);
        }
        // GET: Admin/CreateSupplier
        [HttpGet]
        public IActionResult Create()
        {
            ViewData["Title"] = "Create New Supplier";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RegisterSupplierViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // 1. Create ApplicationUser without password
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                EmailConfirmed = true,
                PhoneNumber = model.Phone
            };

            var userCreationResult = await userManager.CreateAsync(user);

            if (userCreationResult.Succeeded)
            {
                // 2. Check role exists and assign "Supplier" role
                var roleExists = await context.Roles.AnyAsync(r => r.Name == "Supplier");
                if (!roleExists)
                {
                    ModelState.AddModelError(string.Empty, "Supplier role does not exist. Please create the role first.");
                    return View(model);
                }

                var roleResult = await userManager.AddToRoleAsync(user, "Supplier");

                if (roleResult.Succeeded)
                {
                    // 3. Create Supplier record
                    var supplier = new Supplier
                    {
                        ApplicationUserId = user.Id,
                        Name = model.Name,
                        Phone = model.Phone,
                        Address = model.Address,
                        IsActive = true
                    };

                    await context.Suppliers.AddAsync(supplier);
                    await context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Supplier '{model.Name}' created successfully.";
                    return RedirectToAction("Suppliers"); // Adjust this action to your supplier list page
                }

                // Cleanup user if role assignment fails
                await userManager.DeleteAsync(user);
                ModelState.AddModelError(string.Empty, "Failed to assign 'Supplier' role. User has been deleted.");
            }
            else
            {
                foreach (var error in userCreationResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }
    }
}
