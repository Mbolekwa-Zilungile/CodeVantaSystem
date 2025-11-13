using Code_Vanta_Systems.Data;
using Code_Vanta_Systems.Helper;
using Code_Vanta_Systems.Models;
using Code_Vanta_Systems.Models.Enums;
using Code_Vanta_Systems.ViewModels;
using Code_Vanta_Systems.ViewModels.InventoryLiaison; // Assuming specific ViewModels are here
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Drawing;
using System.Reflection;
using Microsoft.AspNetCore.Hosting; // Used for IWebHostEnvironment

namespace Code_Vanta_Systems.Controllers
{
    [Authorize(Roles = "Inventory Liaison")]
    public class InventoryLiaisonController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;
        // Placeholder: Assume ID 1 is the main warehouse location ID, or it's dynamically found.
        private readonly string defaultWarehouseDescription = "Main Warehouse";

        public InventoryLiaisonController(ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }

        #region Catalogue (FridgeModel) Management

        [HttpGet]
        public async Task<IActionResult> ModelList(string searchModel, int? category, int? supplier)
        {
            var modelsQuery = _context.FridgeModels
                .Include(fm => fm.Supplier)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchModel))
                modelsQuery = modelsQuery.Where(fm => fm.ModelName.Contains(searchModel));

            if (category.HasValue)
                modelsQuery = modelsQuery.Where(fm => (int)fm.Category == category.Value);

            if (supplier.HasValue)
                modelsQuery = modelsQuery.Where(fm => fm.SupplierId == supplier.Value);

            var model = await modelsQuery
                .Select(fm => new FridgeModelViewModel // Assuming a ViewModel for the list
                {
                    Id = fm.Id,
                    ModelName = fm.ModelName,
                    SupplierName = fm.Supplier.Name,
                    Category = fm.Category,
                    RentalPricePerMonth = fm.RentalPricePerMonth,
                    // FIX: Dynamically calculate stock by counting IN_WAREHOUSE units
                    StockQuantity = _context.FridgeUnits
                                            .Count(fu => fu.FridgeModelId == fm.Id && fu.Status == FridgeStatus.InWarehouse)
                })
                .ToListAsync();

            ViewBag.Categories = GetCategorySelectList(category);
            ViewBag.Suppliers = await GetSupplierSelectListAsync(supplier);
            ViewData["searchModel"] = searchModel;

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> CreateFridgeModel()
        {
            // CreateFridgeModelViewModel was provided in the previous response
            var model = new CreateFridgeModelViewModel();
            ViewBag.Suppliers = await GetSupplierSelectListAsync();
            ViewBag.Categories = GetCategorySelectList();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFridgeModel(CreateFridgeModelViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Suppliers = await GetSupplierSelectListAsync(model.SupplierId);
                ViewBag.Categories = GetCategorySelectList((int)model.Category);
                return View(model);
            }

            var fridgeModel = new FridgeModel
            {
                ModelName = model.ModelName,
                Category = model.Category,
                RentalPricePerMonth = model.RentalPricePerMonth,
                Description = model.Description,
                SupplierId = model.SupplierId,
                ImageUrl = UploadFile(model.ImageFile)
            };

            _context.FridgeModels.Add(fridgeModel);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"New Fridge Model '{fridgeModel.ModelName}' created successfully.";
            return RedirectToAction(nameof(ModelList));
        }

        [HttpGet]
        public async Task<IActionResult> EditFridgeModel(int id)
        {
            var fridgeModel = await _context.FridgeModels.FindAsync(id);

            if (fridgeModel == null) return NotFound();

            // EditFridgeModelViewModel is assumed to exist for this action
            var model = new EditFridgeModelViewModel
            {
                Id = fridgeModel.Id,
                ModelName = fridgeModel.ModelName,
                Category = fridgeModel.Category,
                RentalPricePerMonth = fridgeModel.RentalPricePerMonth,
                Description = fridgeModel.Description,
                SupplierId = fridgeModel.SupplierId,
                ExistingImageUrl = fridgeModel.ImageUrl
            };

            ViewBag.Suppliers = await GetSupplierSelectListAsync(fridgeModel.SupplierId);
            ViewBag.Categories = GetCategorySelectList((int)fridgeModel.Category);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditFridgeModel(EditFridgeModelViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Suppliers = await GetSupplierSelectListAsync(model.SupplierId);
                ViewBag.Categories = GetCategorySelectList((int)model.Category);
                return View(model);
            }

            var fridgeModel = await _context.FridgeModels.FindAsync(model.Id);
            if (fridgeModel == null) return NotFound();

            fridgeModel.ModelName = model.ModelName;
            fridgeModel.Category = model.Category;
            fridgeModel.RentalPricePerMonth = model.RentalPricePerMonth;
            fridgeModel.Description = model.Description;
            fridgeModel.SupplierId = model.SupplierId;

            if (model.ImageFile != null)
            {
                // Delete old image if needed (Implementation in DeleteFile)
                if (!string.IsNullOrEmpty(fridgeModel.ImageUrl))
                    DeleteFile(fridgeModel.ImageUrl);

                fridgeModel.ImageUrl = UploadFile(model.ImageFile);
            }

            _context.FridgeModels.Update(fridgeModel);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Fridge Model '{fridgeModel.ModelName}' updated successfully.";
            return RedirectToAction(nameof(ModelList));
        }

        [HttpGet]
        public async Task<IActionResult> ViewFridgeModel(int id)
        {
            var fridgeModel = await _context.FridgeModels
                .Include(fm => fm.Supplier)
                .FirstOrDefaultAsync(fm => fm.Id == id);

            if (fridgeModel == null) return NotFound();

            // FIX: Count actual physical units for this model
            ViewBag.TotalUnits = await _context.FridgeUnits.CountAsync(fu => fu.FridgeModelId == id);
            ViewBag.AvailableUnits = await _context.FridgeUnits.CountAsync(fu => fu.FridgeModelId == id && fu.Status == FridgeStatus.InWarehouse);

            return View(fridgeModel);
        }
        #endregion

        //--------------------------------------------------------------------------------

        #region Physical Inventory (FridgeUnit) Management

        [HttpGet]
        public async Task<IActionResult> UnitList(string searchSerial, int? modelId, int? status, int? locationId)
        {
            var unitsQuery = _context.FridgeUnits
                .Include(fu => fu.FridgeModel)
                .Include(fu => fu.Location)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchSerial))
                unitsQuery = unitsQuery.Where(fu => fu.SerialNumber.Contains(searchSerial));

            if (modelId.HasValue)
                unitsQuery = unitsQuery.Where(fu => fu.FridgeModelId == modelId.Value);

            if (status.HasValue)
                unitsQuery = unitsQuery.Where(fu => (int)fu.Status == status.Value);

            if (locationId.HasValue)
                unitsQuery = unitsQuery.Where(fu => fu.LocationId == locationId.Value);

            var units = await unitsQuery
                .Select(fu => new FridgeUnitViewModel // FridgeUnitViewModel was provided
                {
                    Id = fu.Id,
                    ModelName = fu.FridgeModel.ModelName,
                    SerialNumber = fu.SerialNumber,
                    Status = fu.Status,
                    Condition = fu.Condition,
                    LocationName = fu.Location.Description
                })
                .ToListAsync();

            ViewBag.FridgeModels = await GetFridgeModelSelectListAsync(modelId);
            ViewBag.Locations = await GetLocationSelectListAsync(locationId);
            ViewBag.Statuses = GetStatusSelectList(status);
            ViewData["searchSerial"] = searchSerial;

            return View(units);
        }

        [HttpGet]
        public async Task<IActionResult> CreateFridgeUnit(int? fridgeModelId)
        {
            var defaultWarehouseId = await GetDefaultWarehouseIdAsync();

            var model = new CreateFridgeUnitViewModel
            {
                PurchaseDate = DateTime.Now,
                FridgeModelId = fridgeModelId ?? 0,
                // Default new unit to a main warehouse and Pending status
                LocationId = defaultWarehouseId,
                Status = FridgeStatus.Pending,
                Condition = FridgeCondition.New
            };

            ViewBag.FridgeModels = await GetFridgeModelSelectListAsync(fridgeModelId);
            ViewBag.Locations = await GetLocationSelectListAsync(model.LocationId);
            ViewBag.Conditions = GetConditionSelectList((int)model.Condition);
            ViewBag.Statuses = GetStatusSelectList((int)model.Status);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFridgeUnit(CreateFridgeUnitViewModel model)
        {
            // Simple validation to ensure the serial number is unique for this model
            if (await _context.FridgeUnits.AnyAsync(fu => fu.SerialNumber == model.SerialNumber))
            {
                ModelState.AddModelError(nameof(model.SerialNumber), "A unit with this serial number already exists.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.FridgeModels = await GetFridgeModelSelectListAsync(model.FridgeModelId);
                ViewBag.Locations = await GetLocationSelectListAsync(model.LocationId);
                ViewBag.Conditions = GetConditionSelectList((int)model.Condition);
                ViewBag.Statuses = GetStatusSelectList((int)model.Status);
                return View(model);
            }

            var fridgeUnit = new FridgeUnit
            {
                FridgeModelId = model.FridgeModelId,
                SerialNumber = model.SerialNumber,
                PurchaseDate = model.PurchaseDate,
                Condition = model.Condition,
                Status = model.Status,
                LocationId = model.LocationId,
            };

            _context.FridgeUnits.Add(fridgeUnit);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"New Fridge Unit '{fridgeUnit.SerialNumber}' added successfully.";
            return RedirectToAction(nameof(UnitList));
        }

        [HttpGet]
        public async Task<IActionResult> EditFridgeUnit(int id)
        {
            var fridgeUnit = await _context.FridgeUnits.FindAsync(id);

            if (fridgeUnit == null) return NotFound();

            // EditFridgeUnitViewModel is assumed to exist for this action
            var model = new EditFridgeUnitViewModel
            {
                Id = fridgeUnit.Id,
                FridgeModelId = fridgeUnit.FridgeModelId,
                SerialNumber = fridgeUnit.SerialNumber,
                PurchaseDate = fridgeUnit.PurchaseDate,
                Condition = fridgeUnit.Condition,
                Status = fridgeUnit.Status,
                LocationId = fridgeUnit.LocationId,
            };

            ViewBag.FridgeModels = await GetFridgeModelSelectListAsync(fridgeUnit.FridgeModelId);
            ViewBag.Locations = await GetLocationSelectListAsync(fridgeUnit.LocationId);
            ViewBag.Conditions = GetConditionSelectList((int)fridgeUnit.Condition);
            ViewBag.Statuses = GetStatusSelectList((int)fridgeUnit.Status);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditFridgeUnit(EditFridgeUnitViewModel model)
        {
            var fridgeUnit = await _context.FridgeUnits.FindAsync(model.Id);
            if (fridgeUnit == null) return NotFound();

            // Simple validation to ensure the serial number is unique (excluding its own ID)
            if (await _context.FridgeUnits.AnyAsync(fu => fu.SerialNumber == model.SerialNumber && fu.Id != model.Id))
            {
                ModelState.AddModelError(nameof(model.SerialNumber), "A unit with this serial number already exists.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.FridgeModels = await GetFridgeModelSelectListAsync(model.FridgeModelId);
                ViewBag.Locations = await GetLocationSelectListAsync(model.LocationId);
                ViewBag.Conditions = GetConditionSelectList((int)model.Condition);
                ViewBag.Statuses = GetStatusSelectList((int)model.Status);
                return View(model);
            }

            fridgeUnit.FridgeModelId = model.FridgeModelId;
            fridgeUnit.SerialNumber = model.SerialNumber;
            fridgeUnit.PurchaseDate = model.PurchaseDate;
            fridgeUnit.Condition = model.Condition;
            fridgeUnit.Status = model.Status;
            fridgeUnit.LocationId = model.LocationId;

            _context.FridgeUnits.Update(fridgeUnit);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Fridge Unit '{fridgeUnit.SerialNumber}' updated successfully.";
            return RedirectToAction(nameof(UnitList));
        }

        [HttpGet]
        public async Task<IActionResult> ViewFridgeUnit(int id)
        {
            var fridgeUnit = await _context.FridgeUnits
                .Include(fu => fu.FridgeModel)
                .Include(fu => fu.Location)
                .Include(fu => fu.Allocations).ThenInclude(a => a.Customer)
                .FirstOrDefaultAsync(fu => fu.Id == id);

            if (fridgeUnit == null) return NotFound();

            ViewBag.CurrentAllocation = fridgeUnit.Allocations.OrderByDescending(a => a.AllocationDate).FirstOrDefault(a => a.DeallocationDate == null);

            return View(fridgeUnit);
        }

        [HttpGet]
        public async Task<IActionResult> PendingFridges()
        {
            var pendingFridges = await _context.FridgeUnits
                .Include(fu => fu.FridgeModel)
                .Where(fu => fu.Status == FridgeStatus.Pending)
                // PendingFridgeEditViewModel is assumed to exist for this list
                .Select(fu => new PendingFridgeEditViewModel
                {
                    Id = fu.Id,
                    SerialNumber = fu.SerialNumber,
                    ModelName = fu.FridgeModel.ModelName,
                    Condition = fu.Condition,
                    RentalPricePerMonth = fu.FridgeModel.RentalPricePerMonth
                }).ToListAsync();

            return View(pendingFridges);
        }

        // This action would handle moving a unit from Pending to InWarehouse
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptPendingUnit(int id)
        {
            var unit = await _context.FridgeUnits.FindAsync(id);
            if (unit == null)
            {
                TempData["ErrorMessage"] = "Fridge unit not found.";
                return RedirectToAction(nameof(PendingFridges));
            }

            if (unit.Status != FridgeStatus.Pending)
            {
                TempData["ErrorMessage"] = "Unit is not in Pending status and cannot be accepted.";
                return RedirectToAction(nameof(PendingFridges));
            }

            unit.Status = FridgeStatus.InWarehouse;
            _context.FridgeUnits.Update(unit);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Unit {unit.SerialNumber} accepted and moved to InWarehouse status.";
            return RedirectToAction(nameof(PendingFridges));
        }


        #endregion

        //--------------------------------------------------------------------------------

        #region Helpers (Includes file operations and SelectList generators)

        private async Task<SelectList> GetSupplierSelectListAsync(int? selectedId = null)
        {
            var suppliers = await _context.Suppliers.ToListAsync();
            return new SelectList(suppliers, "Id", "Name", selectedId);
        }

        private List<SelectListItem> GetCategorySelectList(int? selectedId = null)
        {
            return Enum.GetValues(typeof(FridgeCategory))
                       .Cast<FridgeCategory>()
                       .Select(c => new SelectListItem
                       {
                           Value = ((int)c).ToString(),
                           Text = GetDisplayName(c),
                           Selected = (int?)c == selectedId
                       })
                       .ToList();
        }

        private List<SelectListItem> GetStatusSelectList(int? selectedId = null)
        {
            return Enum.GetValues(typeof(FridgeStatus))
                       .Cast<FridgeStatus>()
                       .Select(s => new SelectListItem
                       {
                           Value = ((int)s).ToString(),
                           Text = s.ToString(),
                           Selected = (int?)s == selectedId
                       })
                       .ToList();
        }

        private List<SelectListItem> GetConditionSelectList(int? selectedId = null)
        {
            return Enum.GetValues(typeof(FridgeCondition))
                       .Cast<FridgeCondition>()
                       .Select(c => new SelectListItem
                       {
                           Value = ((int)c).ToString(),
                           Text = c.ToString(),
                           Selected = (int?)c == selectedId
                       })
                       .ToList();
        }

        private async Task<SelectList> GetFridgeModelSelectListAsync(int? selectedId = null)
        {
            var models = await _context.FridgeModels.ToListAsync();
            return new SelectList(models, "Id", "ModelName", selectedId);
        }

        private async Task<SelectList> GetLocationSelectListAsync(int? selectedId = null)
        {
            var locations = await _context.Locations.ToListAsync();
            return new SelectList(locations, "Id", "Description", selectedId);
        }

        private async Task<int> GetDefaultWarehouseIdAsync()
        {
            // Logic to find the ID of the main warehouse location
            var warehouse = await _context.Locations.FirstOrDefaultAsync(l => l.Description.Contains(defaultWarehouseDescription));
            // Return ID if found, otherwise assume ID 1 as a fallback (you should ensure this location exists)
            return warehouse?.Id ?? 1;
        }

        // Helper to get DisplayName attribute value from Enum
        private string GetDisplayName(Enum enumValue)
        {
            // Helper method logic is included here
            return enumValue.GetType()
                       .GetMember(enumValue.ToString())
                       .FirstOrDefault()
                       ?.GetCustomAttribute<System.ComponentModel.DataAnnotations.DisplayAttribute>()
                       ?.Name ?? enumValue.ToString();
        }

        // 🛑 FULL IMPLEMENTATION OF FILE UPLOAD LOGIC INCLUDED 🛑
        private string? UploadFile(IFormFile? file)
        {
            if (file == null || file.Length == 0) return null;

            // Define the path where images will be saved
            string uploadDir = Path.Combine(_environment.WebRootPath, "images", "fridgemodels");
            if (!Directory.Exists(uploadDir))
            {
                Directory.CreateDirectory(uploadDir);
            }

            // Create a unique filename
            string fileExtension = Path.GetExtension(file.FileName);
            string uniqueFileName = Guid.NewGuid().ToString() + fileExtension;
            string filePath = Path.Combine(uploadDir, uniqueFileName);

            // Save the file
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                file.CopyTo(fileStream);
            }

            // Return the relative URL path
            return $"/images/fridgemodels/{uniqueFileName}";
        }

        // 🛑 FULL IMPLEMENTATION OF FILE DELETION LOGIC INCLUDED 🛑
        private void DeleteFile(string? relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return;

            // Convert relative path to absolute path
            string absolutePath = Path.Combine(_environment.WebRootPath, relativePath.TrimStart('/'));

            if (System.IO.File.Exists(absolutePath))
            {
                try
                {
                    System.IO.File.Delete(absolutePath);
                }
                catch
                {
                    // Log the exception if necessary, but don't stop the request
                    DebuggerNotify($"Failed to delete file at: {absolutePath}");
                }
            }
        }

        #endregion

        #region ViewModels (Required for compilation, usually in a separate file)
        // NOTE: These ViewModels are defined here for completeness of the controller file, 
        // but should ideally reside in the ViewModels.InventoryLiaison namespace.

        // Assumed structure for editing a FridgeModel
        public class EditFridgeModelViewModel : CreateFridgeModelViewModel
        {
            public int Id { get; set; }
            public string? ExistingImageUrl { get; set; }
        }

        // Assumed structure for editing a FridgeUnit
        public class EditFridgeUnitViewModel : CreateFridgeUnitViewModel
        {
            public int Id { get; set; }
        }

        // Assumed structure for the Pending Fridges list
        public class PendingFridgeEditViewModel
        {
            public int Id { get; set; }
            public string SerialNumber { get; set; }
            public string ModelName { get; set; }
            public FridgeCondition Condition { get; set; }
            public decimal RentalPricePerMonth { get; set; }
        }
        #endregion

        #region ERROR_IDENTIFIES

        public void DebuggerNotify(string bug)
        {
            // Simple console logging for file deletion errors
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR] InventoryLiaisonController: {bug}");
            Console.ForegroundColor = ConsoleColor.White;
        }

        #endregion
    }
}