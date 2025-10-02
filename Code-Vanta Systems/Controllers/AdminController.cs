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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
            // --- 1. Calculate Employee Count ---
            var allEmployees = await context.Employees.Include(e => e.User).ToListAsync();
            var activeEmployeeCount = 0;
            var inactiveEmployeeCount = 0;

            foreach (var emp in allEmployees)
            {
                if (emp.User == null) continue;

                // Exclude Admin, Customer, and Supplier roles
                if (await userManager.IsInRoleAsync(emp.User, "Admin") ||
                    await userManager.IsInRoleAsync(emp.User, "Customer") ||
                    await userManager.IsInRoleAsync(emp.User, "Supplier"))
                {
                    continue;
                }

                if (emp.IsActive)
                {
                    activeEmployeeCount++;
                }
                else
                {
                    inactiveEmployeeCount++;
                }
            }

            // --- 2. Calculate Customer Counts ---
            var allCustomers = await context.Customers.ToListAsync();
            int totalCustomers = allCustomers.Count;
            int activeCustomers = allCustomers.Count(c => c.IsActive);
            int inactiveCustomers = allCustomers.Count(c => !c.IsActive);

            // --- 3. Calculate Total System Users ---
            int totalUsers = await userManager.Users.CountAsync();

            // 4. Populate ViewModel
            var viewModel = new AdminDashboardViewModel
            {
                EmployeeCount = activeEmployeeCount, // Show only active employees on the main card
                ActiveCustomersCount = activeCustomers,
                InactiveCustomersCount = inactiveCustomers,
                TotalCustomers = totalCustomers,
                TotalSystemUsers = totalUsers,

                // You might want to add TotalEmployees or InactiveEmployeeCount to the ViewModel
            };

            return View(viewModel); // Return the Dashboard View
        }

        // Action to redirect to the ManageRoles action for total users
        public IActionResult TotalUsers()
        {
            return RedirectToAction("ManageRoles");
        }

        //---------------------------------------------------------
        // EMPLOYEE MANAGEMENT
        //---------------------------------------------------------

        // Employees List (Active Staff)
        public async Task<IActionResult> Employees()
        {
            var allEmployees = await context.Employees.Include(e => e.User).ToListAsync();

            var activeStaff = new List<Employee>();
            int activeCount = 0;
            int inactiveCount = 0;

            foreach (var emp in allEmployees)
            {
                if (emp.User == null) continue;

                // Exclude Admin, Customer, and Supplier roles
                if (await userManager.IsInRoleAsync(emp.User, "Admin") ||
                    await userManager.IsInRoleAsync(emp.User, "Customer") ||
                    await userManager.IsInRoleAsync(emp.User, "Supplier"))
                {
                    continue;
                }

                if (emp.IsActive)
                {
                    activeCount++;
                    activeStaff.Add(emp); // Add active staff to the list model
                }
                else
                {
                    inactiveCount++;
                }
            }

            // Set ViewBag for the Employees.cshtml dashboard cards
            ViewBag.EmployeeCount = activeCount; // For backward compatibility with the original Employees.cshtml
            ViewBag.ActiveEmployeeCount = activeCount;
            ViewBag.InactiveEmployeeCount = inactiveCount;

            return View(activeStaff); // Return only the Active Staff
        }

        // InactiveEmployees (Deactivated Staff List)
        public async Task<IActionResult> InactiveEmployees()
        {
            var allEmployees = await context.Employees.Include(e => e.User).ToListAsync();

            var inactiveStaff = new List<Employee>();
            int activeCount = 0;
            int inactiveCount = 0;

            foreach (var emp in allEmployees)
            {
                if (emp.User == null) continue;

                // Exclude Admin, Customer, and Supplier roles
                if (await userManager.IsInRoleAsync(emp.User, "Admin") ||
                    await userManager.IsInRoleAsync(emp.User, "Customer") ||
                    await userManager.IsInRoleAsync(emp.User, "Supplier"))
                {
                    continue;
                }

                if (emp.IsActive)
                {
                    activeCount++;
                }
                else
                {
                    inactiveCount++;
                    inactiveStaff.Add(emp); // Add INACTIVE staff to the list model
                }
            }

            // Set ViewBag for the view
            ViewBag.ActiveEmployeeCount = activeCount;
            ViewBag.InactiveEmployeeCount = inactiveCount;
            ViewBag.EmployeeCount = inactiveCount; // For backward compatibility with the original Employees.cshtml

            return View(inactiveStaff); // Return only the Inactive Staff
        }

        // GET: Admin/Create (Employee Registration Form)
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var viewModel = new RegisterEmployeeViewModel
            {
                // Fetch all non-Admin/non-Customer/non-Supplier roles
                Roles = await GetAvailableRolesAsync()
            };

            ViewData["Title"] = "Create New Employee";
            return View(viewModel);
        }

        // POST: Admin/Create (Handle Employee Registration Submission)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RegisterEmployeeViewModel model)
        {
            // Re-fetch roles if model state is invalid
            if (!ModelState.IsValid)
            {
                model.Roles = await GetAvailableRolesAsync();
                return View(model);
            }

            // 1. Create ApplicationUser
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                EmailConfirmed = true,
                PhoneNumber = model.PhoneNumber // Added from AdminController1
            };

            var userCreationResult = await userManager.CreateAsync(user, model.Password);

            if (userCreationResult.Succeeded)
            {
                // 2. Assign Role
                var roleResult = await userManager.AddToRoleAsync(user, model.SelectedRole);

                if (roleResult.Succeeded)
                {
                    // 3. Create Employee Profile
                    var employee = new Employee
                    {
                        ApplicationUserId = user.Id, // Link the new user's ID
                        Position = model.SelectedRole,
                        PhoneNumber = model.PhoneNumber,
                        IsActive = true, // Default to active
                        // Assuming CreatedAt exists on Employee model from AdminController1 logic
                        // CreatedAt = DateTime.Now // Add if your model supports it
                    };

                    await context.Employees.AddAsync(employee);
                    await context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Employee '{model.FullName}' created and assigned the '{model.SelectedRole}' role.";
                    return RedirectToAction("Employees");
                }

                // Handle Role Assignment Failure (CRITICAL: Delete the created user)
                await userManager.DeleteAsync(user);
                ModelState.AddModelError(string.Empty, $"Error assigning role '{model.SelectedRole}'. User deleted. Please try again.");
            }
            else
            {
                // If user creation failed, add errors to ModelState
                foreach (var error in userCreationResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }


            // Re-load roles and return to view on any error
            model.Roles = await GetAvailableRolesAsync();
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

            if (employee == null || employee.User == null)
            {
                TempData["ErrorMessage"] = "Error: Employee not found.";
                return RedirectToAction("Employees");
            }

            // 2. Get current role for the user
            var currentRoles = await userManager.GetRolesAsync(employee.User);
            var currentRole = currentRoles.FirstOrDefault();

            // 3. Map Employee data to the ViewModel
            var viewModel = new EditEmployeeViewModel // Using EditEmployeeViewModel from AdminController1 for better data handling
            {
                EmployeeId = employee.Id, // Renamed from 'Id' to 'EmployeeId' for clarity
                ApplicationUserId = employee.ApplicationUserId,
                FullName = employee.User.FullName,
                Email = employee.User.Email,
                PhoneNumber = employee.PhoneNumber,
                IsActive = employee.IsActive,

                // Find the current role
                SelectedRole = currentRole ?? employee.Position,

                // 4. Populate Roles dropdown
                Roles = await GetAvailableRolesAsync(),
            };

            ViewData["Title"] = $"Edit Employee: {employee.User.FullName}";
            // Note: This assumes you have a view for EditEmployeeViewModel
            // If you only have a view for RegisterEmployeeViewModel, you will need to map back
            return View(viewModel);
        }

        // POST: Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditEmployeeViewModel model) // Using EditEmployeeViewModel
        {
            if (id != model.EmployeeId)
            {
                return NotFound();
            }

            // 1. Re-populate roles on error and return
            if (!ModelState.IsValid)
            {
                model.Roles = await GetAvailableRolesAsync();
                return View(model);
            }

            // 2. Fetch both entities
            var employee = await context.Employees.FindAsync(id);
            var user = await userManager.FindByIdAsync(model.ApplicationUserId);

            if (employee == null || user == null)
            {
                TempData["ErrorMessage"] = "Employee or User record not found.";
                return RedirectToAction(nameof(Employees));
            }

            try
            {
                // 3. Update ApplicationUser fields
                user.FullName = model.FullName;
                user.Email = model.Email; // Allow email/username update
                user.UserName = model.Email;

                await userManager.UpdateAsync(user);

                // 4. Update Employee entity fields
                employee.Position = model.SelectedRole;
                employee.PhoneNumber = model.PhoneNumber;
                employee.IsActive = model.IsActive; // Updated IsActive state

                // Handle lockout based on IsActive state
                if (model.IsActive)
                {
                    // Remove lockout if reactivating
                    await userManager.SetLockoutEndDateAsync(user, null);
                }
                else
                {
                    // Set lockout if deactivating
                    await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
                }

                context.Update(employee);
                await context.SaveChangesAsync();

                // 5. Update User Role (only if it has changed)
                var currentRoles = await userManager.GetRolesAsync(user);

                if (currentRoles.FirstOrDefault() != model.SelectedRole)
                {
                    if (currentRoles.Any())
                    {
                        // Remove existing role(s)
                        await userManager.RemoveFromRolesAsync(user, currentRoles);
                    }
                    // Add the new role
                    var roleResult = await userManager.AddToRoleAsync(user, model.SelectedRole);

                    if (!roleResult.Succeeded)
                    {
                        TempData["ErrorMessage"] = $"Employee updated, but failed to change role: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}";
                        return RedirectToAction(nameof(Employees));
                    }
                }

                TempData["SuccessMessage"] = $"Employee details for '{model.FullName}' updated successfully. Status: {(model.IsActive ? "Active" : "Deactivated")}.";
                return RedirectToAction(nameof(Employees));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!context.Employees.Any(e => e.Id == id))
                {
                    return NotFound();
                }
                throw;
            }
        }

        // GET: Admin/Delete/5 (Employee Deactivate Confirmation View)
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

            // Map the Employee data to the RegisterEmployeeViewModel for display
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
        public async Task<IActionResult> DeactivateConfirmed(int id) // Using the explicit name from AdminController1
        {
            var employee = await context.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null || employee.User == null)
            {
                TempData["ErrorMessage"] = "Error: Employee not found. Deactivation failed.";
                return RedirectToAction("Employees");
            }

            var user = employee.User;

            // Prevent deactivation of Admin/Customer/Supplier roles
            if (await userManager.IsInRoleAsync(user, "Admin") ||
                await userManager.IsInRoleAsync(user, "Customer") ||
                await userManager.IsInRoleAsync(user, "Supplier"))
            {
                TempData["ErrorMessage"] = "Cannot deactivate employees with Admin, Customer, or Supplier roles.";
                return RedirectToAction("Employees");
            }

            // 1. Remove user from all roles (optional security added from AdminController1)
            var roles = await userManager.GetRolesAsync(user);
            if (roles.Any())
            {
                await userManager.RemoveFromRolesAsync(user, roles);
            }

            // 2. Perform Soft Delete
            employee.IsActive = false;
            context.Employees.Update(employee);

            // 3. Lock the ApplicationUser account to prevent login
            await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
            context.Users.Update(user);

            await context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Employee '{employee.User.FullName}' successfully deactivated.";
            return RedirectToAction("Employees");
        }

        // POST: Admin/Reactivate/5 - Employee Reactivation (Reverse Soft Delete)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reactivate(int id)
        {
            var employee = await context.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null || employee.User == null)
            {
                TempData["ErrorMessage"] = "Employee not found.";
                return RedirectToAction(nameof(InactiveEmployees));
            }

            var user = employee.User;

            // 1. Reverse the soft delete
            employee.IsActive = true;

            // 2. Remove the lockout (Re-enables login)
            await userManager.SetLockoutEndDateAsync(user, null);

            // 3. Re-assign the employee's role
            var roleName = employee.Position;
            if (!string.IsNullOrEmpty(roleName) && await roleManager.RoleExistsAsync(roleName))
            {
                // Ensure they don't have a role before adding
                var currentRoles = await userManager.GetRolesAsync(user);
                if (!currentRoles.Contains(roleName))
                {
                    await userManager.AddToRoleAsync(user, roleName);
                }
            }
            else
            {
                TempData["WarningMessage"] = $"Employee '{user.Email}' reactivated, but failed to re-assign role: '{roleName}'. Please assign a role manually.";
            }

            context.Update(employee);
            await context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Employee '{user.Email}' has been successfully REACTIVATED. Please confirm their role assignment.";
            return RedirectToAction(nameof(Employees)); // Redirect to the active list
        }

        // Helper method to get roles, updated to exclude "Supplier" role as per original file
        private async Task<List<SelectListItem>> GetAvailableRolesAsync()
        {
            // Filter out roles that are not intended for regular staff (like "Admin", "Customer", and "Supplier")
            return await roleManager.Roles
                .Where(r => r.Name != "Admin" && r.Name != "Customer" && r.Name != "Supplier")
                .Select(r => new SelectListItem
                {
                    Value = r.Name,
                    Text = r.Name
                })
                .ToListAsync();
        }

        //---------------------------------------------------------
        // CUSTOMER MANAGEMENT
        //---------------------------------------------------------

        // Customers List (Active Customers)
        public async Task<IActionResult> Customers()
        {
            // Only fetch active customers for the main list
            var customers = await context.Customers
                .Include(c => c.User)
                .Include(c => c.Location)
                .Where(c => c.IsActive == true)
                .ToListAsync();

            ViewBag.CustomerCount = customers.Count;
            ViewData["ListTitle"] = "Active Customers"; // Added for clarity in the view

            // Add the list of locations to ViewBag for the 'Register' button link
            ViewBag.Locations = await context.Locations
                .Select(l => new SelectListItem
                {
                    Value = l.Id.ToString(),
                    Text = l.Description // PLACHOLDER FIX: Use the actual property name here
                })
                .ToListAsync();

            return View(customers); // Pass the list of Customer models to the view
        }

        // Inactive Customers List
        public async Task<IActionResult> InactiveCustomers()
        {
            var customers = await context.Customers
                .Include(c => c.User)
                .Include(c => c.Location)
                .Where(c => !c.IsActive) // Filter for Inactive customers
                .ToListAsync();

            ViewBag.CustomerCount = customers.Count;
            ViewData["ListTitle"] = "Inactive Customers (Deactivated)";
            // Re-use the Customers.cshtml view but with a different list
            return View("Customers", customers);
        }

        // GET: Admin/CreateCustomer
        public async Task<IActionResult> CreateCustomer()
        {
            var model = new RegisterCustomerViewModel();

            // Fetch Locations for the dropdown
            model.AvailableLocations = await context.Locations
                .Select(l => new SelectListItem
                {
                    Value = l.Id.ToString(),
                    Text = l.Description // PLACHOLDER FIX: Use the actual property name here
                })
                .ToListAsync();

            ViewData["Title"] = "Register New Customer";
            return View(model);
        }

        // POST: Admin/CreateCustomer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCustomer(RegisterCustomerViewModel model)
        {
            // Re-populate Locations list on error
            if (!ModelState.IsValid)
            {
                model.AvailableLocations = await context.Locations
                    .Select(l => new SelectListItem
                    {
                        Value = l.Id.ToString(),
                        Text = l.Description // PLACHOLDER FIX: Use the actual property name here
                    })
                    .ToListAsync();
                return View(model);
            }

            // 2. Create the Identity User (Customer Role)
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.ContactName,
                PhoneNumber = model.Phone,
                EmailConfirmed = true
            };

            const string customerRole = "Customer";

            var userCreationResult = await userManager.CreateAsync(user, model.Password);

            if (userCreationResult.Succeeded)
            {
                // 3. Assign the Customer role
                await userManager.AddToRoleAsync(user, customerRole);

                // 4. Create the Customer Entity
                var customer = new Customer
                {
                    UserId = user.Id,
                    BusinessName = model.BusinessName,
                    ContactName = model.ContactName,
                    Email = model.Email,
                    Phone = model.Phone,
                    BusinessType = model.BusinessType,
                    PhysicalAddress = model.PhysicalAddress,
                    LocationId = model.SelectedLocationId,
                    IsActive = true,
                };

                context.Customers.Add(customer);
                await context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Customer '{model.BusinessName}' registered successfully with login: {model.Email}.";
                return RedirectToAction(nameof(Customers));
            }

            // Handle user creation failure
            foreach (var error in userCreationResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            // Re-populate locations and return view on error
            model.AvailableLocations = await context.Locations
                .Select(l => new SelectListItem
                {
                    Value = l.Id.ToString(),
                    Text = l.Description // PLACHOLDER FIX: Use the actual property name here
                })
                .ToListAsync();

            return View(model);
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

        // GET: Admin/CreateRole (NEW ACTION from AdminController1)
        [HttpGet]
        public IActionResult CreateRole()
        {
            ViewData["Title"] = "Create New Role";
            return View();
        }

        // POST: Admin/CreateRole (NEW ACTION from AdminController1)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRole(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
            {
                ModelState.AddModelError("", "Role name cannot be empty.");
                return View();
            }

            if (await roleManager.RoleExistsAsync(roleName))
            {
                ModelState.AddModelError("", "This role already exists.");
                return View();
            }

            var result = await roleManager.CreateAsync(new IdentityRole(roleName));

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"Role '{roleName}' created successfully.";
                return RedirectToAction("ManageRoles"); // point to your role listing page
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View();
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