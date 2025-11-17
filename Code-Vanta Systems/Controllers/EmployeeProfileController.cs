using Code_Vanta_Systems.Data;
using Code_Vanta_Systems.Models;
using Code_Vanta_Systems.ViewModels;
using Code_Vanta_Systems.ViewModels.EmployeeProfile;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Code_Vanta_Systems.Controllers
{
    public class EmployeeProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public EmployeeProfileController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        // GET: /EmployeeProfile/Index
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            var employee = await _context.Employees
                .Include(e => e.User)
                .Include(e => e.Location)
                .FirstOrDefaultAsync(e => e.ApplicationUserId == userId);

            if (employee == null) return NotFound();

            var model = new EditEmployeeViewModel
            {
                EmployeeId = employee.Id,
                ApplicationUserId = employee.ApplicationUserId,
                FullName = employee.User.FullName,
                Email = employee.User.Email,
                PhoneNumber = employee.User.PhoneNumber,
                SelectedRole = employee.Role.ToString(),
                Position = employee.Position,
                IsActive = employee.IsActive,
                LocationId = employee.LocationId,
                Roles = Enum.GetValues(typeof(EmployeeRole))
                            .Cast<EmployeeRole>()
                            .Select(r => new SelectListItem { Value = r.ToString(), Text = r.ToString() })
                            .ToList(),
                AvailableLocations = await _context.Locations
                            .Select(l => new SelectListItem { Value = l.Id.ToString(), Text = l.Description })
                            .ToListAsync()
            };

            return View(model);
        }

        // GET: /EmployeeProfile/Edit
        public async Task<IActionResult> Edit()
        {
            var userId = _userManager.GetUserId(User);

            var employee = await _context.Employees
                .Include(e => e.User)
                .Include(e => e.Location)
                .FirstOrDefaultAsync(e => e.ApplicationUserId == userId);

            if (employee == null) return NotFound();

            var model = new EditEmployeeProfile
            {
                EmployeeId = employee.Id,
                ApplicationUserId = employee.ApplicationUserId,
                FullName = employee.User.FullName,
                Email = employee.User.Email,
                PhoneNumber = employee.User.PhoneNumber,
               
            };

            return View(model);
        }

        // POST: /EmployeeProfile/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditEmployeeProfile model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var employee = await _context.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.Id == model.EmployeeId);

            if (employee == null) return NotFound();

            var user = employee.User;
            if (user == null) return NotFound();

            // Update Email properly through UserManager
            if (user.Email != model.Email)
            {
                var setEmailResult = await _userManager.SetEmailAsync(user, model.Email);
                if (!setEmailResult.Succeeded)
                {
                    foreach (var error in setEmailResult.Errors)
                        ModelState.AddModelError(string.Empty, error.Description);
                    return View(model);
                }

                // Optional: If username should match email
                var setUserNameResult = await _userManager.SetUserNameAsync(user, model.Email);
                if (!setUserNameResult.Succeeded)
                {
                    foreach (var error in setUserNameResult.Errors)
                        ModelState.AddModelError(string.Empty, error.Description);
                    return View(model);
                }
            }

            // Update other properties
            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;

            _context.Employees.Update(employee);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Your profile has been updated successfully!";
            return RedirectToAction("Index");
        }


        // GET: /EmployeeProfile/ChangePassword
        public IActionResult ChangePassword()
        {
            return View();
        }

        // POST: /EmployeeProfile/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return View(model);
            }

            await _signInManager.RefreshSignInAsync(user);
            TempData["SuccessMessage"] = "Password changed successfully!";
            return RedirectToAction("Index");
        }
    }
}
