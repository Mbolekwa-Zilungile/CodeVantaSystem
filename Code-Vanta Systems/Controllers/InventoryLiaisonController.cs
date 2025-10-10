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
using System.Drawing;

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


        #region Index / Dashboards With Filters

        public IActionResult Index(string searchModel, int? category, int? status, int? supplier, int? location)
        {
            var allFridges = _context.Fridges.AsQueryable();

            // Only active fridges
            var fridges = allFridges.Where(f => !f.IsDeleted);

            // Filter by search
            if (!string.IsNullOrEmpty(searchModel))
                fridges = fridges.Where(f => f.Model.Contains(searchModel));

            // Filter by category (compare string enum)
            if (category.HasValue)
            {
                var categoryString = ((FridgeCategory)category.Value).ToString();
                fridges = fridges.Where(f => f.Category.ToString() == categoryString);
            }

            // Filter by status (compare string enum)
            if (status.HasValue)
            {
                var statusString = ((FridgeStatus)status.Value).ToString();
                fridges = fridges.Where(f => f.Status.ToString() == statusString);
            }

            if (supplier.HasValue)
                fridges = fridges.Where(f => f.SupplierId == supplier.Value);

            if (location.HasValue)
                fridges = fridges.Where(f => f.LocationId == location.Value);

            // Dropdowns
            ViewBag.Categories = Enum.GetValues(typeof(FridgeCategory))
                .Cast<FridgeCategory>()
                .Select(c => new SelectListItem { Value = ((int)c).ToString(), Text = c.ToString() })
                .ToList();

            ViewBag.Statuses = Enum.GetValues(typeof(FridgeStatus))
                .Cast<FridgeStatus>()
                .Select(s => new SelectListItem { Value = ((int)s).ToString(), Text = s.ToString() })
                .ToList();

            ViewBag.Suppliers = _context.Suppliers
                .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name })
                .ToList();

            ViewBag.Locations = _context.Locations
                .Select(l => new SelectListItem { Value = l.Id.ToString(), Text = l.Description })
                .ToList();

            // Counts
            ViewBag.TotalFridges = allFridges.Count();
            ViewBag.ActiveFridges = allFridges.Count(f => !f.IsDeleted);
            ViewBag.DeletedFridges = allFridges.Count(f => f.IsDeleted);
            ViewBag.AvailableFridges = allFridges.Count(f => f.Status == FridgeStatus.Available && !f.IsDeleted);
            ViewBag.OutOfStockFridges = allFridges.Count(f => f.StockQuantity == 0 && !f.IsDeleted);
            ViewBag.InRepairFridges = allFridges.Count(f => f.Status == FridgeStatus.InRepair && !f.IsDeleted);
            ViewBag.RetiredFridges = allFridges.Count(f => f.Status == FridgeStatus.Retired && !f.IsDeleted);
            ViewBag.TotalSuppliers = _context.Suppliers.Count();

            // Map to ViewModel
            var vmList = fridges.Select(f => new CreateFridgeViewModel
            {
                Id = f.Id,
                Model = f.Model,
                SerialNumber = f.SerialNumber,
                Category = f.Category,
                Status = f.Status,
                SupplierId = f.SupplierId,
                LocationId = f.LocationId,
                RentalPricePerMonth = f.RentalPricePerMonth,
                StockQuantity = f.StockQuantity,
                Description = f.Description,
                ImageUrl = f.ImageUrl
            }).ToList();

            return View(vmList);
        }


        #endregion

        // Dashboard..





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

            // Validate expected date
            if (model.ExpectedDate.HasValue && model.ExpectedDate.Value.Date < DateTime.Now.Date)
            {
                ModelState.AddModelError("ExpectedDate", "Expected date cannot be in the past.");
            }

            // Validate fridge selection
            if (model.Items == null || !model.Items.Any(i => i.FridgeId > 0))
            {
                ModelState.AddModelError("", "You must select at least one fridge.");
            }

            // **Remove ModelState errors for FridgeModel fields**
            var keysToRemove = ModelState.Keys.Where(k => k.EndsWith("FridgeModel")).ToList();
            foreach (var key in keysToRemove)
            {
                ModelState.Remove(key);
            }

            if (!ModelState.IsValid)
            {
                var allFridges = await _context.Fridges
                    .Where(f => !f.IsDeleted)
                    .Select(f => new SelectListItem
                    {
                        Value = f.Id.ToString(),
                        Text = $"{f.Model} ({f.StockQuantity} in stock)"
                    })
                    .ToListAsync();

                foreach (var item in model.Items)
                {
                    item.AvailableItems = allFridges;

                    // Set FridgeModel for display if fridge is selected
                    if (item.FridgeId > 0)
                    {
                        var fridge = await _context.Fridges.FirstOrDefaultAsync(f => f.Id == item.FridgeId);
                        if (fridge != null)
                        {
                            item.FridgeModel = fridge.Model;
                        }
                    }
                }

                model.AvailableItems = allFridges;

                return View(model);
            }

            // Continue with purchase request creation as you had it...
            var request = new PurchaseRequest
            {
                RequestedByUserId = user.Id,
                RequestDate = DateTime.Now,
                ExpectedDate = model.ExpectedDate,
                Priority = model.Priority,
                Notes = model.Notes,
                Status = RequestStatus.Submitted
            };

            _context.PurchaseRequests.Add(request);
            await _context.SaveChangesAsync();

            var fridgeIds = model.Items.Where(i => i.FridgeId > 0).Select(i => i.FridgeId).Distinct();
            var fridgesDict = await _context.Fridges
                .Where(f => fridgeIds.Contains(f.Id))
                .ToDictionaryAsync(f => f.Id);

            foreach (var item in model.Items.Where(i => i.FridgeId > 0))
            {
                if (fridgesDict.TryGetValue(item.FridgeId, out var fridge))
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

            // Notify Purchasing Manager
            var purchasingManagers = await _userManager.GetUsersInRoleAsync("Purchasing Manager");
            var liaisonUser = purchasingManagers.FirstOrDefault();

            if (liaisonUser != null)
            {
                var notification = new Notification
                {
                    RecipientUserId = liaisonUser.Id,
                    Title = "New Purchase Request Sent",
                    Message = $"A new purchase request #{request.Id} was submitted by {User.Identity?.Name ?? "Unknown User"}.",
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false,
                    TargetController = "PurchasingManager",
                    TargetAction = "PurchaseRequests"
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Purchase request submitted successfully!";
            return RedirectToAction(nameof(SubmitedPurchaseRequests));
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
                    AvailableItems = fridges,
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
            var keysToRemove = ModelState.Keys.Where(k => k.EndsWith("FridgeModel")).ToList();
            foreach (var key in keysToRemove)
            {
                ModelState.Remove(key);
            } 
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

        [HttpGet]
        public IActionResult CreateFridge()
        {
            var model = new CreateFridgeViewModel();
            PopulateDropdowns(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFridge(CreateFridgeViewModel vm)
        {

            PopulateDropdowns(vm);

            if (!ModelState.IsValid)
            {
                DebuggerNotify("ModelState invalid.");
                return View(vm);
            }

            string imageUrl = "/images/fridges/placeholder.png";

            if (vm.ImageFile != null && vm.ImageFile.Length > 0)
            {
                DebuggerNotify($"Uploading image: {vm.ImageFile.FileName}");
                string uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "fridges");
                Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = Guid.NewGuid() + Path.GetExtension(vm.ImageFile.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                await using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await vm.ImageFile.CopyToAsync(fileStream);
                }

                imageUrl = "/images/fridges/" + uniqueFileName;
                DebuggerNotify($"Image saved to: {imageUrl}");
            }

            var fridge = new Fridge
            {
                Model = vm.Model,
                SerialNumber = vm.SerialNumber,
                Category = vm.Category,
                Status = vm.Status,
                SupplierId = vm.SupplierId,
                LocationId = vm.LocationId,
                RentalPricePerMonth = vm.RentalPricePerMonth,
                StockQuantity = vm.StockQuantity,
                Description = vm.Description,
                ImageUrl = imageUrl,
                PurchaseDate = DateTime.Now,
                IsDeleted = false
            };

            _context.Fridges.Add(fridge);

            try
            {
                await _context.SaveChangesAsync();
                DebuggerNotify("Fridge saved successfully.");
            }
            catch (Exception ex)
            {
                DebuggerNotify("Error saving fridge: " + ex.Message);
                ModelState.AddModelError("", "Unable to save fridge. Check server logs.");
                return View(vm);
            }

            TempData["SuccessMessage"] = "Fridge added successfully!";
            return RedirectToAction(nameof(Index));
        }


        private void PopulateDropdowns(CreateFridgeViewModel vm)
        {
            vm.Suppliers = _context.Suppliers
                .Select(s => new CreateFridgeViewModel.SupplierDropdownViewModel { Id = s.Id, Name = s.Name })
                .ToList();

            vm.Locations = _context.Locations
                .Select(l => new CreateFridgeViewModel.LocationDropdownViewModel { Id = l.Id, Name = l.Description })
                .ToList();
        }


        #endregion



        #region EDIT FRIDGE


        [HttpGet]
        public async Task<IActionResult> EditFridge(int id)
        {
            var fridge = await _context.Fridges.FindAsync(id);
            if (fridge == null) return NotFound();

            var vm = new CreateFridgeViewModel
            {
                Id = fridge.Id,
                Model = fridge.Model,
                SerialNumber = fridge.SerialNumber,
                Category = fridge.Category,
                Status = fridge.Status,
                SupplierId = fridge.SupplierId,
                LocationId = fridge.LocationId,
                RentalPricePerMonth = fridge.RentalPricePerMonth,
                StockQuantity = fridge.StockQuantity,
                Description = fridge.Description,
                ImageUrl = fridge.ImageUrl
            };

            PopulateDropdowns(vm);

            return View(vm);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditFridge(CreateFridgeViewModel vm)
        {

            PopulateDropdowns(vm);

            if (!ModelState.IsValid)
            {
                DebuggerNotify("ModelState invalid.");
                return View(vm);
            }

            var fridge = await _context.Fridges.FindAsync(vm.Id);
            if (fridge == null) return NotFound();

            fridge.Model = vm.Model;
            fridge.SerialNumber = vm.SerialNumber;
            fridge.Category = vm.Category;
            fridge.Status = vm.Status;
            fridge.SupplierId = vm.SupplierId;
            fridge.LocationId = vm.LocationId;
            fridge.RentalPricePerMonth = vm.RentalPricePerMonth;
            fridge.StockQuantity = vm.StockQuantity;
            fridge.Description = vm.Description;


            if (vm.ImageFile != null && vm.ImageFile.Length > 0)
            {
                string uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "fridges");
                Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = Guid.NewGuid() + Path.GetExtension(vm.ImageFile.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                await using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await vm.ImageFile.CopyToAsync(fileStream);
                }


                if (!string.IsNullOrEmpty(fridge.ImageUrl) && fridge.ImageUrl != "/images/fridges/placeholder.png")
                {
                    var oldImagePath = Path.Combine(_environment.WebRootPath, fridge.ImageUrl.TrimStart('/').Replace("/", "\\"));
                    if (System.IO.File.Exists(oldImagePath))
                        System.IO.File.Delete(oldImagePath);
                }

                fridge.ImageUrl = "/images/fridges/" + uniqueFileName;
            }

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Fridge updated successfully!";
                DebuggerNotify("Fridge updated successfully.");
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                DebuggerNotify("Error updating fridge: " + ex.Message);
                ModelState.AddModelError("", "Unable to update fridge. Check server logs.");
                return View(vm);
            }
        }

        #endregion


        #region DELETED FRIDGES

        public IActionResult DeletedFridges(string searchModel, int? category, int? status, int? supplier, int? location)
        {
            var fridges = _context.Fridges
                .Where(f => f.IsDeleted)
                .AsQueryable();


            if (!string.IsNullOrEmpty(searchModel))
                fridges = fridges.Where(f => f.Model.Contains(searchModel));

            if (category.HasValue)
                fridges = fridges.Where(f => (int)f.Category == category.Value);

            if (status.HasValue)
                fridges = fridges.Where(f => (int)f.Status == status.Value);

            if (supplier.HasValue)
                fridges = fridges.Where(f => f.SupplierId == supplier.Value);

            if (location.HasValue)
                fridges = fridges.Where(f => f.LocationId == location.Value);


            var vmList = fridges
                .Select(f => new CreateFridgeViewModel
                {
                    Id = f.Id,
                    Model = f.Model,
                    SerialNumber = f.SerialNumber,
                    Category = f.Category,
                    Status = f.Status,
                    SupplierId = f.SupplierId,
                    LocationId = f.LocationId,
                    RentalPricePerMonth = f.RentalPricePerMonth,
                    StockQuantity = f.StockQuantity,
                    Description = f.Description,
                    ImageUrl = f.ImageUrl,
                    Suppliers = _context.Suppliers
                        .Select(s => new CreateFridgeViewModel.SupplierDropdownViewModel { Id = s.Id, Name = s.Name })
                        .ToList(),
                    Locations = _context.Locations
                        .Select(l => new CreateFridgeViewModel.LocationDropdownViewModel { Id = l.Id, Name = l.Description })
                        .ToList()
                })
                .ToList();


            ViewBag.Categories = Enum.GetValues(typeof(FridgeCategory))
                .Cast<FridgeCategory>()
                .Select(c => new SelectListItem
                {
                    Value = ((int)c).ToString(),
                    Text = c.ToString()
                })
                .ToList();

            ViewBag.Statuses = Enum.GetValues(typeof(FridgeStatus))
                .Cast<FridgeStatus>()
                .Select(s => new SelectListItem
                {
                    Value = ((int)s).ToString(),
                    Text = s.ToString()
                })
                .ToList();

            ViewBag.SelectedModel = searchModel;
            ViewBag.SelectedCategory = category?.ToString();
            ViewBag.SelectedStatus = status?.ToString();
            ViewBag.SelectedSupplier = supplier?.ToString();
            ViewBag.SelectedLocation = location?.ToString();

            ViewBag.Suppliers = _context.Suppliers
                .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name })
                .ToList();

            ViewBag.Locations = _context.Locations
                .Select(l => new SelectListItem { Value = l.Id.ToString(), Text = l.Description })
                .ToList();

            return View(vmList);
        }

        #endregion

        #region SOFT HEAD DELETE

        // DeleteConfirmation/5
        public async Task<IActionResult> DeleteConfirmation(int id)
        {
            var fridge = await _context.Fridges
                .Include(f => f.Supplier)
                .Include(f => f.Location)
                .Include(f => f.Maintenances)
                .Include(f => f.UsageLogs)
                .Include(f => f.Faults)
                .Include(f => f.Allocations)
                .Include(f => f.SensorReadings)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (fridge == null)
                return NotFound();

            return View(fridge);
        }

        // POST: SoftDelete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SoftDelete(int id)
        {
            var fridge = await _context.Fridges.FindAsync(id);
            if (fridge == null)
                return NotFound();

            fridge.IsDeleted = true;
            fridge.DeletedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Fridge deleted successfully (soft delete).";
            return RedirectToAction(nameof(Index));
        }

        #endregion

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RestoreFridge(int id)
        {
            var fridge = _context.Fridges.FirstOrDefault(f => f.Id == id && f.IsDeleted);
            if (fridge == null)
                return NotFound();

            fridge.IsDeleted = false;
            fridge.DeletedAt = null;

            _context.SaveChanges();

            TempData["SuccessMessage"] = "Fridge restored successfully!";
            return RedirectToAction("DeletedFridges");
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





        #region ERROR_IDENTIFIES

        public void DebuggerNotify(string bug)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine(bug);
        }

        #endregion

    }
}
