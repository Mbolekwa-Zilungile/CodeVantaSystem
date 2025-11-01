using Code_Vanta_Systems.Data;
using Code_Vanta_Systems.Models;
using Code_Vanta_Systems.Models.Enums;
using Code_Vanta_Systems.ViewModels.FridgeRequests;
using Code_Vanta_Systems.ViewModels; // Add this for FridgeModelViewModel
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Code_Vanta_Systems.Controllers
{
    [Authorize(Roles = "Customer")]
    public class FridgeRequestController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public FridgeRequestController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        #region Fridge Requests (Rental Requests)
        // List all fridge rental requests
        [HttpGet]
        public async Task<IActionResult> Index(int? pageIndex, string? search)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);

                if (customer == null)
                {
                    TempData["ErrorMessage"] = "Customer profile not found.";
                    return RedirectToAction("Dashboard", "Customer");
                }

                var query = _context.RentalRequests
                    .Include(r => r.FridgeModel)
                    .Include(r => r.RequestedByUser)
                    .Where(r => r.CustomerId == customer.Id) // ✅ Now CustomerId exists
                    .AsQueryable();

                if (!string.IsNullOrEmpty(search))
                {
                    search = search.ToLower();
                    query = query.Where(r =>
                        r.FridgeModel.Name.ToLower().Contains(search) ||
                        r.Status.ToLower().Contains(search) ||
                        (r.SpecialRequirements != null && r.SpecialRequirements.ToLower().Contains(search))
                    );
                }

                query = query.OrderByDescending(r => r.CreatedAt);

                // Pagination
                pageIndex ??= 1;
                int pageSize = 10;
                int totalRecords = await query.CountAsync();
                int totalPages = (int)Math.Ceiling((decimal)totalRecords / pageSize);

                var requests = await query
                    .Skip((pageIndex.Value - 1) * pageSize)
                    .Take(pageSize)
                    .Select(r => new RentalRequestViewModel
                    {
                        Id = r.Id,
                        FridgeModelId = r.FridgeModelId,
                        FridgeModelName = r.FridgeModel.Name,
                        FridgeModelCategory = r.FridgeModel.Category,
                        Quantity = r.Quantity,
                        RequestedDuration = r.RequestedDuration, // ✅ Now exists
                        SpecialRequirements = r.SpecialRequirements, // ✅ Now exists
                        Status = r.Status,
                        IsPaid = r.IsPaid,
                        CreatedAt = r.CreatedAt,
                        UpdatedAt = r.UpdatedAt
                    })
                    .ToListAsync();

                ViewData["currentPage"] = pageIndex.Value;
                ViewData["totalPages"] = totalPages;
                ViewData["search"] = search ?? "";

                return View(requests);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error loading fridge requests";
                return RedirectToAction("Dashboard", "Customer");
            }
        }

        // Browse available fridge models
        [HttpGet]
        public async Task<IActionResult> BrowseFridges(int? category, string? search)
        {
            try
            {
                var query = _context.FridgeModels
                    .Include(fm => fm.Supplier)
                    .AsQueryable();

                // Filter by category
                if (category.HasValue)
                {
                    var categoryEnum = (FridgeCategory)category.Value;
                    query = query.Where(fm => fm.Category == categoryEnum);
                }

                // Search by model name
                if (!string.IsNullOrEmpty(search))
                {
                    search = search.ToLower();
                    query = query.Where(fm => fm.Name.ToLower().Contains(search));
                }

                // ✅ Use fully qualified name to resolve ambiguity
                var fridgeModels = await query
                    .Select(fm => new Code_Vanta_Systems.ViewModels.FridgeRequests.FridgeModelCatalogViewModel                    {
                        Id = fm.Id,
                        Name = fm.Name,
                        Category = fm.Category,
                        RentalPricePerMonth = fm.RentalPricePerMonth,
                        Description = fm.Description,
                        ImageUrl = fm.ImageUrl,
                        SupplierName = fm.Supplier.Name,
                        AvailableUnits = _context.FridgeUnits.Count(fu => fu.FridgeModelId == fm.Id && fu.Status == FridgeStatus.InWarehouse)
                    })
                    .Where(fm => fm.AvailableUnits > 0) // Only show models with available units
                    .ToListAsync();

                ViewBag.Categories = Enum.GetValues(typeof(FridgeCategory))
                    .Cast<FridgeCategory>()
                    .Select(c => new { Id = (int)c, Name = c.ToString() })
                    .ToList();

                ViewBag.SelectedCategory = category;
                ViewBag.SearchTerm = search;

                return View(fridgeModels);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error loading available fridges";
                return RedirectToAction("Dashboard", "Customer");
            }
        }

        [HttpGet]
        public async Task<IActionResult> CreateRequest(int fridgeModelId)
        {
            try
            {
                var fridgeModel = await _context.FridgeModels
                    .FirstOrDefaultAsync(fm => fm.Id == fridgeModelId);

                if (fridgeModel == null)
                {
                    TempData["ErrorMessage"] = "Fridge model not found.";
                    return RedirectToAction("BrowseFridges");
                }

                var availableUnits = await _context.FridgeUnits
                    .CountAsync(fu => fu.FridgeModelId == fridgeModelId && fu.Status == FridgeStatus.InWarehouse);

                if (availableUnits == 0)
                {
                    TempData["ErrorMessage"] = "No available units for this fridge model.";
                    return RedirectToAction("BrowseFridges");
                }

                var model = new CreateRentalRequestViewModel
                {
                    FridgeModelId = fridgeModelId,
                    FridgeModelName = fridgeModel.Name,
                    FridgeModelCategory = fridgeModel.Category,
                    RentalPricePerMonth = fridgeModel.RentalPricePerMonth,
                    MaxQuantity = availableUnits,
                    AvailableUnits = availableUnits
                };

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error loading request form";
                return RedirectToAction("BrowseFridges");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRequest(CreateRentalRequestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Reload fridge model info
                var fridgeModel = await _context.FridgeModels.FindAsync(model.FridgeModelId);
                if (fridgeModel != null)
                {
                    model.FridgeModelName = fridgeModel.Name;
                    model.FridgeModelCategory = fridgeModel.Category;
                    model.RentalPricePerMonth = fridgeModel.RentalPricePerMonth;
                    model.MaxQuantity = await _context.FridgeUnits
                        .CountAsync(fu => fu.FridgeModelId == model.FridgeModelId && fu.Status == FridgeStatus.InWarehouse);
                    model.AvailableUnits = model.MaxQuantity;
                }
                return View(model);
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == user.Id);

                if (customer == null)
                {
                    TempData["ErrorMessage"] = "Customer profile not found.";
                    return RedirectToAction("Dashboard", "Customer");
                }

                // Check available units
                var availableUnits = await _context.FridgeUnits
                    .CountAsync(fu => fu.FridgeModelId == model.FridgeModelId && fu.Status == FridgeStatus.InWarehouse);

                if (availableUnits < model.Quantity)
                {
                    ModelState.AddModelError("Quantity", $"Only {availableUnits} units available.");
                    var fridgeModel = await _context.FridgeModels.FindAsync(model.FridgeModelId);
                    if (fridgeModel != null)
                    {
                        model.FridgeModelName = fridgeModel.Name;
                        model.FridgeModelCategory = fridgeModel.Category;
                        model.RentalPricePerMonth = fridgeModel.RentalPricePerMonth;
                        model.MaxQuantity = availableUnits;
                        model.AvailableUnits = availableUnits;
                    }
                    return View(model);
                }

                var rentalRequest = new RentalRequest
                {
                    FridgeModelId = model.FridgeModelId,
                    RequestedByUserId = user.Id,
                    CustomerId = customer.Id, // ✅ Now CustomerId exists
                    Quantity = model.Quantity,
                    RequestedDuration = model.RequestedDuration, // ✅ Now exists
                    SpecialRequirements = model.SpecialRequirements, // ✅ Now exists
                    Status = "Pending",
                    IsPaid = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.RentalRequests.Add(rentalRequest);
                await _context.SaveChangesAsync();

                // Notify Customer Liaisons
                var customerLiaisons = await _userManager.GetUsersInRoleAsync("Customer Liaison");
                foreach (var liaison in customerLiaisons)
                {
                    _context.Notifications.Add(new Notification
                    {
                        RecipientUserId = liaison.Id,
                        Title = "New Rental Request",
                        Message = $"Customer '{customer.BusinessName}' submitted a new rental request for {model.Quantity} x {model.FridgeModelName}.",
                        CreatedAt = DateTime.UtcNow,
                        IsRead = false,
                        TargetController = "CustomerLiaison",
                        TargetAction = "Requests"
                    });
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Rental request submitted successfully! You will be notified when it's approved.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error creating rental request.";
                return RedirectToAction("BrowseFridges");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelRequest(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);

                var request = await _context.RentalRequests
                    .FirstOrDefaultAsync(r => r.Id == id && r.CustomerId == customer.Id && r.Status == "Pending"); // ✅ Now CustomerId exists

                if (request == null)
                {
                    return Json(new { success = false, message = "Request not found or cannot be cancelled." });
                }

                request.Status = "Cancelled";
                request.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Request cancelled successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error cancelling request." });
            }
        }
        #endregion

        #region Customer Dashboard & Allocations
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);

                if (customer == null)
                {
                    TempData["ErrorMessage"] = "Customer profile not found.";
                    return RedirectToAction("Index", "Home");
                }

                // Get statistics
                var pendingRequests = await _context.RentalRequests
                    .CountAsync(r => r.CustomerId == customer.Id && r.Status == "Pending"); // ✅ Now CustomerId exists

                var approvedRequests = await _context.RentalRequests
                    .CountAsync(r => r.CustomerId == customer.Id && r.Status == "Approved"); // ✅ Now CustomerId exists

                var activeAllocations = await _context.FridgeAllocations
                    .Include(a => a.FridgeUnit)
                    .ThenInclude(fu => fu.FridgeModel)
                    .CountAsync(a => a.CustomerId == customer.Id && a.DeallocationDate == null);

                var totalMonthlyRental = await _context.FridgeAllocations
                    .Include(a => a.FridgeUnit)
                    .ThenInclude(fu => fu.FridgeModel)
                    .Where(a => a.CustomerId == customer.Id && a.DeallocationDate == null)
                    .SumAsync(a => a.FridgeUnit.FridgeModel.RentalPricePerMonth);

                ViewBag.PendingRequests = pendingRequests;
                ViewBag.ApprovedRequests = approvedRequests;
                ViewBag.ActiveAllocations = activeAllocations;
                ViewBag.TotalMonthlyRental = totalMonthlyRental;
                ViewBag.CustomerName = customer.BusinessName;
                ViewBag.ContactName = customer.ContactName;

                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error loading dashboard";
                return RedirectToAction("Index", "Home");
            }
        }

        public async Task<IActionResult> MyAllocations()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);

                if (customer == null)
                {
                    TempData["ErrorMessage"] = "Customer profile not found.";
                    return RedirectToAction("Dashboard");
                }

                var allocations = await _context.FridgeAllocations
                    .Include(a => a.FridgeUnit)
                    .ThenInclude(fu => fu.FridgeModel)
                    .Include(a => a.FridgeUnit)
                    .ThenInclude(fu => fu.Location)
                    .Where(a => a.CustomerId == customer.Id)
                    .OrderByDescending(a => a.AllocationDate)
                    .Select(a => new CustomerAllocationViewModel // ✅ Use correct ViewModel
                    {
                        Id = a.Id,
                        FridgeModelName = a.FridgeUnit.FridgeModel.Name,
                        SerialNumber = a.FridgeUnit.SerialNumber,
                        AllocationDate = a.AllocationDate,
                        DeallocationDate = a.DeallocationDate,
                        Status = a.FridgeUnit.Status.ToString(),
                        Location = a.FridgeUnit.Location.Description,
                        IsActive = a.DeallocationDate == null
                    })
                    .ToListAsync();

                return View(allocations);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error loading allocations";
                return RedirectToAction("Dashboard");
            }
        }

        [HttpGet]
        public async Task<IActionResult> RequestDetails(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);

                var request = await _context.RentalRequests
                    .Include(r => r.FridgeModel)
                    .Include(r => r.RequestedByUser)
                    .FirstOrDefaultAsync(r => r.Id == id && r.CustomerId == customer.Id); // ✅ Now CustomerId exists

                if (request == null)
                {
                    TempData["ErrorMessage"] = "Request not found.";
                    return RedirectToAction(nameof(Index));
                }

                var model = new RentalRequestDetailsViewModel
                {
                    Id = request.Id,
                    FridgeModelName = request.FridgeModel.Name,
                    FridgeModelCategory = request.FridgeModel.Category,
                    Quantity = request.Quantity,
                    RequestedDuration = request.RequestedDuration, // ✅ Now exists
                    SpecialRequirements = request.SpecialRequirements, // ✅ Now exists
                    Status = request.Status,
                    IsPaid = request.IsPaid,
                    CreatedAt = request.CreatedAt,
                    UpdatedAt = request.UpdatedAt,
                    MonthlyCost = request.Quantity * request.FridgeModel.RentalPricePerMonth
                };

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error loading request details";
                return RedirectToAction(nameof(Index));
            }
        }
        #endregion

        #region Helper Methods
        private async Task<int> GetAvailableUnitsCount(int fridgeModelId)
        {
            return await _context.FridgeUnits
                .CountAsync(fu => fu.FridgeModelId == fridgeModelId && fu.Status == FridgeStatus.InWarehouse);
        }

        private async Task<Customer> GetCurrentCustomer()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
        }
        #endregion
    }
}