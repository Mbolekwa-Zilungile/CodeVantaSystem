using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Code_Vanta_Systems.Controllers
{
    public class PurchasingManagerController : Controller
    {
        [Authorize(Roles = "Purchasing Manager")]
        public IActionResult Index()
        {
            return View();
        }
    }
}
