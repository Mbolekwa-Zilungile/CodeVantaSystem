using Code_Vanta_Systems.Data;
using Code_Vanta_Systems.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Code_Vanta_Systems.Controllers
{
    public class Notifications : Controller
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly ApplicationDbContext context;

        public Notifications(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            this.userManager = userManager;
            this.roleManager = roleManager;
            this.context = context;
        }
        public IActionResult Index()
        {
            return View();
        }
        //notifications

        // 📬 Show dashboard with new notifications
        //public async Task<IActionResult> Notifications()
        //{
        //    var user = await userManager.GetUserAsync(User);

        //    var notifications = await context.Notifications
        //        .Where(n => n.RecipientUserId == user.Id)
        //        .OrderByDescending(n => n.CreatedAt)
        //        .ToListAsync();

        //    var unreadCount = notifications.Count(n => !n.IsRead);

        //    ViewBag.UnreadNotifications = unreadCount;

        //    return View(notifications);
        //}

        // ✅ Mark notification as read
        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var notification = await context.Notifications.FindAsync(id);
            if (notification != null)
            {
                notification.IsRead = true;
                await context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Notifications));
        }
    }
}
