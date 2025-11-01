// File: Controllers/AdminController.cs

using Code_Vanta_Systems.Data;
using Code_Vanta_Systems.Models;
using Code_Vanta_Systems.Models.Enums;
using Code_Vanta_Systems.Services;
using Code_Vanta_Systems.ViewModels;
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

            // --- 4. Calculate Fridge Statistics ---
            var totalFridgeUnits = await context.FridgeUnits.CountAsync();
            var availableFridgeUnits = await context.FridgeUnits.CountAsync(fu => fu.Status == FridgeStatus.InWarehouse);
            var allocatedFridgeUnits = await context.FridgeUnits.CountAsync(fu => fu.Status == FridgeStatus.Allocated);
            var inRepairFridgeUnits = await context.FridgeUnits.CountAsync(fu => fu.Status == FridgeStatus.InRepair);
            var retiredFridgeUnits = await context.FridgeUnits.CountAsync(fu => fu.Status == FridgeStatus.Retired);

            // --- 5. Calculate Supplier Statistics ---
            var activeSuppliers = await context.Suppliers.CountAsync(s => s.IsActive);
            var inactiveSuppliers = await context.Suppliers.CountAsync(s => !s.IsActive);
            var totalSuppliers = await context.Suppliers.CountAsync();

            // --- 6. Calculate Rental Request Statistics ---
            var pendingRentalRequests = await context.RentalRequests.CountAsync(r => r.Status == "Pending");
            var approvedRentalRequests = await context.RentalRequests.CountAsync(r => r.Status == "Approved");
            var totalRentalRequests = await context.RentalRequests.CountAsync();

            // --- 7. Calculate Fault Statistics ---
            var totalFaults = await context.FridgeFaults.CountAsync();
            var openFaults = await context.FridgeFaults.CountAsync(f => f.Status == FaultStatus.Open);
            var inProgressFaults = await context.FridgeFaults.CountAsync(f => f.Status == FaultStatus.InProgress);
            var resolvedFaults = await context.FridgeFaults.CountAsync(f => f.Status == FaultStatus.Resolved);

            // 8. Populate ViewModel
            var viewModel = new AdminDashboardViewModel
            {
                EmployeeCount = activeEmployeeCount,
                ActiveCustomersCount = activeCustomers,
                InactiveCustomersCount = inactiveCustomers,
                TotalCustomers = totalCustomers,
                TotalSystemUsers = totalUsers,

                // Fridge statistics
                TotalFridgeUnits = totalFridgeUnits,
                AvailableFridgeUnits = availableFridgeUnits,
                AllocatedFridgeUnits = allocatedFridgeUnits,
                InRepairFridgeUnits = inRepairFridgeUnits,
                RetiredFridgeUnits = retiredFridgeUnits,

                // Supplier statistics
                ActiveSuppliersCount = activeSuppliers,
                InactiveSuppliersCount = inactiveSuppliers,
                TotalSuppliers = totalSuppliers,

                // Rental statistics
                PendingRentalRequests = pendingRentalRequests,
                ApprovedRentalRequests = approvedRentalRequests,
                TotalRentalRequests = totalRentalRequests,

                // Fault statistics
                TotalFaults = totalFaults,
                OpenFaults = openFaults,
                InProgressFaults = inProgressFaults,
                ResolvedFaults = resolvedFaults,

                // Legacy properties for backward compatibility
                TotalFridgesInInventory = totalFridgeUnits,
                OpenFaultsCount = openFaults,
                PendingMaintenanceCount = inRepairFridgeUnits
            };

            return View(viewModel);
        }
        // Action to redirect to the ManageRoles action for total users
        public IActionResult TotalUsers()
        {
            return RedirectToAction("ManageRoles");
        }

        #region Employees
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
                    activeStaff.Add(emp);
                }
                else
                {
                    inactiveCount++;
                }
            }

            ViewBag.EmployeeCount = activeCount;
            ViewBag.ActiveEmployeeCount = activeCount;
            ViewBag.InactiveEmployeeCount = inactiveCount;

            return View(activeStaff);
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
                    inactiveStaff.Add(emp);
                }
            }

            ViewBag.ActiveEmployeeCount = activeCount;
            ViewBag.InactiveEmployeeCount = inactiveCount;
            ViewBag.EmployeeCount = inactiveCount;

            return View(inactiveStaff);
        }

        // GET: Admin/Create (Employee Registration Form)
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var viewModel = new RegisterEmployeeViewModel
            {
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
            if (!ModelState.IsValid)
            {
                model.Roles = await GetAvailableRolesAsync();
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                EmailConfirmed = true,
                PhoneNumber = model.PhoneNumber
            };

            var userCreationResult = await userManager.CreateAsync(user, model.Password);

            if (userCreationResult.Succeeded)
            {
                var roleResult = await userManager.AddToRoleAsync(user, model.SelectedRole);

                if (roleResult.Succeeded)
                {
                    var employee = new Employee
                    {
                        ApplicationUserId = user.Id,
                        Position = model.SelectedRole,
                        PhoneNumber = model.PhoneNumber,
                        IsActive = true,
                    };

                    await context.Employees.AddAsync(employee);
                    await context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Employee '{model.FullName}' created and assigned the '{model.SelectedRole}' role.";
                    return RedirectToAction("Employees");
                }

                await userManager.DeleteAsync(user);
                ModelState.AddModelError(string.Empty, $"Error assigning role '{model.SelectedRole}'. User deleted. Please try again.");
            }
            else
            {
                foreach (var error in userCreationResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            model.Roles = await GetAvailableRolesAsync();
            return View(model);
        }

        // GET: Admin/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var employee = await context.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null || employee.User == null)
            {
                TempData["ErrorMessage"] = "Error: Employee not found.";
                return RedirectToAction("Employees");
            }

            var currentRoles = await userManager.GetRolesAsync(employee.User);
            var currentRole = currentRoles.FirstOrDefault();

            var viewModel = new EditEmployeeViewModel
            {
                EmployeeId = employee.Id,
                ApplicationUserId = employee.ApplicationUserId,
                FullName = employee.User.FullName,
                Email = employee.User.Email,
                PhoneNumber = employee.PhoneNumber,
                IsActive = employee.IsActive,
                SelectedRole = currentRole ?? employee.Position,
                Roles = await GetAvailableRolesAsync(),
            };

            ViewData["Title"] = $"Edit Employee: {employee.User.FullName}";
            return View(viewModel);
        }

        // POST: Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditEmployeeViewModel model)
        {
            if (id != model.EmployeeId)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                model.Roles = await GetAvailableRolesAsync();
                return View(model);
            }

            var employee = await context.Employees.FindAsync(id);
            var user = await userManager.FindByIdAsync(model.ApplicationUserId);

            if (employee == null || user == null)
            {
                TempData["ErrorMessage"] = "Employee or User record not found.";
                return RedirectToAction(nameof(Employees));
            }

            try
            {
                user.FullName = model.FullName;
                user.Email = model.Email;
                user.UserName = model.Email;

                await userManager.UpdateAsync(user);

                employee.Position = model.SelectedRole;
                employee.PhoneNumber = model.PhoneNumber;
                employee.IsActive = model.IsActive;

                if (model.IsActive)
                {
                    await userManager.SetLockoutEndDateAsync(user, null);
                }
                else
                {
                    await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
                }

                context.Update(employee);
                await context.SaveChangesAsync();

                var currentRoles = await userManager.GetRolesAsync(user);

                if (currentRoles.FirstOrDefault() != model.SelectedRole)
                {
                    if (currentRoles.Any())
                    {
                        await userManager.RemoveFromRolesAsync(user, currentRoles);
                    }
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

            var viewModel = new RegisterEmployeeViewModel
            {
                Id = employee.Id,
                FullName = employee.User?.FullName,
                Email = employee.User?.Email,
            };

            ViewData["Title"] = $"Deactivate Employee: {employee.User?.FullName}";
            return View(viewModel);
        }

        // POST: Admin/Delete/5 (Employee Soft Delete Execution)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateConfirmed(int id)
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

            if (await userManager.IsInRoleAsync(user, "Admin") ||
                await userManager.IsInRoleAsync(user, "Customer") ||
                await userManager.IsInRoleAsync(user, "Supplier"))
            {
                TempData["ErrorMessage"] = "Cannot deactivate employees with Admin, Customer, or Supplier roles.";
                return RedirectToAction("Employees");
            }

            var roles = await userManager.GetRolesAsync(user);
            if (roles.Any())
            {
                await userManager.RemoveFromRolesAsync(user, roles);
            }

            employee.IsActive = false;
            context.Employees.Update(employee);

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

            employee.IsActive = true;
            await userManager.SetLockoutEndDateAsync(user, null);

            var roleName = employee.Position;
            if (!string.IsNullOrEmpty(roleName) && await roleManager.RoleExistsAsync(roleName))
            {
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
            return RedirectToAction(nameof(Employees));
        }

        // Helper method to get roles, updated to exclude "Supplier" role as per original file
        private async Task<List<SelectListItem>> GetAvailableRolesAsync()
        {
            return await roleManager.Roles
                .Where(r => r.Name != "Admin" && r.Name != "Customer" && r.Name != "Supplier")
                .Select(r => new SelectListItem
                {
                    Value = r.Name,
                    Text = r.Name
                })
                .ToListAsync();
        }

        #endregion

        #region Customer
        //---------------------------------------------------------
        // CUSTOMER MANAGEMENT
        //---------------------------------------------------------

        // Customers List (Active Customers)
        public async Task<IActionResult> Customers()
        {
            var customers = await context.Customers
                .Include(c => c.User)
                .Include(c => c.Location)
                .Where(c => c.IsActive == true)
                .ToListAsync();

            ViewBag.CustomerCount = customers.Count;
            ViewData["ListTitle"] = "Active Customers";

            ViewBag.Locations = await context.Locations
                .Select(l => new SelectListItem
                {
                    Value = l.Id.ToString(),
                    Text = l.Description
                })
                .ToListAsync();

            return View(customers);
        }

        // Inactive Customers List
        public async Task<IActionResult> InactiveCustomers()
        {
            var customers = await context.Customers
                .Include(c => c.User)
                .Include(c => c.Location)
                .Where(c => !c.IsActive)
                .ToListAsync();

            ViewBag.CustomerCount = customers.Count;
            ViewData["ListTitle"] = "Inactive Customers (Deactivated)";
            return View("Customers", customers);
        }

        // GET: Admin/CreateCustomer
        public async Task<IActionResult> CreateCustomer()
        {
            var model = new RegisterCustomerViewModel();

            model.AvailableLocations = await context.Locations
                .Select(l => new SelectListItem
                {
                    Value = l.Id.ToString(),
                    Text = l.Description
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
            if (!ModelState.IsValid)
            {
                model.AvailableLocations = await context.Locations
                    .Select(l => new SelectListItem
                    {
                        Value = l.Id.ToString(),
                        Text = l.Description
                    })
                    .ToListAsync();
                return View(model);
            }

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
                await userManager.AddToRoleAsync(user, customerRole);

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

            foreach (var error in userCreationResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            model.AvailableLocations = await context.Locations
                .Select(l => new SelectListItem
                {
                    Value = l.Id.ToString(),
                    Text = l.Description
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

            var locations = await context.Locations
                .Select(l => new SelectListItem
                {
                    Value = l.Id.ToString(),
                    Text = $"{l.Description} ({l.City}, {l.Province})"
                })
                .ToListAsync();

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

            var customer = await context.Customers
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == model.Id);

            if (customer == null)
            {
                TempData["ErrorMessage"] = "Error: Customer not found for update.";
                return RedirectToAction("Customers");
            }

            customer.BusinessName = model.BusinessName;
            customer.ContactName = model.ContactName;
            customer.Phone = model.Phone;
            customer.Email = model.Email;
            customer.BusinessType = model.BusinessType;
            customer.PhysicalAddress = model.PhysicalAddress;
            customer.LocationId = model.LocationId;

            if (customer.User != null)
            {
                customer.User.FullName = model.ContactName;
                customer.User.Email = model.Email;
                customer.User.UserName = model.Email;
                context.Users.Update(customer.User);
            }

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
            return View(viewModel);
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

            customer.IsActive = false;
            context.Customers.Update(customer);

            if (customer.UserId != null)
            {
                var user = await userManager.FindByIdAsync(customer.UserId);
                if (user != null)
                {
                    await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
                    context.Users.Update(user);
                }
            }

            await context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Customer '{customer.BusinessName}' successfully deactivated (soft deleted).";
            return RedirectToAction("Customers");
        }

        #endregion

        #region Role
        //---------------------------------------------------------
        // ROLE MANAGEMENT
        //---------------------------------------------------------

        // Manage Roles - Lists all users and their roles
        public async Task<IActionResult> ManageRoles()
        {
            var users = await userManager.Users.ToListAsync();
            var roles = await roleManager.Roles.ToListAsync();
            var userRoles = new Dictionary<string, IList<string>>();

            foreach (var user in users)
            {
                var rolesForUser = await userManager.GetRolesAsync(user);
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
                return RedirectToAction("ManageRoles");
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
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Error: User not found.";
                return RedirectToAction("ManageRoles");
            }

            var currentRoles = await userManager.GetRolesAsync(user);
            var allRoles = await roleManager.Roles
                .Select(r => new SelectListItem
                {
                    Value = r.Name,
                    Text = r.Name
                })
                .ToListAsync();

            var viewModel = new RoleAssignmentViewModel
            {
                UserId = user.Id,
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

            var currentRoles = await userManager.GetRolesAsync(user);

            if (currentRoles.Any())
            {
                var removeResult = await userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                {
                    TempData["ErrorMessage"] = $"Failed to remove old roles: {string.Join(", ", removeResult.Errors.Select(e => e.Description))}";
                    return RedirectToAction("ManageRoles");
                }
            }

            var result = await userManager.AddToRoleAsync(user, model.SelectedRole);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"Successfully assigned '{model.SelectedRole}' role to {user.Email}.";
                return RedirectToAction("ManageRoles");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            model.AvailableRoles = await roleManager.Roles
                .Select(r => new SelectListItem
                {
                    Value = r.Name,
                    Text = r.Name
                })
                .ToListAsync();
            return View(model);
        }
        #endregion

        #region Supplier

        public async Task<IActionResult> Suppliers()
        {
            var activeSuppliers = await context.Suppliers.Include(s => s.User)
            .Where(s => s.IsActive)
            .ToListAsync();
            ViewBag.ActiveSupplierCount = activeSuppliers.Count;

            ViewBag.InactiveSuppliersCount = await context.Suppliers.CountAsync(s => !s.IsActive);
            return View(activeSuppliers);
        }

        public async Task<IActionResult> InactiveSuppliers()
        {
            var activeSuppliers = await context.Suppliers.Include(s => s.User)
            .Where(s => s.IsActive)
            .ToListAsync();
            ViewBag.ActiveSupplierCount = activeSuppliers.Count;

            var InactiveSuppliers = await context.Suppliers.Include(s => s.User)
           .Where(s => !s.IsActive)
           .ToListAsync();

            ViewBag.InactiveSuppliersCount = InactiveSuppliers.Count;
            return View(InactiveSuppliers);
        }

        [HttpGet]
        public IActionResult CreateSupplier()
        {
            ViewData["Title"] = "Create New Supplier";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSupplier(RegisterSupplierViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.ContactPerson,
                EmailConfirmed = true,
                PhoneNumber = model.Phone
            };

            var result = await userManager.CreateAsync(user, "Supplier@123!");

            if (result.Succeeded)
            {
                user.EmailConfirmed = true;
                await userManager.UpdateAsync(user);

                var roleExists = await context.Roles.AnyAsync(r => r.Name == "Supplier");
                if (!roleExists)
                {
                    ModelState.AddModelError(string.Empty, "Supplier role does not exist. Please create the role first.");
                    return View(model);
                }

                var roleResult = await userManager.AddToRoleAsync(user, "Supplier");

                if (roleResult.Succeeded)
                {
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
                    return RedirectToAction("Suppliers");
                }

                await userManager.DeleteAsync(user);
                ModelState.AddModelError(string.Empty, "Failed to assign 'Supplier' role. User has been deleted.");
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }
        #endregion

        #region Maintenance Oversight (Updated for your FridgeUnit model)

        [HttpGet]
        public async Task<IActionResult> MaintenanceOversight()
        {
            // 1. Get all Maintenance Technicians for the dropdown
            var maintenanceRole = await roleManager.FindByNameAsync("Maintenance Technician");
            var maintenanceTechnicians = maintenanceRole != null ?
                await userManager.GetUsersInRoleAsync(maintenanceRole.Name) :
                new List<ApplicationUser>();

            ViewBag.MaintenanceTechnicians = maintenanceTechnicians
                .Select(u => new SelectListItem
                {
                    Value = u.Id,
                    Text = u.FullName
                })
                .ToList();

            // 2. Query for fridge units that require maintenance AND are unassigned
            var fridgeUnitsNeedingAssignment = await context.FridgeUnits
                .Include(fu => fu.FridgeModel)
                .Include(fu => fu.Technician)
                .Include(fu => fu.CurrentCustomer)
                .Include(fu => fu.Location) // Include location for the new property
                .Where(fu =>
                    fu.Condition == FridgeCondition.RequireMaintenance &&
                    fu.TechnicianUserId == null &&
                    fu.Status == FridgeStatus.InRepair)
                .Select(fu => new MaintenanceAssignmentViewModel
                {
                    FridgeId = fu.Id,
                    SerialNumber = fu.SerialNumber,
                    Model = fu.FridgeModel.Name,
                    // Use CurrentCustomer for allocation info
                    CustomerBusinessName = fu.CurrentCustomer != null ? fu.CurrentCustomer.BusinessName : "N/A",
                    CustomerAddress = fu.CurrentCustomer != null ? fu.CurrentCustomer.PhysicalAddress : "N/A",
                    Condition = fu.Condition.ToString(),
                    AllocatedTechnicianId = fu.TechnicianUserId,
                    AllocatedTechnicianName = fu.Technician != null ? fu.Technician.FullName : "Unassigned",
                    TechnicianName = fu.Technician != null ? fu.Technician.FullName : "Unassigned",

                    // Populate the 3 new properties
                    FridgeModelName = fu.FridgeModel.Name,
                    Location = fu.Location != null ? fu.Location.Description : "N/A",
                    PurchaseDate = fu.PurchaseDate
                })
                .OrderBy(f => f.CustomerBusinessName)
                .ToListAsync();

            return View(fridgeUnitsNeedingAssignment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignMaintenanceTechnician(int fridgeId, string technicianId)
        {
            var fridgeUnit = await context.FridgeUnits.FindAsync(fridgeId);

            if (fridgeUnit == null)
            {
                TempData["ErrorMessage"] = "Error: Fridge unit not found.";
                return RedirectToAction(nameof(MaintenanceOversight));
            }

            fridgeUnit.TechnicianUserId = technicianId;
            fridgeUnit.Status = FridgeStatus.InRepair;

            try
            {
                context.Update(fridgeUnit);

                // Create audit trail
                var currentUser = await userManager.GetUserAsync(User);
                var audit = new AuditTrail
                {
                    EntityName = "FridgeUnit",
                    EntityId = fridgeUnit.Id,
                    ChangedByUserId = currentUser?.Id ?? "System",
                    ChangeType = "Maintenance Assignment",
                    ChangeDate = DateTime.Now,
                    ChangeDetails = $"Maintenance technician assigned to fridge unit {fridgeUnit.SerialNumber}."
                };
                context.AuditTrails.Add(audit);

                await context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Fridge unit {fridgeUnit.SerialNumber} successfully assigned to technician.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Assignment failed: {ex.Message}";
            }

            return RedirectToAction(nameof(MaintenanceOversight));
        }

        #endregion

        #region Fridge Management (Updated for your FridgeUnit model)

        [HttpGet]
        public async Task<IActionResult> FridgeUnits()
        {
            var fridgeUnits = await context.FridgeUnits
                .Include(fu => fu.FridgeModel)
                .Include(fu => fu.Technician)
                .Include(fu => fu.CurrentCustomer) // Include current customer
                .Include(fu => fu.Location)
                .OrderBy(fu => fu.Status)
                .ThenBy(fu => fu.SerialNumber)
                .ToListAsync();

            ViewBag.TotalFridgeUnits = await context.FridgeUnits.CountAsync();
            ViewBag.AvailableFridgeUnits = await context.FridgeUnits.CountAsync(fu => fu.Status == FridgeStatus.InWarehouse);
            ViewBag.AllocatedFridgeUnits = await context.FridgeUnits.CountAsync(fu => fu.Status == FridgeStatus.Allocated);
            ViewBag.InRepairFridgeUnits = await context.FridgeUnits.CountAsync(fu => fu.Status == FridgeStatus.InRepair);
            ViewBag.RetiredFridgeUnits = await context.FridgeUnits.CountAsync(fu => fu.Status == FridgeStatus.Retired);

            return View(fridgeUnits);
        }

        [HttpGet]
        public async Task<IActionResult> FridgeModels()
        {
            var fridgeModels = await context.FridgeModels
                .Include(fm => fm.Supplier)
                .Include(fm => fm.FridgeUnits)
                .ThenInclude(fu => fu.CurrentCustomer) // Include current customer for units
                .OrderBy(fm => fm.Name)
                .ToListAsync();

            ViewBag.TotalFridgeModels = fridgeModels.Count;
            ViewBag.TotalFridgeUnits = fridgeModels.Sum(fm => fm.FridgeUnits.Count);
            ViewBag.AvailableFridgeUnits = fridgeModels.Sum(fm => fm.FridgeUnits.Count(fu => fu.Status == FridgeStatus.InWarehouse));
            ViewBag.AllocatedFridgeUnits = fridgeModels.Sum(fm => fm.FridgeUnits.Count(fu => fu.Status == FridgeStatus.Allocated));

            return View(fridgeModels);
        }

        #endregion

        #region Additional Fridge Management Methods

        [HttpGet]
        public async Task<IActionResult> FridgeUnitDetails(int id)
        {
            var fridgeUnit = await context.FridgeUnits
                .Include(fu => fu.FridgeModel)
                .Include(fu => fu.Technician)
                .Include(fu => fu.CurrentCustomer)
                .Include(fu => fu.Location)
                .Include(fu => fu.Allocations)
                    .ThenInclude(a => a.Customer)
                .Include(fu => fu.Faults)
                .Include(fu => fu.Maintenances)
                .FirstOrDefaultAsync(fu => fu.Id == id);

            if (fridgeUnit == null)
            {
                TempData["ErrorMessage"] = "Fridge unit not found.";
                return RedirectToAction(nameof(FridgeUnits));
            }

            return View(fridgeUnit);
        }

        [HttpGet]
        public async Task<IActionResult> AvailableFridgeUnits()
        {
            var availableUnits = await context.FridgeUnits
                .Include(fu => fu.FridgeModel)
                .Include(fu => fu.Location)
                .Where(fu => fu.Status == FridgeStatus.InWarehouse && !fu.IsDeleted)
                .OrderBy(fu => fu.FridgeModel.Name)
                .ThenBy(fu => fu.SerialNumber)
                .ToListAsync();

            ViewBag.TotalAvailable = availableUnits.Count;
            return View(availableUnits);
        }

        [HttpGet]
        public async Task<IActionResult> AllocatedFridgeUnits()
        {
            var allocatedUnits = await context.FridgeUnits
                .Include(fu => fu.FridgeModel)
                .Include(fu => fu.CurrentCustomer)
                .Include(fu => fu.Location)
                .Where(fu => fu.Status == FridgeStatus.Allocated && !fu.IsDeleted)
                .OrderBy(fu => fu.CurrentCustomer.BusinessName)
                .ThenBy(fu => fu.SerialNumber)
                .ToListAsync();

            ViewBag.TotalAllocated = allocatedUnits.Count;
            return View(allocatedUnits);
        }

        #endregion
    }
}