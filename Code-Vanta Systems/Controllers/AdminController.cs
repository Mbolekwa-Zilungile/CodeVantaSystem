using Code_Vanta_Systems.Data;
using Code_Vanta_Systems.Models;
using Code_Vanta_Systems.ViewModels;
using Code_Vanta_Systems.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Code_Vanta_Systems.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly ApplicationDbContext context;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            this.userManager = userManager;
            this.roleManager = roleManager;
            this.context = context;
        }

        //ManageRoles
        public IActionResult Index()
        {
            return RedirectToAction("ManageRoles");
        }

        //Manage Roles
        public IActionResult ManageRoles()
        {
            var users = userManager.Users.ToList();
            var roles = roleManager.Roles.ToList();
            var userRoles = new Dictionary<string, IList<string>>();

            foreach (var user in users)
            {
                var rolesForUser = userManager.GetRolesAsync(user).Result;
                userRoles[user.Id] = rolesForUser;
            }

            return View(new RoleManagementViewModel
            {
                Users = users,
                Roles = roles,
                UserRoles = userRoles
            });
        }

        // Assign Role
        [HttpPost]
        public async Task<IActionResult> AssignRole(string userId, string role)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user != null && await roleManager.RoleExistsAsync(role))
            {
                await userManager.AddToRoleAsync(user, role);
            }

            return RedirectToAction(nameof(ManageRoles));
        }

        // Remove Role
        [HttpPost]
        public async Task<IActionResult> RemoveRole(string userId, string role)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user != null && await userManager.IsInRoleAsync(user, role))
            {
                await userManager.RemoveFromRoleAsync(user, role);
            }

            return RedirectToAction(nameof(ManageRoles));
        }

        // GET: Create
        [HttpGet]
        public IActionResult Create()
        {
            var model = new RegisterEmployeeViewModel
            {
                Roles = GetAvailableRoles()
            };
            return View(model);
        }

        // POST: Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RegisterEmployeeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Roles = GetAvailableRoles();
                return View(model);
            }

            var existingUser = await userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "An account with this email already exists.");
                model.Roles = GetAvailableRoles();
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                EmailConfirmed = true // important
            };

            var result = await userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);

                model.Roles = GetAvailableRoles();
                return View(model);
            }

            await userManager.AddToRoleAsync(user, model.SelectedRole);

            var employee = new Employee
            {
                ApplicationUserId = user.Id,
                Position = model.SelectedRole,
                PhoneNumber = model.PhoneNumber,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                Role = Enum.TryParse<EmployeeRole>(model.SelectedRole, out var parsedRole)
                    ? parsedRole
                    : EmployeeRole.Technician
            };

            context.Employees.Add(employee);
            await context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Employee added successfully";
            return RedirectToAction("Employees");
        }

        // GET: Edit
        public async Task<IActionResult> Edit(int id)
        {
            var employee = await context.Employees
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (employee == null) return NotFound();

            var currentRole = await userManager.GetRolesAsync(employee.User);

            var viewModel = new EditEmployeeViewModel
            {
                Id = employee.Id,
                FullName = employee.User.FullName,
                Email = employee.User.Email,
                PhoneNumber = employee.PhoneNumber,
                SelectedRole = currentRole.FirstOrDefault(),
                Roles = GetAvailableRoles()
            };

            return View(viewModel);
        }


        // POST: Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditEmployeeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Roles = GetAvailableRoles();
                return View(model);
            }

            var employee = await context.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.Id == model.Id);

            if (employee == null) return NotFound();

            // update ApplicationUser info
            employee.User.FullName = model.FullName;

            if (!string.Equals(employee.User.Email, model.Email, StringComparison.OrdinalIgnoreCase))
            {
                await userManager.SetEmailAsync(employee.User, model.Email);
                await userManager.SetUserNameAsync(employee.User, model.Email);
            }

            if (!string.Equals(employee.User.PhoneNumber, model.PhoneNumber, StringComparison.OrdinalIgnoreCase))
            {
                await userManager.SetPhoneNumberAsync(employee.User, model.PhoneNumber);
            }

            // update roles
            var currentRoles = await userManager.GetRolesAsync(employee.User);
            if (currentRoles.Any())
            {
                await userManager.RemoveFromRolesAsync(employee.User, currentRoles);
            }

            if (!string.IsNullOrEmpty(model.SelectedRole))
            {
                await userManager.AddToRoleAsync(employee.User, model.SelectedRole);

                employee.Position = model.SelectedRole;
                employee.Role = Enum.TryParse<EmployeeRole>(model.SelectedRole, out var parsedRole)
                    ? parsedRole
                    : employee.Role;
            }

            employee.PhoneNumber = model.PhoneNumber;

            context.Employees.Update(employee);
            await context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Employee details updated successfully";
            return RedirectToAction("Employees");
        }

        private List<SelectListItem> GetAvailableRoles()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = Roles.CustomerLiaison, Text = "Customer Liaison" },
                new SelectListItem { Value = Roles.InventoryLiaison, Text = "Inventory Liaison" },
                new SelectListItem { Value = Roles.ServiceTechnician, Text = "Service Technician" },
                new SelectListItem { Value = Roles.FaultTechnician, Text = "Fault Technician" },
                new SelectListItem { Value = Roles.MaintenanceTechnician, Text = "Maintenance Technician" },
                new SelectListItem { Value = Roles.PurchasingManager, Text = "Purchasing Manager" },
            };
        }

        // Employees
        public async Task<IActionResult> Employees()
        {
            var employees = await context.Employees
                .Include(e => e.User)
                .Where(e => e.IsActive) // filter active only
                .ToListAsync();

            var filteredEmployees = new List<Employee>();
            foreach (var emp in employees)
            {
                if (!await userManager.IsInRoleAsync(emp.User, "Admin") &&
                    !await userManager.IsInRoleAsync(emp.User, "Customer"))
                {
                    filteredEmployees.Add(emp);
                }
            }

            ViewBag.EmployeeCount = filteredEmployees.Count;
            return View(filteredEmployees);
        }


        // Customers
        public async Task<IActionResult> Customers()
        {
            // Load all employees including ApplicationUser
            var employees = await context.Employees
                .Include(e => e.User)
                .ToListAsync();

            // Filter only users in the "Customer" role
            var customers = new List<Employee>();
            foreach (var emp in employees)
            {
                if (await userManager.IsInRoleAsync(emp.User, "Customer"))
                {
                    customers.Add(emp);
                }
            }

            ViewBag.CustomerCount = customers.Count;
            return View(customers);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var employee = await context.Employees.FindAsync(id);
            if (employee == null)
            {
                return NotFound();
            }

            var user = await userManager.FindByIdAsync(employee.ApplicationUserId);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await userManager.GetRolesAsync(user);

            // Skip Admin or Customer roles
            if (roles.Contains("Admin") || roles.Contains("Customer"))
            {
                TempData["ErrorMessage"] = "Cannot delete employees with Admin or Customer roles.";
                return RedirectToAction(nameof(Employees));
            }

            // Remove all roles from the user
            if (roles.Any())
            {
                var removeResult = await userManager.RemoveFromRolesAsync(user, roles);
                if (!removeResult.Succeeded)
                {
                    TempData["ErrorMessage"] = "Failed to remove user roles.";
                    return RedirectToAction(nameof(Employees));
                }
            }

            employee.IsActive = false; // Soft delete
            context.Update(employee);
            await context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Employee soft deleted and roles removed successfully.";

            return RedirectToAction(nameof(Employees));
        }



    }
}




