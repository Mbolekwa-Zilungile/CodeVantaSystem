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
            ViewBag.ActiveSupplierCount = activeSuppliers.Count;

            var InactiveSuppliers = await context.Suppliers.Include(s => s.User)
           .Where(s => !s.IsActive)
           .ToListAsync();

            ViewBag.InactiveSuppliersCount = InactiveSuppliers.Count;
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
                FullName = model.ContactPerson,
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
                        Name = model.CompanyName,
                        Phone = model.Phone,
                        Address = model.Address,
                        IsActive = true
                    };

                    await context.Suppliers.AddAsync(supplier);
                    await context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Supplier '{model.CompanyName}' created successfully.";
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

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var supplier = await context.Suppliers
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (supplier == null || supplier.User == null)
            {
                TempData["ErrorMessage"] = "Error: Supplier not found. Deactivation failed.";
                return RedirectToAction("Suppliers");
            }

            var user = supplier.User;

            // 1. Perform soft delete
            supplier.IsActive = false;
            context.Suppliers.Update(supplier);

            // 2. Lock user account
            await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
            context.Users.Update(user);

            await context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Supplier '{supplier.Name}' successfully deactivated.";
            return RedirectToAction("Suppliers");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            // 1. Find the Supplier
            var supplier = await context.Suppliers
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (supplier == null || supplier.User == null)
            {
                TempData["ErrorMessage"] = "Supplier not found.";
                return RedirectToAction("Suppliers");
            }

            // 2. Map Supplier data to the ViewModel
            var viewModel = new EditSupplierViewModel
            {
                Id = supplier.Id,
                ApplicationUserId = supplier.ApplicationUserId,
                FullName = supplier.User.FullName,
                Email = supplier.User.Email,
                Phone = supplier.Phone,
                Name = supplier.Name,
                Address = supplier.Address,
                IsActive = supplier.IsActive
            };

            ViewData["Title"] = $"Edit Supplier: {supplier.Name}";
            return View(viewModel); // Ensure you have a View that uses RegisterSupplierViewModel
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditSupplierViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var supplier = await context.Suppliers
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (supplier == null || supplier.User == null)
            {
                TempData["ErrorMessage"] = "Supplier not found.";
                return RedirectToAction("Suppliers");
            }

            var user = supplier.User;

            try
            {
                // 1. Update ApplicationUser fields
                user.FullName = model.FullName;
                user.Email = model.Email;
                user.UserName = model.Email;
                user.PhoneNumber = model.Phone;

                await userManager.UpdateAsync(user);

                // 2. Update Supplier entity
                supplier.Name = model.Name;
                supplier.Phone = model.Phone;
                supplier.Address = model.Address;
                supplier.IsActive = model.IsActive;

                // 3. Lock or unlock the account
                if (model.IsActive)
                {
                    await userManager.SetLockoutEndDateAsync(user, null); // Reactivate
                }
                else
                {
                    await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue); // Deactivate
                }

                context.Suppliers.Update(supplier);
                await context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Supplier '{supplier.Name}' updated successfully. Status: {(model.IsActive ? "Active" : "Inactive")}.";
                return RedirectToAction("Suppliers");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!context.Suppliers.Any(s => s.Id == id))
                {
                    return NotFound();
                }
                throw;
            }
        }

    }
}
