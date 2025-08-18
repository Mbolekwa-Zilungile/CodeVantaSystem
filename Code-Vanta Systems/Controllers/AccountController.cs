using Code_Vanta_Systems.Models;
using Code_Vanta_Systems.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Code_Vanta_Systems.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly RoleManager<IdentityRole> roleManager;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            this.signInManager = signInManager;
            this.userManager = userManager;
            this.roleManager = roleManager;
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

            var result = await signInManager.PasswordSignInAsync(
                model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
                return RedirectToAction("Index", "Home");

            ModelState.AddModelError("", "Invalid login attempt.");
            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            ViewBag.Roles = roleManager.Roles
                .Select(r => new SelectListItem { Value = r.Name, Text = r.Name })
                .ToList();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModels model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.Name
                };

                var result = await userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Assign the chosen role
                    await userManager.AddToRoleAsync(user, model.Role);

                    await signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Home");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // Repopulate roles if model validation fails
            ViewBag.Roles = roleManager.Roles
                .Select(r => new SelectListItem { Value = r.Name, Text = r.Name })
                .ToList();

            return View(model);
        }
    }

}
