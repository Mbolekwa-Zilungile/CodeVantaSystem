using Code_Vanta_Systems.Data;
using Code_Vanta_Systems.Services;
using Code_Vanta_Systems.Models;
using Code_Vanta_Systems.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Code_Vanta_Systems.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CustomerDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly UserManager<ApplicationUser> _userManager;

        public CustomerDashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            // Load the customer record linked to this user
            var customer = await _context.Customers
                .Include(c => c.Location)
                .FirstOrDefaultAsync(c => c.UserId == user.Id);

            if (customer == null) return NotFound("Customer profile not found.");

            return View(customer);
        }

        public async Task<IActionResult> PendingPage()
        {
            var userId = _userManager.GetUserId(User); // safer way to get user Id

            var customer = await _context.Customers
        .FirstOrDefaultAsync(c => c.UserId == userId &&
                                 (c.Status == CustomerStatus.Pending || c.Status == CustomerStatus.Rejected));

            if (customer == null)
            {
                // Either the customer is active or doesn't exist, redirect to dashboard
                return RedirectToAction("Index", "Home");
            }

            return View(customer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PendingPage(IFormFile document)
        {
            if (document == null || document.Length == 0)
            {
                TempData["Error"] = "Please select a file to upload.";
                return RedirectToAction("PendingPage"); // redirect ensures TempData survives
            }

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.UserId == userId &&
                                         (c.Status == CustomerStatus.Pending || c.Status == CustomerStatus.Rejected));

            if (customer == null)
            {
                TempData["Error"] = "No pending or rejected customer found.";
                return RedirectToAction("PendingPage");
            }

            // ✅ If the customer was rejected, reset status back to Pending
            if (customer.Status == CustomerStatus.Rejected)
            {
                customer.Status = CustomerStatus.Pending;
                TempData["Info"] = "Your account status has been reset to Pending for review.";
            }

            // 📁 Save the uploaded file
            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads/business_docs");
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{customer.Id}_{Path.GetFileName(document.FileName)}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await document.CopyToAsync(fileStream);
            }

            // 📄 Update the business document path
            customer.BusinessDocumentPath = $"/uploads/business_docs/{uniqueFileName}";
            _context.Customers.Update(customer);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Your business document has been uploaded successfully! You will be notified when your account is approved or rejected.";

            return RedirectToAction("PendingPage"); // redirect to show success message
        }


        public async Task<IActionResult> ActiveCustomers()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            // Load the customer record linked to this user
            var customer = await _context.Customers
                .Include(c => c.Location)
                .FirstOrDefaultAsync(c => c.UserId == user.Id);

            if (customer == null) return NotFound("Customer profile not found.");

            TempData["Success"] = "The customer has been approved";

            return View(customer);
        }
        // GET: CustomerDashboard/Rejected/5
        public async Task<IActionResult> Rejected()
        {
            var userId = _userManager.GetUserId(User);

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (customer == null)
                return NotFound();

            return View(customer);
        }


    }
}
