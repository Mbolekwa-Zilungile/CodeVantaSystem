using Code_Vanta_Systems.Data;
using Code_Vanta_Systems.ViewModels;
using Code_Vanta_Systems.Models.Enums;
using Code_Vanta_Systems.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Code_Vanta_Systems.Controllers
{
    public class CustomerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly UserManager<ApplicationUser> _userManager;

        public CustomerController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment webHostEnvironment)
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
                return RedirectToAction("PendingPage");
            }

            var userId = _userManager.GetUserId(User);

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.UserId == userId &&
                                         (c.Status == CustomerStatus.Pending || c.Status == CustomerStatus.Rejected));

            if (customer == null)
            {
                TempData["Error"] = "No pending or rejected customer found.";
                return RedirectToAction("PendingPage");
            }

            // ✅ Reset status if rejected
            if (customer.Status == CustomerStatus.Rejected)
            {
                customer.Status = CustomerStatus.Pending;
                TempData["Info"] = "Your account status has been reset to Pending for review.";
            }

            // 📁 Save uploaded document
            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads/business_docs");
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{customer.Id}_{Path.GetFileName(document.FileName)}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await document.CopyToAsync(fileStream);
            }

            // 📄 Update customer record
            customer.BusinessDocumentPath = $"/uploads/business_docs/{uniqueFileName}";
            _context.Customers.Update(customer);
            await _context.SaveChangesAsync();

            // ✅ 1. Get the "Customer Liaison" user(s) properly
            var liaisonUsers = await _userManager.GetUsersInRoleAsync("Customer Liaison");
            var liaisonUser = liaisonUsers.FirstOrDefault();

            if (liaisonUser != null)
            {
                // ✅ 2. Create notification
                var notification = new Notification
                {
                    RecipientUserId = liaisonUser.Id,
                    Title = "New Customer registered",
                    Message = $"Customer '{customer.BusinessName}' has uploaded a new business document for verification.",
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false,
                    TargetController = "CustomerLiaison",
                    TargetAction = "PendingCustomers"
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Your business document has been uploaded successfully! You will be notified when your account is approved or rejected.";

            return RedirectToAction("PendingPage");
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



        public async Task<IActionResult> Rentals(string searchModel, int? category, int? status, int? supplier, int? location)
        {
            var fridgesQuery = _context.Fridges.AsQueryable().Where(f => !f.IsDeleted);

            if (!string.IsNullOrEmpty(searchModel))
                fridgesQuery = fridgesQuery.Where(f => f.Model.Contains(searchModel));

            if (category.HasValue)
                fridgesQuery = fridgesQuery.Where(f => (int)f.Category == category.Value);

            if (status.HasValue)
                fridgesQuery = fridgesQuery.Where(f => (int)f.Status == status.Value);

            if (supplier.HasValue)
                fridgesQuery = fridgesQuery.Where(f => f.SupplierId == supplier.Value);

            if (location.HasValue)
                fridgesQuery = fridgesQuery.Where(f => f.LocationId == location.Value);

            var model = await fridgesQuery
                .Select(f => new RentalViewModel
                {
                    Id = f.Id,
                    Model = f.Model,
                    SerialNumber = f.SerialNumber,
                    PricePerMonth = f.RentalPricePerMonth,
                    StockQuantity = f.StockQuantity,
                    Location = f.Description,
                    ImageUrl = f.ImageUrl
                })
                .ToListAsync();


            ViewBag.Categories = Enum.GetValues(typeof(FridgeCategory))
                                     .Cast<FridgeCategory>()
                                     .Select(c => new SelectListItem { Text = c.ToString(), Value = ((int)c).ToString() })
                                     .ToList();

            ViewBag.Statuses = Enum.GetValues(typeof(FridgeStatus))
                                   .Cast<FridgeStatus>()
                                   .Select(s => new SelectListItem { Text = s.ToString(), Value = ((int)s).ToString() })
                                   .ToList();

            ViewBag.Suppliers = await _context.Fridges
                                              .Select(f => f.Supplier)
                                              .Distinct()
                                              .Select(s => new SelectListItem { Text = s.Name, Value = s.Id.ToString() })
                                              .ToListAsync();

            ViewBag.Locations = await _context.Fridges
                                              .Select(f => f.Location)
                                              .Distinct()
                                              .Select(l => new SelectListItem { Text = l.Description, Value = l.Id.ToString() })
                                              .ToListAsync();

            return View(model);
        }

        // Submit rental request  
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout([FromBody] List<RentalViewModel> cartItems)
        {
            if (cartItems == null || !cartItems.Any())
                return Json(new { success = false, message = "Your cart is empty." });

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Json(new { success = false, message = "User not found." });

            foreach (var item in cartItems)
            {
                var rental = new RentalRequest
                {
                    FridgeId = item.Id,
                    Quantity = item.Quantity,
                    Status = "Pending",
                    RequestedByUserId = user.Id,
                    CreatedAt = DateTime.UtcNow
                };
                await _context.RentalRequests.AddAsync(rental);
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Rental request submitted! Waiting for approval." });
        }

        // List rental requests
        public async Task<IActionResult> MyRentalRequests()
        {
            var user = await _userManager.GetUserAsync(User);
            var rentals = await _context.RentalRequests
                .Include(r => r.Fridge)
                .Where(r => r.RequestedByUserId == user.Id)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(rentals);
        }

        // Active rented fridges  
        public async Task<IActionResult> MyRentedFridges()
        {
            var user = await _userManager.GetUserAsync(User);
            var rentals = await _context.RentalRequests
                .Include(r => r.Fridge)
                .Where(r => r.RequestedByUserId == user.Id && r.Status == "Approved")
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(rentals);
        }

    }
}
