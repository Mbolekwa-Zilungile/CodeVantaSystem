// File: Controllers/AdminController.cs

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

        //---------------------------------------------------------
        // DASHBOARD
        //---------------------------------------------------------

        public async Task<IActionResult> Index()
        {
            // 1. Fetch Employee Count (Users who are not Admin or Customer)
            var allEmployees = await context.Employees.Include(e => e.User).ToListAsync();
            var filteredEmployees = new List<Employee>();

            foreach (var emp in allEmployees)
            {
                if (emp.User != null &&
                    !await userManager.IsInRoleAsync(emp.User, "Admin") &&
                    !await userManager.IsInRoleAsync(emp.User, "Customer"))
                {
                    filteredEmployees.Add(emp);
                }
            }
            int employeeCount = filteredEmployees.Count;

            // 2. Fetch Customer Counts using the IsActive flag
            int totalCustomers = await context.Customers.CountAsync();

            // Count only active customers
            int activeCustomers = await context.Customers
                .CountAsync(c => c.IsActive == true);

            // Calculate inactive customers
            int inactiveCustomers = totalCustomers - activeCustomers;

            // 3. Fetch Total System Users
            int totalUsers = await userManager.Users.CountAsync();

            // 4. Populate ViewModel (Ensure your AdminDashboardViewModel has ActiveCustomersCount and InactiveCustomersCount)
            var viewModel = new AdminDashboardViewModel
            {
                EmployeeCount = employeeCount,
                TotalCustomers = totalCustomers,
                ActiveCustomersCount = activeCustomers,
                InactiveCustomersCount = inactiveCustomers, // <-- This is the stat you need
                TotalSystemUsers = totalUsers,

                // ... (other metrics) ...
            };

            return View(viewModel); // Return the Dashboard View
        }

        //---------------------------------------------------------
        // EMPLOYEE MANAGEMENT
        //---------------------------------------------------------

        // Employees List
        public async Task<IActionResult> Employees()
        {
            var employees = await context.Employees
                .Include(e => e.User)
                // 🚨 NOTE: Assuming Employee model has IsActive, filtering only active employees
                .Where(e => e.IsActive == true)
                .ToListAsync();

            // Filter out employees whose user is in Admin or Customer role
            var filteredEmployees = new List<Employee>();
            foreach (var emp in employees)
            {
                if (emp.User != null &&
                    !await userManager.IsInRoleAsync(emp.User, "Admin") &&
                    !await userManager.IsInRoleAsync(emp.User, "Customer"))
                {
                    filteredEmployees.Add(emp);
                }
            }

            ViewBag.EmployeeCount = filteredEmployees.Count;
            return View(filteredEmployees);
        }

        // GET: Admin/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            // Fetch all roles except 'Admin' and 'Customer'
            var roles = await roleManager.Roles
                .Where(r => r.Name != "Admin" && r.Name != "Customer")
                .Select(r => new SelectListItem
                {
                    Value = r.Name,
                    Text = r.Name
                })
                .ToListAsync();

            var viewModel = new RegisterEmployeeViewModel
            {
                Roles = roles
            };

            ViewData["Title"] = "Create New Employee";
            return View(viewModel);
        }

        // POST: Admin/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RegisterEmployeeViewModel model)
        {
            // Re-fetch roles if model state is invalid
            if (!ModelState.IsValid)
            {
                model.Roles = await roleManager.Roles
                    .Where(r => r.Name != "Admin" && r.Name != "Customer")
                    .Select(r => new SelectListItem { Value = r.Name, Text = r.Name })
                    .ToListAsync();
                return View(model);
            }

            // 1. Create ApplicationUser
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // 2. Assign Role
                await userManager.AddToRoleAsync(user, model.SelectedRole);

                // 3. Create Employee Profile
                var employee = new Employee
                {
                    ApplicationUserId = user.Id, // Link the new user's ID
                    Position = model.SelectedRole,
                    PhoneNumber = model.PhoneNumber,
                    IsActive = true // Default to active
                };

                await context.Employees.AddAsync(employee);
                await context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Employee '{model.FullName}' created and assigned the '{model.SelectedRole}' role.";
                return RedirectToAction("Employees");
            }

            // If creation failed, add errors to ModelState
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            // Re-fetch roles and return to view on error
            model.Roles = await roleManager.Roles
                .Where(r => r.Name != "Admin" && r.Name != "Customer")
                .Select(r => new SelectListItem { Value = r.Name, Text = r.Name })
                .ToListAsync();

            return View(model);
        }

        // GET: Admin/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            // 1. Find the Employee 
            var employee = await context.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null)
            {
                TempData["ErrorMessage"] = "Error: Employee not found.";
                return RedirectToAction("Employees");
            }

            // 2. Fetch all non-Admin/non-Customer roles for the dropdown
            var roles = await roleManager.Roles
                .Where(r => r.Name != "Admin" && r.Name != "Customer")
                .Select(r => new SelectListItem
                {
                    Value = r.Name,
                    Text = r.Name
                })
                .ToListAsync();

            // 3. Map Employee data to the ViewModel
            var viewModel = new RegisterEmployeeViewModel
            {
                Id = employee.Id,
                FullName = employee.User?.FullName,
                Email = employee.User?.Email,
                PhoneNumber = employee.PhoneNumber,

                // Find the current role
                SelectedRole = (await userManager.GetRolesAsync(employee.User)).FirstOrDefault() ?? employee.Position,

                Roles = roles,
            };

            ViewData["Title"] = $"Edit Employee: {employee.User?.FullName}";
            return View(viewModel);
        }

        // POST: Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(RegisterEmployeeViewModel model)
        {
            // 🚨 FIX: Remove Password validation if fields are empty
            if (string.IsNullOrEmpty(model.Password))
            {
                ModelState.Remove("Password");
                ModelState.Remove("ConfirmPassword");
            }

            // If the model is invalid, re-fetch roles and return
            if (!ModelState.IsValid)
            {
                // Re-fetch roles excluding Admin/Customer for the dropdown
                model.Roles = await roleManager.Roles
                    .Where(r => r.Name != "Admin" && r.Name != "Customer")
                    .Select(r => new SelectListItem { Value = r.Name, Text = r.Name })
                    .ToListAsync();
                return View(model);
            }

            // Fetch the employee to update
            var employee = await context.Employees.Include(e => e.User).FirstOrDefaultAsync(e => e.Id == model.Id);

            if (employee == null || employee.User == null)
            {
                TempData["ErrorMessage"] = "Error: Employee or associated user not found for update.";
                return RedirectToAction("Employees");
            }

            // Update ApplicationUser info
            employee.User.FullName = model.FullName;
            employee.User.Email = model.Email;
            employee.User.UserName = model.Email;

            // Update Employee Profile info
            employee.Position = model.SelectedRole;
            employee.PhoneNumber = model.PhoneNumber;

            // Update Role
            var currentRoles = await userManager.GetRolesAsync(employee.User);

            if (currentRoles.Any())
            {
                // Remove all current roles
                await userManager.RemoveFromRolesAsync(employee.User, currentRoles);
            }
            // Add the new role
            await userManager.AddToRoleAsync(employee.User, model.SelectedRole);

            // Password update logic REMOVED as requested.

            // Save changes to the database
            context.Employees.Update(employee);
            context.Users.Update(employee.User);
            await context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Employee details for '{model.FullName}' updated successfully.";
            return RedirectToAction("Employees");
        }

        // GET: Admin/Delete/5 (Employee Confirmation View for SOFT DELETE)
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var employee = await context.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null)
            {
                TempData["ErrorMessage"] = "Error: Employee not found.";
                return RedirectToAction("Employees");
            }

            // Map the Employee data to the RegisterEmployeeViewModel
            var viewModel = new RegisterEmployeeViewModel
            {
                Id = employee.Id,
                FullName = employee.User?.FullName,
                Email = employee.User?.Email,
            };

            ViewData["Title"] = $"Deactivate Employee: {employee.User?.FullName}";
            return View(viewModel); // Points to Views/Admin/Delete.cshtml
        }

        // POST: Admin/Delete/5 (Employee Soft Delete Execution)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        // 🚨 Renaming this to DeleteConfirmed is fine as it applies to employees only
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var employee = await context.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null || employee.User == null)
            {
                TempData["ErrorMessage"] = "Error: Employee not found. Deactivation failed.";
                return RedirectToAction("Employees");
            }

            // Prevent deactivation of Admin/Customer roles if they somehow got here
            if (await userManager.IsInRoleAsync(employee.User, "Admin") ||
                await userManager.IsInRoleAsync(employee.User, "Customer"))
            {
                TempData["ErrorMessage"] = "Cannot deactivate employees with Admin or Customer roles.";
                return RedirectToAction("Employees");
            }

            // 1. Perform Soft Delete
            employee.IsActive = false;
            context.Employees.Update(employee);

            // 2. Lock the ApplicationUser account to prevent login (Optional but recommended for deactivation)
            await userManager.SetLockoutEndDateAsync(employee.User, DateTimeOffset.MaxValue);
            context.Users.Update(employee.User);

            await context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Employee '{employee.User.FullName}' successfully deactivated.";
            return RedirectToAction("Employees");
        }


        //---------------------------------------------------------
        // CUSTOMER MANAGEMENT
        //---------------------------------------------------------

        // Customers List
        public async Task<IActionResult> Customers()
        {
            // Load directly from the Customers table, including related User and Location data
            var customers = await context.Customers
                .Include(c => c.User)      // For Full Name, Email
                .Include(c => c.Location)  // For Region/Location details
                .Where(c => c.UserId != null && c.IsActive == true) // 🚨 FIX: Show only ACTIVE customers
                .ToListAsync();

            ViewBag.CustomerCount = customers.Count; // This is now the ACTIVE count
            return View(customers); // Pass the list of Customer models to the view
        }

        // GET: Admin/EditCustomer/7
        [HttpGet]
        public async Task<IActionResult> EditCustomer(int id)
        {
            var customer = await context.Customers
                .Include(c => c.User)
                .Include(c => c.Location)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (customer == null)
            {
                TempData["ErrorMessage"] = "Error: Customer not found.";
                return RedirectToAction("Customers");
            }

            // Fetch all locations for the dropdown
            var locations = await context.Locations
                .Select(l => new SelectListItem
                {
                    Value = l.Id.ToString(),
                    Text = $"{l.Description} ({l.City}, {l.Province})"
                })
                .ToListAsync();

            // Map Customer model to ViewModel
            var viewModel = new EditCustomerViewModel
            {
                Id = customer.Id,
                UserId = customer.UserId,
                BusinessName = customer.BusinessName,
                ContactName = customer.ContactName,
                Phone = customer.Phone,
                Email = customer.User?.Email ?? customer.Email,
                BusinessType = customer.BusinessType,
                PhysicalAddress = customer.PhysicalAddress,
                LocationId = customer.LocationId,
                CurrentLocationDescription = customer.Location?.Description,
                AvailableLocations = locations
            };

            ViewData["Title"] = $"Edit Customer: {customer.BusinessName}";
            return View(viewModel);
        }

        // POST: Admin/EditCustomer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCustomer(EditCustomerViewModel model)
        {
            // Re-fetch locations for dropdown in case model state fails
            if (!ModelState.IsValid)
            {
                model.AvailableLocations = await context.Locations
                    .Select(l => new SelectListItem
                    {
                        Value = l.Id.ToString(),
                        Text = $"{l.Description} ({l.City}, {l.Province})"
                    })
                    .ToListAsync();
                return View(model);
            }

            // 1. Fetch the Customer entity
            var customer = await context.Customers
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == model.Id);

            if (customer == null)
            {
                TempData["ErrorMessage"] = "Error: Customer not found for update.";
                return RedirectToAction("Customers");
            }

            // 2. Update Customer Profile data
            customer.BusinessName = model.BusinessName;
            customer.ContactName = model.ContactName;
            customer.Phone = model.Phone;
            customer.Email = model.Email; // Update Customer table email
            customer.BusinessType = model.BusinessType;
            customer.PhysicalAddress = model.PhysicalAddress;
            customer.LocationId = model.LocationId;

            // 3. Update ApplicationUser data (if the user is linked)
            if (customer.User != null)
            {
                customer.User.FullName = model.ContactName; // Use ContactName as FullName
                customer.User.Email = model.Email;
                customer.User.UserName = model.Email;

                // Ensure update is tracked
                context.Users.Update(customer.User);
            }

            // 4. Save changes
            context.Customers.Update(customer);
            await context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Customer '{model.BusinessName}' details updated successfully.";
            return RedirectToAction("Customers");
        }


        // GET: Admin/DeleteCustomer/7 (Confirmation View)
        [HttpGet]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            var customer = await context.Customers
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (customer == null)
            {
                TempData["ErrorMessage"] = "Error: Customer not found.";
                return RedirectToAction("Customers");
            }

            // Map the Customer data to the EditCustomerViewModel (used for displaying details)
            var viewModel = new EditCustomerViewModel
            {
                Id = customer.Id,
                UserId = customer.UserId,
                BusinessName = customer.BusinessName,
                ContactName = customer.ContactName,
                Email = customer.User?.Email ?? customer.Email,
                Phone = customer.Phone
            };

            ViewData["Title"] = $"Deactivate Customer: {customer.BusinessName}";
            return View(viewModel); // Points to Views/Admin/DeleteCustomer.cshtml
        }

        // POST: Admin/DeleteCustomer/7 (Soft Delete/Deactivation Execution)
        [HttpPost, ActionName("DeleteCustomer")]
        [ValidateAntiForgeryToken]
        // 🚨 FIX: Renamed to avoid conflict with employee's DeleteConfirmed(int id)
        public async Task<IActionResult> DeactivateCustomerConfirmed(int id)
        {
            var customer = await context.Customers
                .FirstOrDefaultAsync(c => c.Id == id);

            if (customer == null)
            {
                TempData["ErrorMessage"] = "Error: Customer not found. Deactivation failed.";
                return RedirectToAction("Customers");
            }

            // 1. Perform Soft Delete on the Customer record
            customer.IsActive = false;
            context.Customers.Update(customer);

            // 2. Lock the ApplicationUser account to prevent login (if linked)
            if (customer.UserId != null)
            {
                var user = await userManager.FindByIdAsync(customer.UserId);
                if (user != null)
                {
                    // Lock the account indefinitely
                    await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
                    context.Users.Update(user);
                }
            }

            await context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Customer '{customer.BusinessName}' successfully deactivated (soft deleted).";
            return RedirectToAction("Customers");
        }


        //---------------------------------------------------------
        // ROLE MANAGEMENT
        //---------------------------------------------------------

        // Manage Roles - Lists all users and their roles
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

            var viewModel = new RoleManagementViewModel
            {
                Users = users,
                Roles = roles,
                UserRoles = userRoles
            };

            return View(viewModel);
        }

        // GET: Admin/EditUserRoles?userId=...
        [HttpGet]
        public async Task<IActionResult> EditUserRoles(string userId)
        {
            // 1. Find the user
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Error: User not found.";
                return RedirectToAction("ManageRoles");
            }

            // 2. Get the current and all available roles
            var currentRoles = await userManager.GetRolesAsync(user);
            var allRoles = await roleManager.Roles
                .Select(r => new SelectListItem
                {
                    Value = r.Name,
                    Text = r.Name
                })
                .ToListAsync();

            // 3. Map to ViewModel
            var viewModel = new RoleAssignmentViewModel
            {
                UserId = user.Id,
                // Attempt to get FullName from Employee or default to Email
                FullName = (await context.Employees.Include(e => e.User).FirstOrDefaultAsync(e => e.ApplicationUserId == userId))?.User?.FullName ?? user.Email,
                Email = user.Email,
                AvailableRoles = allRoles,
                SelectedRole = currentRoles.FirstOrDefault() ?? ""
            };

            ViewData["Title"] = $"Manage Roles for {user.Email}";
            return View(viewModel);
        }

        // POST: Admin/EditUserRoles
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUserRoles(RoleAssignmentViewModel model)
        {
            // Re-load roles if validation fails
            if (!ModelState.IsValid)
            {
                model.AvailableRoles = await roleManager.Roles
                    .Select(r => new SelectListItem
                    {
                        Value = r.Name,
                        Text = r.Name
                    })
                    .ToListAsync();
                return View(model);
            }

            var user = await userManager.FindByIdAsync(model.UserId);
            if (user == null) return NotFound();

            // 1. Get current roles
            var currentRoles = await userManager.GetRolesAsync(user);

            // 2. Remove all current roles
            if (currentRoles.Any())
            {
                var removeResult = await userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                {
                    TempData["ErrorMessage"] = $"Failed to remove old roles: {string.Join(", ", removeResult.Errors.Select(e => e.Description))}";
                    return RedirectToAction("ManageRoles");
                }
            }

            // 3. Add the new role
            var result = await userManager.AddToRoleAsync(user, model.SelectedRole);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"Successfully assigned '{model.SelectedRole}' role to {user.Email}.";
                return RedirectToAction("ManageRoles");
            }

            // Handle errors if role assignment fails
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            // Re-load roles and return to view on error
            model.AvailableRoles = await roleManager.Roles
                .Select(r => new SelectListItem
                {
                    Value = r.Name,
                    Text = r.Name
                })
                .ToListAsync();
            return View(model);
        }
    }
}