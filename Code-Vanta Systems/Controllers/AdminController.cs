using Code_Vanta_Systems.Data;
using Code_Vanta_Systems.Models;
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

        public AdminController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext context)
        {
            this.userManager = userManager;
            this.roleManager = roleManager;
            this.context = context;
        }

        public IActionResult Index()
        {
            return RedirectToAction("ManageRoles");
        }

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

        public IActionResult Create()
        {
            var model = new RegisterEmployeeViewModel
            {
                Roles = GetAvailableRoles()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RegisterEmployeeViewModel model)
        {
            Console.WriteLine("➡️ Create POST called"); // 👈 step 1

            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return Content("❌ ModelState invalid: " + errors);
            }

            var existingUser = await userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                Console.WriteLine("❌ User already exists with this email");
                model.Roles = GetAvailableRoles();
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
            };

            var result = await userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                Console.WriteLine("✅ User created successfully");

                await userManager.AddToRoleAsync(user, model.SelectedRole);
                Console.WriteLine($"✅ Role {model.SelectedRole} assigned");

                var employee = new Employee
                {
                    ApplicationUserId = user.Id,
                    Position = model.SelectedRole,
                    PhoneNumber = model.PhoneNumber,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    Role = Enum.TryParse<EmployeeRole>(model.SelectedRole, out var role)
               ? role
               : EmployeeRole.Technician // default if parsing fails
                };

                context.Employees.Add(employee);
                await context.SaveChangesAsync();
                Console.WriteLine("✅ Employee saved to database");

                return RedirectToAction("Employees");
            }

            foreach (var error in result.Errors)
            {
                Console.WriteLine($"❌ Identity error: {error.Description}");
                ModelState.AddModelError("", error.Description);
            }

            model.Roles = GetAvailableRoles();
            return View(model);
        }

        private List<SelectListItem> GetAvailableRoles()
        {
            return roleManager.Roles
                .Where(r => r.Name != "Customer" && r.Name != "Admin")
                .Select(r => new SelectListItem
                {
                    Value = r.Name,
                    Text = r.Name
                }).ToList();
        }

        public async Task<IActionResult> Employees()
        {
            var employees = await context.Employees
        .Include(e => e.User)
        .ToListAsync();

            // Filter out employees whose user is in Admin or Customer role
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

       public async Task<IActionResult>Edit(int id)
        {
            var employee = await context.Employees.Include(x => x.User).FirstOrDefaultAsync(x => x.Id == id);
            if (employee == null)
            {
                return NotFound();
            }
            var roles = await roleManager.Roles.Where(r => r.Name != "Customer").ToListAsync();
            var currentRole = await userManager.GetRolesAsync(employee.User);
            var viewModel = new RegisterEmployeeViewModel
            {
                Id = employee.Id,
                FullName = employee.User.FullName,
                Email = employee.User.Email,
                Role = currentRole.FirstOrDefault(),
                PhoneNumber = employee.PhoneNumber
            };

            ViewBag.Roles = roles;
            return View(viewModel);
        }

        // POST: Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
       public async Task<IActionResult>Edit(RegisterEmployeeViewModel model)
        {
            if(!ModelState.IsValid)
            {
                ViewBag.Roles = await roleManager.Roles.ToListAsync();
                return View(model);
            }
            var employee = await context.Employees.Include(e => e.User).FirstOrDefaultAsync(e => e.Id == model.Id);

            if(employee == null) return NotFound();

            //update ApplicationUser info
            employee.User.FullName = model.FullName;
            employee.User.Email = model.Email;
            employee.User.UserName = model.Email;

            //update role
            var currentRole = await userManager.GetRolesAsync(employee.User);

            if (currentRole.Any())
            {
                await userManager.RemoveFromRoleAsync(employee.User, currentRole.First() );
            }
                await userManager.AddToRoleAsync(employee.User,model.Role);

                context.Employees.Update(employee);
                await context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Employee details updated successfully";
            return RedirectToAction("Employees");
            

        }
    }
}




