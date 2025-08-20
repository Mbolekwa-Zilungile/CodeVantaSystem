using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Code_Vanta_Systems.Controllers
{
    [Authorize(Roles = "Service Technician")]
    public class ServiceTechnicianController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
