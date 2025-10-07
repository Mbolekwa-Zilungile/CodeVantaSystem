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
            var PendingRequests = await _context.PurchaseRequests
                .Include(pr => pr.RequestedBy)
                .Include(pr => pr.RequestedItems)
                .Where(pr => pr.Status == Code_Vanta_Systems.Models.Enums.RequestStatus.Pending)
                .OrderByDescending(pr => pr.RequestDate)
                .ToListAsync();

            ViewBag.PendingCount = ViewBag.RejectedRequests = await _context.PurchaseRequests
               .CountAsync(s => s.Status == RequestStatus.Pending);
            ViewBag.SubmitedCount = ViewBag.RejectedRequests = await _context.PurchaseRequests
                .CountAsync(s => s.Status == RequestStatus.Submitted);

            ViewBag.RejectedRequests = await _context.PurchaseRequests
                .CountAsync(s => s.Status == RequestStatus.Rejected);

            ViewBag.ApprovedRequestsCount = await _context.PurchaseRequests
                .CountAsync(s => s.Status == RequestStatus.Approved);
            ViewBag.CompletedRequestsCount = await _context.PurchaseRequests
               .CountAsync(s => s.Status == RequestStatus.Completed);
            return View(PendingRequests);
        }

        public async Task<IActionResult> RejectedPurchaseRequests()
        {
            var purchaseRequests = await _context.PurchaseRequests
               .Include(pr => pr.RequestedBy)
               .Include(pr => pr.RequestedItems)
               .Where(pr => pr.Status == RequestStatus.Rejected)
               .OrderByDescending(pr => pr.RequestDate)
               .ToListAsync();

            ViewBag.PendingCount = ViewBag.RejectedRequests = await _context.PurchaseRequests
               .CountAsync(s => s.Status == RequestStatus.Pending);
            ViewBag.SubmitedCount = ViewBag.RejectedRequests = await _context.PurchaseRequests
                .CountAsync(s => s.Status == RequestStatus.Submitted);

            ViewBag.RejectedRequests = await _context.PurchaseRequests
                .CountAsync(s => s.Status == RequestStatus.Rejected);

            ViewBag.ApprovedRequestsCount = await _context.PurchaseRequests
                .CountAsync(s => s.Status == RequestStatus.Approved);
            ViewBag.CompletedRequestsCount = await _context.PurchaseRequests
               .CountAsync(s => s.Status == RequestStatus.Completed);
            return View(purchaseRequests);
        }
        public async Task<IActionResult> SubmitedPurchaseRequests()
        {
            var SubmitedRequests = await _context.PurchaseRequests
               .Include(pr => pr.RequestedBy)
               .Include(pr => pr.RequestedItems)
               .Where(pr => pr.Status == Code_Vanta_Systems.Models.Enums.RequestStatus.Submitted)
               .OrderByDescending(pr => pr.RequestDate)
               .ToListAsync();

            ViewBag.PendingCount = ViewBag.RejectedRequests = await _context.PurchaseRequests
               .CountAsync(s => s.Status == RequestStatus.Pending);
            ViewBag.SubmitedCount = ViewBag.RejectedRequests = await _context.PurchaseRequests
                .CountAsync(s => s.Status == RequestStatus.Submitted);

            ViewBag.RejectedRequests = await _context.PurchaseRequests
                .CountAsync(s => s.Status == RequestStatus.Rejected);

            ViewBag.ApprovedRequestsCount = await _context.PurchaseRequests
                .CountAsync(s => s.Status == RequestStatus.Approved);
            ViewBag.CompletedRequestsCount = await _context.PurchaseRequests
               .CountAsync(s => s.Status == RequestStatus.Completed);
            return View(SubmitedRequests);
        }


        [HttpGet]
        public async Task<IActionResult> Create()
        {
            // Load available fridges
            var fridges = await _context.Fridges
             .Where(f => !f.IsDeleted) // show all fridges, even 0 stock
             .Select(f => new SelectListItem
             {
                 Value = f.Id.ToString(),
                 Text = $"{f.Model} ({f.StockQuantity} in stock)"
             })
             .ToListAsync();


            var model = new CreatePurchaseRequestViewModel
            {
                Items = new List<PurchaseRequestItemViewModel>
        {
            new PurchaseRequestItemViewModel { AvailableItems = fridges }
        }
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreatePurchaseRequestViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);

            // Validate priority
            if (model.Priority == null)
            {
                ModelState.AddModelError(nameof(model.Priority), "Priority is required.");
            }

            if (model.ExpectedDate.HasValue && model.ExpectedDate.Value.Date < DateTime.Now.Date)
            {
                ModelState.AddModelError("ExpectedDate", "Expected date cannot be in the past.");
            }

            // Validate at least one fridge is selected
            if (model.Items == null || !model.Items.Any(i => i.FridgeId > 0))
            {
                ModelState.AddModelError("", "You must select at least one fridge.");
            }
            if (!ModelState.IsValid)
            {
                var allFridges = await _context.Fridges
                    .Where(f => !f.IsDeleted)
                    .Select(f => new SelectListItem { Value = f.Id.ToString(), Text = f.Model })
                    .ToListAsync();

                foreach (var item in model.Items)
                {
                    item.AvailableItems = allFridges;
                }

                model.AvailableItems = allFridges; // optional, if you use it in the view

                return View(model);
            }
            // Create purchase request
            var request = new PurchaseRequest
            {
                RequestedByUserId = user.Id,
                RequestDate = DateTime.Now,
                ExpectedDate = model.ExpectedDate,
                Priority = model.Priority,
                Notes = model.Notes,
                Status = Models.Enums.RequestStatus.Submitted
            };

            _context.PurchaseRequests.Add(request);
            await _context.SaveChangesAsync(); // request.Id is now available

            // Map selected fridges to request items
            foreach (var item in model.Items.Where(i => i.FridgeId > 0))
            {
                var fridge = await _context.Fridges.FindAsync(item.FridgeId);
                if (fridge != null)
                {
                    var requestItem = new PurchaseRequestItem
                    {
                        PurchaseRequestId = request.Id,
                        FridgeId = fridge.Id,
                        ModelRequested = fridge.Model,
                        Description = fridge.Description,
                        Quantity = item.Quantity
                    };
                    _context.PurchaseRequestItems.Add(requestItem);
                }
            }

            await _context.SaveChangesAsync();

            var purchasemanager = await _userManager.GetUsersInRoleAsync("Purchasing Manager");
            var liaisonUser = purchasemanager.FirstOrDefault();

            if (liaisonUser != null)
            {
                // ✅ 2. Create notification
                var notification = new Notification
                {
                    RecipientUserId = liaisonUser.Id,
                    Title = "New Purchase Request Sent",
                    Message = $"A new purchase request #{request.Id} was submitted by {User.Identity.Name}.",
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false,
                    TargetController = "PurchasingManager",
                    TargetAction = "PurchaseRequests"
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Purchase request submitted successfully!";
            return RedirectToAction(nameof(PurchaseRequests));
        }

        public async Task<IActionResult> Details(int id)
        {
            var request = await _context.PurchaseRequests
                         .Include(r => r.RequestedBy)
                         .Include(r => r.RequestedItems)
                         /*.ThenInclude(i => i.ItemCatalog) */// <--- This is correct for eager loading
                         .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null) return NotFound();

            return View(request);
        }

        [HttpGet]
        public async Task<IActionResult> ReEditRequest(int id)
        {
            // 1. Fetch the existing Purchase Request and its line items
            var request = await _context.PurchaseRequests
                .Include(r => r.RequestedItems)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
                return NotFound();

            // 2. Get all active fridges for dropdown lists
            var fridges = await _context.Fridges
                 .Where(f => !f.IsDeleted)
                 .Select(f => new SelectListItem
                 {
                     Value = f.Id.ToString(),
                     Text = $"{f.Model} ({f.StockQuantity} in stock)"
                 })
                 .ToListAsync();
            request.Notes = string.Empty;
            // 3. Map the entity to the view model
            var model = new EditPurchaseRequestViewModel
            {
                Id = request.Id,
                ExpectedDate = request.ExpectedDate,
                Priority = request.Priority,
                Status = RequestStatus.Pending,
                Notes = request.Notes,
                Items = request.RequestedItems.Select(i => new PurchaseRequestItemViewModel
                {
                    PurchaseRequestItemId = i.Id,
                    FridgeId = i.FridgeId,
                    Quantity = i.Quantity,
                    AvailableItems = fridges
                }).ToList()
            };

            model.Items.Add(new PurchaseRequestItemViewModel { AvailableItems = fridges });

            ViewBag.Fridges = fridges;
            return View(model);
        }

        // POST: Handle the form submission for editing
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReEditRequest(EditPurchaseRequestViewModel model)//Reedit purchase request
        {
            if (!ModelState.IsValid)
            { 
                var fridges = await _context.Fridges
                     .Where(f => !f.IsDeleted)
                     .Select(f => new SelectListItem
                     {
                         Value = f.Id.ToString(),
                         Text = $"{f.Model} ({f.StockQuantity} in stock)"
                     })
                     .ToListAsync();

                foreach (var item in model.Items)
                    item.AvailableItems = fridges;

                ViewBag.Fridges = fridges;
                return View(model);
            }

            var request = await _context.PurchaseRequests
                .Include(r => r.RequestedItems)
                .FirstOrDefaultAsync(r => r.Id == model.Id);

            if (request == null) return NotFound();

            // 3. Update main request fields
            request.ExpectedDate = model.ExpectedDate;
            request.Priority = model.Priority;
            request.Status = Code_Vanta_Systems.Models.Enums.RequestStatus.Pending; // Allows manager to potentially change status
            request.Notes = model.Notes;

            // 4. Manage line items (Delete, Update, Add)

            // Identify which existing items were removed from the form
            var itemIdsFromForm = model.Items
                .Where(i => i.PurchaseRequestItemId.HasValue)
                .Select(i => i.PurchaseRequestItemId.Value)
                .ToList();
            var itemsToRemove = request.RequestedItems
                .Where(i => !itemIdsFromForm.Contains(i.Id))
                .ToList();
            _context.PurchaseRequestItems.RemoveRange(itemsToRemove);

            // Process all items submitted in the form
            foreach (var itemVm in model.Items.Where(i => i.FridgeId > 0 && i.Quantity >= 1))
            {
                // Fetch the details of the selected Fridge (needed for ModelRequested/Description)
                var fridge = await _context.Fridges.FindAsync(itemVm.FridgeId);

                if (fridge == null) continue;

                if (itemVm.PurchaseRequestItemId.HasValue)
                {
                    // A. Update Existing Item
                    var existingItem = request.RequestedItems.First(i => i.Id == itemVm.PurchaseRequestItemId.Value);

                    existingItem.FridgeId = fridge.Id;
                    existingItem.ModelRequested = fridge.Model;
                    existingItem.Description = fridge.Description;
                    existingItem.Quantity = itemVm.Quantity;
                }
                else
                {
                    // B. Add New Item
                    var newItem = new PurchaseRequestItem
                    {
                        PurchaseRequestId = request.Id,
                        FridgeId = fridge.Id,
                        ModelRequested = fridge.Model,
                        Description = fridge.Description,
                        Quantity = itemVm.Quantity
                    };
                    _context.PurchaseRequestItems.Add(newItem);
                }
            }

            // 5. Save all changes (Request updates, item removals, item updates, item additions)
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Purchase request #{request.Id} updated successfully!";
            return RedirectToAction("Details", new { id = request.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendRequest(int id)
        {
            var request = await _context.PurchaseRequests
                .Include(r => r.RequestedItems)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null) return NotFound();

            // Only allow sending if status is not already Submitted
            if (request.Status != Models.Enums.RequestStatus.Submitted)
            {
                request.Status = Models.Enums.RequestStatus.Submitted;
                await _context.SaveChangesAsync();
            }

            // Notify the Purchasing Manager
            var purchasemanager = await _userManager.GetUsersInRoleAsync("Purchasing Manager");
            var liaisonUser = purchasemanager.FirstOrDefault();

            if (liaisonUser != null)
            {
                var notification = new Notification
                {
                    RecipientUserId = liaisonUser.Id,
                    Title = "New Purchase Request Sent",
                    Message = $"Purchase request #{request.Id} was submitted by {User.Identity.Name}.",
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false,
                    TargetController = "PurchasingManager",
                    TargetAction = "PurchaseRequests"
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
            }

            // ✅ Use TempData for success message
            TempData["Success"] = $"Purchase request #{request.Id} sent successfully!";

            // Redirect back to the same page (e.g., index or list)
            return RedirectToAction("SubmitedPurchaseRequests");
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
