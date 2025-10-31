using Code_Vanta_Systems.Models;
using Code_Vanta_Systems.ViewModels.EmployeeProfile;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Code_Vanta_Systems.Controllers
{
    public class EmployeeProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public EmployeeProfileController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // GET: EmployeeProfile
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var model = new EmployeeProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber
            };

            return View(model);
        }

        // GET: EmployeeProfile/Edit
        public async Task<IActionResult> Edit()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var model = new EmployeeProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber
            };

            return View(model);
        }

        // POST: EmployeeProfile/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EmployeeProfileViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // Update basic info
            user.FullName = model.FullName;
            user.Email = model.Email;
            user.UserName = model.Email;
            user.PhoneNumber = model.PhoneNumber;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return View(model);
            }

            // Change password if provided
            if (!string.IsNullOrEmpty(model.CurrentPassword) && !string.IsNullOrEmpty(model.NewPassword))
            {
                var passwordChangeResult = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
                if (!passwordChangeResult.Succeeded)
                {
                    foreach (var error in passwordChangeResult.Errors)
                        ModelState.AddModelError(string.Empty, error.Description);
                    return View(model);
                }
            }

            await _signInManager.RefreshSignInAsync(user); // refresh login info
            TempData["SuccessMessage"] = "Profile updated successfully!";
            return RedirectToAction(nameof(Index));
        }
    }
}
