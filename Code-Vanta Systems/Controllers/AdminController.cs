using Code_Vanta_Systems.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Code_Vanta_Systems.ViewModels;

namespace Code_Vanta_Systems.Controllers
{
    public class AdminController : Controller
    {

        private readonly UserManager<ApplicationUser> userManager;
        private readonly RoleManager<IdentityRole> roleManager;

        public AdminController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            userManager = userManager;
            roleManager = roleManager;
        }

        public IActionResult ManageRoles()
        {
            var users = userManager.Users.ToList();
            var roles = roleManager.Roles.ToList();
            return View(new RoleManagementViewModel { Users = users, Roles = roles });
        }

        [HttpPost]
        public async Task<IActionResult> AssignRole(string userId, string role)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user != null && await roleManager.RoleExistsAsync(role))
            {
                await userManager.AddToRoleAsync(user, role);
            }

            return RedirectToAction("ManageRoles");
        }
    }
}
