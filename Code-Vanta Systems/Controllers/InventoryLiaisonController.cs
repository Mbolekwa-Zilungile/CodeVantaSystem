using Code_Vanta_Systems.Data;
using Code_Vanta_Systems.Helper;
using Code_Vanta_Systems.Models;
using Code_Vanta_Systems.Models.Enums;
using Code_Vanta_Systems.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Code_Vanta_Systems.Controllers
{
    [Authorize(Roles = "Inventory Liaison")]
    public class InventoryLiaisonController : Controller
    {

        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;

        public InventoryLiaisonController(ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }


        // Dashboard..

        public async Task<IActionResult> Dashboard()
        {
            var fridges = await _context.Fridges.ToListAsync();

            // Soft-deleted
            var deletedStock = fridges.Count(f => f.IsDeleted);

            // Active fridges
            var activeFridges = fridges.Where(f => !f.IsDeleted).ToList();
            var totalStock = activeFridges.Count;

            // Count by status
            var statusCounts = activeFridges
                .GroupBy(f => f.Status)
                .ToDictionary(g => g.Key, g => g.Count());

            // Group by category & model
            var groupedStock = activeFridges
                .GroupBy(f => new { f.Category, f.Model })
                .Select(g => new GroupedStockViewModel
                {
                    Category = g.Key.Category.ToString(),
                    Model = g.Key.Model,
                    Count = g.Count(),
                    StockLevel = g.Count() < 5 ? "Low" : g.Count() < 10 ? "Average" : "High"
                })
                .ToList();

            var model = new DashboardViewModel
            {
                TotalStock = totalStock,
                DeletedStock = deletedStock,
                StatusCounts = statusCounts,
                GroupedStock = groupedStock
            };

            return View(model);
        }

        // Example action for sending stock emails
        public IActionResult SendStockEmail(string category, string model)
        {
            // TODO: Implement email sending logic
            TempData["Message"] = $"Stock email sent for {category} - {model}!";
            return RedirectToAction("Dashboard");
        }

        public async Task<IActionResult> Index()
        {
            var fridges = await _context.Fridges
            .Where(f => !f.IsDeleted)
            .Include(f => f.Supplier)
            .Include(f => f.Location)
            .OrderByDescending(f => f.PurchaseDate)
            .ToListAsync();

            return View(fridges);
        }


        #region PURCHASE REQUESTS
        // 🗂️ Display all purchase requests created by this liaison

        public async Task<IActionResult> PurchaseRequests()
        {
            var purchaseRequests = await _context.PurchaseRequests
                .Include(pr => pr.RequestedBy)
                .Include(pr => pr.RequestedItems)
                .Where(pr => pr.Status == RequestStatus.Submitted)
                .OrderByDescending(pr => pr.RequestDate)
                .ToListAsync();
            ViewBag.SubmitedCount = purchaseRequests.Count;

            ViewBag.RejectedRequests = await _context.PurchaseRequests
                .CountAsync(s => s.Status == RequestStatus.Rejected);

            ViewBag.PendingRequestsCount = await _context.PurchaseRequests
                .CountAsync(s => s.Status == RequestStatus.Pending);

            ViewBag.CompletedRequestsCount = await _context.PurchaseRequests
                .CountAsync(s => s.Status == RequestStatus.Approved);
            return View(purchaseRequests);
        }

        public async Task<IActionResult> RejectedPurchaseRequests()
        {
            var purchaseRequests = await _context.PurchaseRequests
                .Include(pr => pr.RequestedBy)
                .Include(pr => pr.RequestedItems)
                .Where(pr => pr.Status == RequestStatus.Rejected)
                .OrderByDescending(pr => pr.RequestDate)
                .ToListAsync();

            return View(purchaseRequests);
        }

        // ✍️ GET: Create new purchase request
        [HttpGet]
        public async Task<IActionResult> Create()
        {
               var items = await _context.ItemCatalogs
                .Select(i => new SelectListItem { Value = i.Id.ToString(), Text = i.Name })
                .ToListAsync();

            var model = new CreatePurchaseRequestViewModel
            {
                Items = new List<PurchaseRequestItemViewModel>
        {
            new PurchaseRequestItemViewModel { AvailableItems = items }
        }
            };

            return View(model);
        }

        // ✍️ POST: Create new purchase request
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreatePurchaseRequestViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);

            // ✅ Validate priority
            if (model.Priority == null)
            {
                ModelState.AddModelError(nameof(model.Priority), "Priority is required.");
            }

            if (model.ExpectedDate.HasValue && model.ExpectedDate.Value.Date < DateTime.Now.Date)
            {
                ModelState.AddModelError("ExpectedDate", "Expected date cannot be in the past.");
            }

            // ✅ Validate at least one item is selected
            if (model.Items == null || !model.Items.Any(i => i.ItemCatalogId > 0))
            {
                ModelState.AddModelError("", "You must select at least one item.");
            }

            if (!ModelState.IsValid)
            {
                var allItems = await _context.ItemCatalogs
                    .Where(i => i.IsActive)
                    .Select(i => new SelectListItem { Value = i.Id.ToString(), Text = i.Name })
                    .ToListAsync();

                // Repopulate AvailableItems for all rows
                if (model.Items != null)
                {
                    foreach (var item in model.Items)
                    {
                        item.AvailableItems = allItems;
                    }
                }

                return View(model);
            }

            // 🛒 Create purchase request
            var request = new PurchaseRequest
            {
                RequestedByUserId = user.Id,
                RequestDate = DateTime.Now,
                ExpectedDate = model.ExpectedDate,
                Priority = model.Priority,
                Notes = model.Notes,
                Status=Models.Enums.RequestStatus.Submitted,
               // RequestedItems = new List<PurchaseRequestItem>() // 👈 IMPORTANT
            };

            // Add request first to generate ID
            _context.PurchaseRequests.Add(request);
            await _context.SaveChangesAsync(); // now request.Id is available

            // 🛒 Map selected items and auto-assign description from catalog
            foreach (var item in model.Items.Where(i => i.ItemCatalogId > 0))
            {
                var catalogItem = await _context.ItemCatalogs.FindAsync(item.ItemCatalogId);
                if (catalogItem != null)
                {
                    var requestItem = new PurchaseRequestItem
                    {
                        PurchaseRequestId = request.Id, // explicitly set FK
                        ItemCatalogId = catalogItem.Id,
                        ModelRequested = catalogItem.Name,
                        Description = catalogItem.Description,
                        Quantity = item.Quantity
                    };
                    _context.PurchaseRequestItems.Add(requestItem);
                }
            }

            await _context.SaveChangesAsync();

            // 🔔 Notify Purchasing Manager
            var purchasingManager = (await _userManager.GetUsersInRoleAsync("PurchasingManager")).FirstOrDefault();
            if (purchasingManager != null)
            {
                _context.Notifications.Add(new Notification
                {
                    RecipientUserId = purchasingManager.Id,
                    Message = $"New purchase request #{request.Id} submitted by {user.UserName}.",
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Purchase request submitted successfully!";
            return RedirectToAction(nameof(Index));
        }

        #endregion


        #region CREATE FRIDGE

        private void PopulateDropdowns()
        {
            Console.WriteLine("PopulateDropdowns() called");

            ViewBag.Categories = Enum.GetValues(typeof(FridgeCategory))
                .Cast<FridgeCategory>()
                .Select(c => new SelectListItem { Value = c.ToString(), Text = GetDisplayName(c) });

            ViewBag.Suppliers = _context.Suppliers
                .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name });

            ViewBag.Locations = _context.Locations
                .Select(l => new SelectListItem { Value = l.Id.ToString(), Text = l.Description });

            ViewBag.Statuses = Enum.GetValues(typeof(FridgeStatus))
            .Cast<FridgeStatus>()
            .Select(s => new SelectListItem { Value = s.ToString(), Text = s.ToString() });

            // Debug: counts
            var categories = ViewBag.Categories as IEnumerable<SelectListItem>;
            var suppliers = ViewBag.Suppliers as IEnumerable<SelectListItem>;
            var locations = ViewBag.Locations as IEnumerable<SelectListItem>;
            var statuses = ViewBag.Statuses as IEnumerable<SelectListItem>;

            Console.WriteLine($"Categories Loaded: {categories?.Count() ?? 0}");
            Console.WriteLine($"Suppliers Loaded: {suppliers?.Count() ?? 0}");
            Console.WriteLine($"Locations Loaded: {locations?.Count() ?? 0}");
            Console.WriteLine($"Statuses Loaded: {statuses?.Count() ?? 0}");
        }

        // GET
        public IActionResult CreateFridge()
        {
            Console.WriteLine("GET CreateFridge called");
            PopulateDropdowns();
            return View();
        }

        // POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFridge(CreateFridgeViewModel newFridge)
        {
            Console.WriteLine("POST CreateFridge called");
            Console.WriteLine($"ModelState.IsValid: {ModelState.IsValid}");

            foreach (var key in Request.Form.Keys)
                Console.WriteLine($"{key} = {Request.Form[key]}");

            if (!ModelState.IsValid)
            {
                Console.WriteLine("ModelState invalid:");
                foreach (var key in ModelState.Keys)
                {
                    var state = ModelState[key];
                    foreach (var error in state.Errors)
                        Console.WriteLine($" - {key}: {error.ErrorMessage}");
                }

                PopulateDropdowns();
                return View(newFridge);
            }

            string imageUrl = null;
            if (newFridge.ImageFile != null)
            {
                Console.WriteLine($"Uploading image: {newFridge.ImageFile.FileName}");
                string uploadsFolder = Path.Combine(_environment.WebRootPath, "images/fridges");
                Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = Guid.NewGuid() + Path.GetExtension(newFridge.ImageFile.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await newFridge.ImageFile.CopyToAsync(fileStream);
                }

                imageUrl = "/images/fridges/" + uniqueFileName;
                Console.WriteLine($"Image saved to: {imageUrl}");
            }

            var fridge = new Fridge
            {
                Category = newFridge.Category,
                Model = newFridge.Model,
                SerialNumber = newFridge.SerialNumber,
                SupplierId = newFridge.SupplierId,
                LocationId = newFridge.LocationId,
                Status = FridgeStatus.Available,
                ImageUrl = imageUrl,
                PurchaseDate = newFridge.PurchaseDate,
                Description = newFridge.Description,
                RentalPricePerMonth = newFridge.RentalPricePerMonth,
                StockQuantity = newFridge.StockQuantity,
            };

            _context.Fridges.Add(fridge);
            await _context.SaveChangesAsync();

            Console.WriteLine("Fridge saved successfully");
            return RedirectToAction("Index");
        }

        // AJAX: Get Models by Category
        [HttpGet]
        public JsonResult GetModels(string category)
        {
            Console.WriteLine($"GetModels called for category: {category}");
            if (Enum.TryParse(category, out FridgeCategory parsedCategory))
            {
                var models = parsedCategory.GetModels(); // make sure GetModels() returns List<string>
                Console.WriteLine($"Returning {models.Count} models");
                return Json(models);
            }

            Console.WriteLine("Invalid category, returning empty list");
            return Json(new List<string>());
        }

        #endregion



        #region UPDATE FRIDGE

        // GET: Edit Fridge
        public async Task<IActionResult> EditFridge(int id)
        {
            Console.WriteLine($"=== GET EditFridge HIT === Id: {id}");

            var fridge = await _context.Fridges
                .FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);

            if (fridge == null)
            {
                Console.WriteLine("Fridge not found or deleted.");
                return NotFound();
            }

            var updateFridge = new EditFridgeViewModel
            {
                Id = fridge.Id,
                Category = fridge.Category,
                Model = fridge.Model,
                SerialNumber = fridge.SerialNumber,
                SupplierId = fridge.SupplierId,
                LocationId = fridge.LocationId,
                PurchaseDate = fridge.PurchaseDate,
                Status = fridge.Status,
                RentalPricePerMonth = fridge.RentalPricePerMonth,
                Description = fridge.Description,
                ExistingImageUrl = fridge.ImageUrl,
                StockQuantity = fridge.StockQuantity
            };

            Console.WriteLine($"Loaded Fridge: {fridge.Model} ({fridge.SerialNumber})");
            PopulateDropdowns();

            ViewBag.Statuses = Enum.GetValues(typeof(FridgeStatus))
                                   .Cast<FridgeStatus>()
                                   .Select(s => new SelectListItem { Value = s.ToString(), Text = s.ToString() });

            return View(updateFridge);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditFridge(EditFridgeViewModel updateFridge)
        {
            Console.WriteLine("=== POST EditFridge HIT ===");
            Console.WriteLine($"Id: {updateFridge.Id}, Model: {updateFridge.Model}, Category: {updateFridge.Category}");
            Console.WriteLine($"Serial: {updateFridge.SerialNumber}, Supplier: {updateFridge.SupplierId}, Location: {updateFridge.LocationId}");
            Console.WriteLine($"PurchaseDate: {updateFridge.PurchaseDate}, Status: {updateFridge.Status}, Rental: {updateFridge.RentalPricePerMonth}");
            Console.WriteLine($"Description: {updateFridge.Description}, ExistingImageUrl: {updateFridge.ExistingImageUrl}");

            if (!ModelState.IsValid)
            {
                PopulateDropdowns();
                ViewBag.Statuses = Enum.GetValues(typeof(FridgeStatus))
                                       .Cast<FridgeStatus>()
                                       .Select(s => new SelectListItem { Value = s.ToString(), Text = s.ToString() });
                Console.WriteLine("ModelState invalid, returning view...");
                return View(updateFridge);
            }

            var fridge = await _context.Fridges.FirstOrDefaultAsync(f => f.Id == updateFridge.Id && !f.IsDeleted);
            if (fridge == null) return NotFound();

            // Update Image if new file uploaded
            if (updateFridge.ImageFile != null)
            {
                // Upload new image
                string uploadsFolder = Path.Combine(_environment.WebRootPath, "images/fridges");
                Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = Guid.NewGuid() + Path.GetExtension(updateFridge.ImageFile.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await updateFridge.ImageFile.CopyToAsync(fileStream);
                }

                fridge.ImageUrl = "/images/fridges/" + uniqueFileName;
                Console.WriteLine("New image uploaded: " + fridge.ImageUrl);
            }
            else
            {
                // Keep existing image
                fridge.ImageUrl = updateFridge.ExistingImageUrl;
                Console.WriteLine("No new image, keeping existing: " + fridge.ImageUrl);
            }

            // Update all other fields
            fridge.Category = updateFridge.Category;
            fridge.Model = updateFridge.Model;
            fridge.SerialNumber = updateFridge.SerialNumber;
            fridge.SupplierId = updateFridge.SupplierId;
            fridge.LocationId = updateFridge.LocationId;
            fridge.PurchaseDate = updateFridge.PurchaseDate;
            fridge.Status = updateFridge.Status;
            fridge.RentalPricePerMonth = updateFridge.RentalPricePerMonth;
            fridge.Description = updateFridge.Description;
            fridge.StockQuantity = updateFridge.StockQuantity;

            await _context.SaveChangesAsync();
            Console.WriteLine("Fridge updated successfully.");

            return RedirectToAction("Index");
        }


        #endregion



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFridge(int id)
        {
            var fridge = await _context.Fridges.FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);
            if (fridge == null) return NotFound();

            fridge.IsDeleted = true;
            fridge.DeletedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }


        #region #Details Fridge
        public async Task<IActionResult> DetailsFridge(int id)
        {
            var fridge = await _context.Fridges
                .Include(f => f.Supplier)
                .Include(f => f.Location)
                .FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);

            if (fridge == null) return NotFound();

            return View(fridge);
        }




        #endregion

        #region Helpers

        private string GetDisplayName(FridgeCategory category)
        {
            var field = category.GetType().GetField(category.ToString());
            var attr = field.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.DisplayAttribute), false)
                .FirstOrDefault() as System.ComponentModel.DataAnnotations.DisplayAttribute;
            return attr?.Name ?? category.ToString();
        }

        #endregion






    }
}
