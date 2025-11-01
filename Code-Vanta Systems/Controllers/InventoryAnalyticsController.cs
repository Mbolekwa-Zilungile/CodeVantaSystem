using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Code_Vanta_Systems.Data;
using Code_Vanta_Systems.Models;
using Code_Vanta_Systems.ViewModels;
using Code_Vanta_Systems.Models.Enums;
using Code_Vanta_Systems.ViewModels.FridgeModel;
using Code_Vanta_Systems.ViewModels.FridgeUnit;
using Code_Vanta_Systems.ViewModels.PurchaseRequest;
using Code_Vanta_Systems.ViewModels.Analytics;
using Code_Vanta_Systems.ViewModels.Shared;
using System.Linq;
using System.Threading.Tasks;

namespace Code_Vanta_Systems.Controllers
{
    public class InventoryAnalyticsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InventoryAnalyticsController(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Inventory Overview
        public async Task<IActionResult> InventoryOverview(DateTime? startDate, DateTime? endDate)
        {
            // Set default date range
            endDate ??= DateTime.UtcNow;
            startDate ??= endDate.Value.AddMonths(-1); // Default to last month

            // 1. Fridge Unit Status Analytics
            var fridgeUnitStatus = await _context.FridgeUnits
                .GroupBy(f => f.Status)
                .Select(g => new StatusCount
                {
                    Status = g.Key.ToString(),
                    Count = g.Count()
                })
                .ToListAsync();

            // 2. Fridge Units by Location/Region
            var fridgeUnitsByLocation = await _context.FridgeUnits
                .Include(f => f.Location)
                .Where(f => f.Location != null)
                .GroupBy(f => f.Location.Description)
                .Select(g => new StatusCount
                {
                    Status = g.Key ?? "Unknown Location",
                    Count = g.Count()
                })
                .ToListAsync();

            // 3. Fridge Models by Category with Stock Levels
            var fridgeModelsByCategory = await _context.FridgeModels
                .GroupBy(f => f.Category)
                .Select(g => new CategoryStockViewModel
                {
                    Category = g.Key.ToString(),
                    TotalModels = g.Count(),
                    // Calculate available units per category
                    AvailableUnits = _context.FridgeUnits
                        .Count(fu => fu.FridgeModel.Category == g.Key && fu.Status == FridgeStatus.InWarehouse),
                    TotalUnits = _context.FridgeUnits.Count(fu => fu.FridgeModel.Category == g.Key),
                    UnitsInRepair = _context.FridgeUnits.Count(fu => fu.FridgeModel.Category == g.Key && fu.Status == FridgeStatus.InRepair),
                    // Original properties for compatibility
                    TotalStock = _context.FridgeUnits.Count(fu => fu.FridgeModel.Category == g.Key),
                    HighStock = _context.FridgeUnits.Count(fu => fu.FridgeModel.Category == g.Key && fu.Status == FridgeStatus.InWarehouse && _context.FridgeUnits.Count(f => f.FridgeModel.Category == g.Key && f.Status == FridgeStatus.InWarehouse) >= 15),
                    MediumStock = _context.FridgeUnits.Count(fu => fu.FridgeModel.Category == g.Key && fu.Status == FridgeStatus.InWarehouse && _context.FridgeUnits.Count(f => f.FridgeModel.Category == g.Key && f.Status == FridgeStatus.InWarehouse) >= 5),
                    LowStock = _context.FridgeUnits.Count(fu => fu.FridgeModel.Category == g.Key && fu.Status == FridgeStatus.InWarehouse && _context.FridgeUnits.Count(f => f.FridgeModel.Category == g.Key && f.Status == FridgeStatus.InWarehouse) < 5),
                    CriticalStock = _context.FridgeUnits.Count(fu => fu.FridgeModel.Category == g.Key && fu.Status == FridgeStatus.InWarehouse && _context.FridgeUnits.Count(f => f.FridgeModel.Category == g.Key && f.Status == FridgeStatus.InWarehouse) == 0)
                })
                .ToListAsync();

            // 4. Allocation Trends (using FridgeAllocation)
            var allocations = await _context.FridgeAllocations
                .Where(a => a.AllocationDate >= startDate && a.AllocationDate <= endDate)
                .GroupBy(a => a.AllocationDate.Date)
                .Select(g => new AllocationTrend
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .OrderBy(g => g.Date)
                .ToListAsync();

            // 5. Rental Request Status
            var rentalRequestStatus = await _context.RentalRequests
                .GroupBy(r => r.Status)
                .Select(g => new StatusCount
                {
                    Status = g.Key.ToString(),
                    Count = g.Count()
                })
                .ToListAsync();

            // 6. Customer Status
            var customerStatus = await _context.Customers
                .GroupBy(c => c.Status)
                .Select(g => new StatusCount
                {
                    Status = g.Key.ToString(),
                    Count = g.Count()
                })
                .ToListAsync();

            // 7. Supplier Performance (Models per Supplier)
            var supplierPerformance = await _context.Suppliers
                .Select(s => new SupplierPerformanceViewModel
                {
                    SupplierName = s.Name,
                    TotalModels = s.FridgeModels.Count,
                    TotalUnits = s.FridgeModels.Sum(fm => fm.FridgeUnits.Count),
                    AvailableUnits = s.FridgeModels.Sum(fm => fm.FridgeUnits.Count(fu => fu.Status == FridgeStatus.InWarehouse)),
                    // Original properties for compatibility
                    TotalOrders = 0, // You might need to calculate this from purchase orders
                    CompletedOrders = 0,
                    PendingOrders = 0,
                    OnTimeDeliveryRate = 0,
                    QualityRating = 0
                })
                .ToListAsync();

            // 8. Maintenance Analytics
            var maintenanceStats = await _context.FridgeMaintenances
             .Where(m => m.MaintenanceDate >= startDate && m.MaintenanceDate <= endDate)
             .GroupBy(m => m.MaintenanceType)
             .Select(g => new StatusCount
             {
                 Status = g.Key.ToString(), // ✅ Convert enum to string
                 Count = g.Count()
             })
             .ToListAsync();

            // 9. Fault Analytics
            var faultStats = await _context.FridgeFaults
                .Where(f => f.ReportedDate >= startDate && f.ReportedDate <= endDate)
                .GroupBy(f => f.Status)
                .Select(g => new StatusCount
                {
                    Status = g.Key.ToString(),
                    Count = g.Count()
                })
                .ToListAsync();

            // 10. Get all fridge models for detailed view
            var allFridgeModels = await _context.FridgeModels
    .Include(fm => fm.Supplier)
    .Include(fm => fm.FridgeUnits)
    .ThenInclude(fu => fu.Location)
    .Select(fm => new FridgeModelAnalyticsViewModel
    {
        Id = fm.Id,
        Name = fm.Name,
        Category = fm.Category.ToString(),
        RentalPricePerMonth = fm.RentalPricePerMonth,
        SupplierName = fm.Supplier.Name,
        TotalUnits = fm.FridgeUnits.Count,
        AvailableUnits = fm.FridgeUnits.Count(fu => fu.Status == FridgeStatus.InWarehouse),
        UnitsInRepair = fm.FridgeUnits.Count(fu => fu.Status == FridgeStatus.InRepair),
        UnitsAllocated = fm.FridgeUnits.Count(fu => fu.Status == FridgeStatus.Allocated),
        AllocatedUnits = fm.FridgeUnits.Count(fu => fu.Status == FridgeStatus.Allocated),
        AverageUnitAge = fm.FridgeUnits.Any() ?
            (int)fm.FridgeUnits.Average(fu => (DateTime.Now - fu.PurchaseDate).TotalDays) : 0,
        UtilizationRate = fm.FridgeUnits.Count > 0 ?
            (double)fm.FridgeUnits.Count(fu => fu.Status == FridgeStatus.Allocated) / fm.FridgeUnits.Count * 100 : 0,
        Value = fm.FridgeUnits.Sum(fu => fu.PurchasePrice) // Now using the PurchasePrice property
    })
    .ToListAsync();

            var model = new AnalyticsDashboardViewModel
            {
                // Updated properties for new structure
                FridgeUnitStatus = fridgeUnitStatus,
                FridgeUnitsByLocation = fridgeUnitsByLocation,
                FridgeModelsByCategory = fridgeModelsByCategory,
                Allocations = allocations,
                RentalRequestStatus = rentalRequestStatus,
                CustomerStatus = customerStatus,
                SupplierPerformance = supplierPerformance,
                MaintenanceStats = maintenanceStats,
                FaultStats = faultStats,
                AllFridgeModels = allFridgeModels,
                StartDate = startDate.Value,
                EndDate = endDate.Value,

                // For compatibility with CustomerLiaisonAnalyticsController
                FridgeStatus = fridgeUnitStatus,
                FridgeRegion = fridgeUnitsByLocation,
                FridgeCategories = fridgeModelsByCategory,
                OrderStatus = rentalRequestStatus
            };

            return View(model);
        }
        #endregion

        #region Detailed Analytics Reports
        public async Task<IActionResult> FridgeModelReport()
        {
            var models = await _context.FridgeModels
                .Include(fm => fm.Supplier)
                .Include(fm => fm.FridgeUnits)
                .Select(fm => new FridgeModelAnalyticsViewModel
                {
                    Id = fm.Id,
                    Name = fm.Name,
                    Category = fm.Category.ToString(),
                    RentalPricePerMonth = fm.RentalPricePerMonth,
                    SupplierName = fm.Supplier.Name,
                    TotalUnits = fm.FridgeUnits.Count,
                    AvailableUnits = fm.FridgeUnits.Count(fu => fu.Status == FridgeStatus.InWarehouse),
                    UnitsInRepair = fm.FridgeUnits.Count(fu => fu.Status == FridgeStatus.InRepair),
                    UnitsAllocated = fm.FridgeUnits.Count(fu => fu.Status == FridgeStatus.Allocated),
                    AllocatedUnits = fm.FridgeUnits.Count(fu => fu.Status == FridgeStatus.Allocated),
                    UtilizationRate = fm.FridgeUnits.Count > 0 ?
                        (double)fm.FridgeUnits.Count(fu => fu.Status == FridgeStatus.Allocated) / fm.FridgeUnits.Count * 100 : 0
                })
                .OrderByDescending(fm => fm.UtilizationRate)
                .ToListAsync();

            return View(models);
        }

        public async Task<IActionResult> MaintenanceReport()
        {
            var maintenanceData = await _context.FridgeMaintenances
                .Include(m => m.FridgeUnit)
                .ThenInclude(fu => fu.FridgeModel)
                .Include(m => m.Technician)
                .Where(m => m.MaintenanceDate >= DateTime.Now.AddMonths(-6))
                .Select(m => new MaintenanceAnalyticsViewModel
                {
                    MaintenanceDate = m.MaintenanceDate,
                    FridgeModelName = m.FridgeUnit.FridgeModel.Name,
                    SerialNumber = m.FridgeUnit.SerialNumber,
                    MaintenanceType = m.MaintenanceType.ToString(), // ✅ Convert enum to string
                    TechnicianName = m.Technician != null ? m.Technician.FullName : "Unassigned",
                    Cost = m.Cost,
                    DurationHours = m.DurationHours,
                    Status = m.Status.ToString()
                })
                .OrderByDescending(m => m.MaintenanceDate)
                .ToListAsync();

            return View(maintenanceData);
        }

        public async Task<IActionResult> AllocationReport()
        {
            var allocationData = await _context.FridgeAllocations
                .Include(a => a.FridgeUnit)
                .ThenInclude(fu => fu.FridgeModel)
                .Include(a => a.Customer)
                .Where(a => a.AllocationDate >= DateTime.Now.AddMonths(-3)) // Last 3 months
                .Select(a => new AllocationAnalyticsViewModel
                {
                    AllocationDate = a.AllocationDate,
                    FridgeModelName = a.FridgeUnit.FridgeModel.Name,
                    SerialNumber = a.FridgeUnit.SerialNumber,
                    CustomerName = a.Customer.BusinessName, // Changed from Name to BusinessName
                    CustomerEmail = a.Customer.Email,
                    DeallocationDate = a.DeallocationDate,
                    AllocationDuration = a.DeallocationDate.HasValue ?
                        (a.DeallocationDate.Value - a.AllocationDate).TotalDays : 0
                })
                .OrderByDescending(a => a.AllocationDate)
                .ToListAsync();

            return View(allocationData);
        }
        #endregion

        #region Low Stock Alerts
        public async Task<IActionResult> LowStockAlerts()
        {
            var lowStockModels = await _context.FridgeModels
                .Include(fm => fm.Supplier)
                .Include(fm => fm.FridgeUnits)
                .Select(fm => new LowStockAlertViewModel
                {
                    FridgeModelId = fm.Id,
                    ModelName = fm.Name,
                    Category = fm.Category.ToString(),
                    SupplierName = fm.Supplier.Name,
                    AvailableUnits = fm.FridgeUnits.Count(fu => fu.Status == FridgeStatus.InWarehouse),
                    TotalUnits = fm.FridgeUnits.Count,
                    StockLevel = GetStockLevel(fm.FridgeUnits.Count(fu => fu.Status == FridgeStatus.InWarehouse))
                })
                .Where(fm => fm.StockLevel == StockLevel.Low || fm.StockLevel == StockLevel.Critical)
                .OrderBy(fm => fm.AvailableUnits)
                .ThenBy(fm => fm.StockLevel)
                .ToListAsync();

            return View(lowStockModels);
        }

        private StockLevel GetStockLevel(int availableUnits)
        {
            return availableUnits switch
            {
                0 => StockLevel.Critical,
                <= 2 => StockLevel.Low,
                <= 5 => StockLevel.Medium,
                _ => StockLevel.High
            };
        }
        #endregion
    }
}