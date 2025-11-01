using Code_Vanta_Systems.Data;
using Code_Vanta_Systems.Helper;
using Code_Vanta_Systems.Models;
using Code_Vanta_Systems.Models.Enums;
using Code_Vanta_Systems.ViewModels;
using Code_Vanta_Systems.ViewModels.FridgeUnit;
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

        #region Index / Dashboard - UPDATED FOR THREE-TIER MODEL
        public IActionResult Index(string searchSerial, int? category, int? status, int? supplier, int? location)
        {
            // Get all fridge units (individual physical fridges)
            var allFridgeUnits = _context.FridgeUnits.AsQueryable();

            // Filter by search
            if (!string.IsNullOrEmpty(searchSerial))
                allFridgeUnits = allFridgeUnits.Where(f => f.SerialNumber.Contains(searchSerial));

            // Filter by category
            if (category.HasValue)
            {
                var categoryString = ((FridgeCategory)category.Value).ToString();
                allFridgeUnits = allFridgeUnits.Where(f => f.FridgeModel.Category.ToString() == categoryString);
            }

            // Filter by status
            if (status.HasValue)
            {
                var statusEnum = (FridgeStatus)status.Value;
                allFridgeUnits = allFridgeUnits.Where(f => f.Status == statusEnum);
            }

            if (supplier.HasValue)
                allFridgeUnits = allFridgeUnits.Where(f => f.FridgeModel.SupplierId == supplier.Value);

            if (location.HasValue)
                allFridgeUnits = allFridgeUnits.Where(f => f.LocationId == location.Value);

            // Include related data
            var fridgeUnits = allFridgeUnits
                .Include(f => f.FridgeModel)
                .Include(f => f.Location)
                .Include(f => f.CurrentCustomer)
                .ToList();

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

            // Counts based on individual units
            ViewBag.TotalFridgeUnits = allFridgeUnits.Count();
            ViewBag.AvailableFridgeUnits = allFridgeUnits.Count(f => f.Status == FridgeStatus.InWarehouse);
            ViewBag.AllocatedFridgeUnits = allFridgeUnits.Count(f => f.Status == FridgeStatus.Allocated);
            ViewBag.InRepairFridgeUnits = allFridgeUnits.Count(f => f.Status == FridgeStatus.InRepair);
            ViewBag.ScrappedFridgeUnits = allFridgeUnits.Count(f => f.Status == FridgeStatus.Scrapped);
            ViewBag.TotalSuppliers = _context.Suppliers.Count();

            // Map to ViewModel
            var vmList = fridgeUnits.Select(f => new FridgeUnitViewModel
            {
                Id = f.Id,
                SerialNumber = f.SerialNumber,
                FridgeModelName = f.FridgeModel.Name,
                Category = f.FridgeModel.Category,
                Status = f.Status,
                Condition = f.Condition,
                LocationName = f.Location?.Description ?? "N/A",
                CurrentCustomerName = f.CurrentCustomer?.BusinessName ?? "Not Allocated",
                PurchaseDate = f.PurchaseDate
            }).ToList();

            return View(vmList);
        }
        #endregion

        #region CREATE FRIDGE UNIT - UPDATED
        [HttpGet]
        public IActionResult CreateFridgeUnit()
        {
            var model = new CreateFridgeUnitViewModel();
            PopulateFridgeUnitDropdowns(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFridgeUnit(CreateFridgeUnitViewModel vm)
        {
            PopulateFridgeUnitDropdowns(vm);

            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            // Check if serial number already exists
            var existingSerial = await _context.FridgeUnits
                .AnyAsync(f => f.SerialNumber == vm.SerialNumber);

            if (existingSerial)
            {
                ModelState.AddModelError("SerialNumber", "Serial number already exists. Each fridge unit must have a unique serial number.");
                return View(vm);
            }

            var fridgeUnit = new FridgeUnit
            {
                SerialNumber = vm.SerialNumber,
                Status = FridgeStatus.InWarehouse,
                Condition = vm.Condition,
                PurchaseDate = vm.PurchaseDate,
                FridgeModelId = vm.FridgeModelId,
                LocationId = vm.LocationId
            };

            _context.FridgeUnits.Add(fridgeUnit);

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Fridge unit added successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Unable to save fridge unit. Check server logs.");
                return View(vm);
            }
        }

        private void PopulateFridgeUnitDropdowns(CreateFridgeUnitViewModel vm)
        {
            vm.FridgeModels = _context.FridgeModels
                .Select(fm => new SelectListItem { Value = fm.Id.ToString(), Text = $"{fm.Name} ({fm.Category})" })
                .ToList();

            vm.Locations = _context.Locations
                .Select(l => new SelectListItem { Value = l.Id.ToString(), Text = l.Description })
                .ToList();

            vm.Conditions = Enum.GetValues(typeof(FridgeCondition))
                .Cast<FridgeCondition>()
                .Select(c => new SelectListItem { Value = ((int)c).ToString(), Text = c.ToString() })
                .ToList();
        }
        #endregion

        #region ALLOCATE FRIDGE UNIT - NEW METHOD
        [HttpGet]
        public async Task<IActionResult> AllocateFridgeUnit(int fridgeUnitId)
        {
            var fridgeUnit = await _context.FridgeUnits
                .Include(f => f.FridgeModel)
                .FirstOrDefaultAsync(f => f.Id == fridgeUnitId && f.Status == FridgeStatus.InWarehouse);

            if (fridgeUnit == null)
            {
                TempData["ErrorMessage"] = "Fridge unit not found or not available for allocation.";
                return RedirectToAction(nameof(Index));
            }

            var model = new AllocateFridgeUnitViewModel
            {
                FridgeUnitId = fridgeUnitId,
                SerialNumber = fridgeUnit.SerialNumber,
                FridgeModelName = fridgeUnit.FridgeModel.Name,
                Customers = await _context.Customers
                    .Where(c => c.Status == CustomerStatus.Active)
                    .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.BusinessName })
                    .ToListAsync()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AllocateFridgeUnit(AllocateFridgeUnitViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Customers = await _context.Customers
                    .Where(c => c.Status == CustomerStatus.Active)
                    .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.BusinessName })
                    .ToListAsync();
                return View(model);
            }

            var fridgeUnit = await _context.FridgeUnits
                .FirstOrDefaultAsync(f => f.Id == model.FridgeUnitId && f.Status == FridgeStatus.InWarehouse);

            if (fridgeUnit == null)
            {
                TempData["ErrorMessage"] = "Fridge unit not available for allocation.";
                return RedirectToAction(nameof(Index));
            }

            // Update fridge unit status
            fridgeUnit.Status = FridgeStatus.Allocated;
            fridgeUnit.CurrentCustomerId = model.CustomerId;

            // Create allocation record
            var allocation = new FridgeAllocation
            {
                FridgeUnitId = fridgeUnit.Id,
                CustomerId = model.CustomerId,
                AllocationDate = DateTime.Now,
                CreatedByUserId = _userManager.GetUserId(User)
            };

            _context.FridgeAllocations.Add(allocation);

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Fridge unit {fridgeUnit.SerialNumber} allocated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error allocating fridge unit.";
                return RedirectToAction(nameof(Index));
            }
        }
        #endregion

        #region DEALLOCATE FRIDGE UNIT - NEW METHOD
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeallocateFridgeUnit(int fridgeUnitId)
        {
            var fridgeUnit = await _context.FridgeUnits
                .Include(f => f.Allocations.Where(a => a.DeallocationDate == null))
                .FirstOrDefaultAsync(f => f.Id == fridgeUnitId && f.Status == FridgeStatus.Allocated);

            if (fridgeUnit == null)
            {
                TempData["ErrorMessage"] = "Fridge unit not found or not allocated.";
                return RedirectToAction(nameof(Index));
            }

            // Update fridge unit status
            fridgeUnit.Status = FridgeStatus.InWarehouse;
            fridgeUnit.CurrentCustomerId = null;

            // Update allocation record
            var activeAllocation = fridgeUnit.Allocations.FirstOrDefault();
            if (activeAllocation != null)
            {
                activeAllocation.DeallocationDate = DateTime.Now;
            }

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Fridge unit {fridgeUnit.SerialNumber} deallocated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error deallocating fridge unit.";
                return RedirectToAction(nameof(Index));
            }
        }
        #endregion

        // ... keep existing Purchase Request methods as they are ...

        #region Helper Methods
        private void DebuggerNotify(string bug)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine(bug);
        }
        #endregion
    }
}