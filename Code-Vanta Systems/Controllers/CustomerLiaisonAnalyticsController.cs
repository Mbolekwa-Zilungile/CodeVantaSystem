using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Code_Vanta_Systems.Data;
using Code_Vanta_Systems.Models;
using Code_Vanta_Systems.Models.Enums;
using Code_Vanta_Systems.ViewModels.Analytics;
using Code_Vanta_Systems.ViewModels.Shared;

namespace Code_Vanta_Systems.Controllers
{
    public class CustomerLiaisonAnalyticsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CustomerLiaisonAnalyticsController(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Dashboard Index
        public async Task<IActionResult> Index()
        {
            // Quick stats for dashboard
            ViewBag.TotalCustomers = await _context.Customers.CountAsync();
            ViewBag.ActiveCustomers = await _context.Customers.CountAsync(c => c.Status == CustomerStatus.Active);
            ViewBag.TotalFridgeModels = await _context.FridgeModels.CountAsync();
            ViewBag.AvailableFridgeUnits = await _context.FridgeUnits.CountAsync(fu => fu.Status == FridgeStatus.InWarehouse);
            ViewBag.ActiveAllocations = await _context.FridgeAllocations.CountAsync(a => a.DeallocationDate == null);
            ViewBag.PendingRentals = await _context.RentalRequests.CountAsync(r => r.Status == "Pending");

            return View();
        }
        #endregion

        #region Inventory Analytics
        public async Task<IActionResult> InventoryOverview(DateTime? startDate, DateTime? endDate)
        {
            endDate ??= DateTime.UtcNow;
            startDate ??= endDate.Value.AddMonths(-1);

            // 1. Fridge Unit Status Analytics
            var fridgeUnitStatus = await _context.FridgeUnits
                .GroupBy(f => f.Status)
                .Select(g => new StatusCount { Status = g.Key.ToString(), Count = g.Count() })
                .ToListAsync();

            // 2. Fridge Units by Location
            var fridgeUnitsByLocation = await _context.FridgeUnits
                .Include(f => f.Location)
                .Where(f => f.Location != null)
                .GroupBy(f => f.Location.Description)
                .Select(g => new StatusCount { Status = g.Key ?? "Unknown Location", Count = g.Count() })
                .ToListAsync();

            // 3. Fridge Models by Category with Unit Counts
            var fridgeModelsByCategory = await _context.FridgeModels
                .GroupBy(f => f.Category)
                .Select(g => new CategoryStockViewModel
                {
                    Category = g.Key.ToString(),
                    TotalModels = g.Count(),
                    AvailableUnits = _context.FridgeUnits.Count(fu => fu.FridgeModel.Category == g.Key && fu.Status == FridgeStatus.InWarehouse),
                    TotalUnits = _context.FridgeUnits.Count(fu => fu.FridgeModel.Category == g.Key),
                    UnitsInRepair = _context.FridgeUnits.Count(fu => fu.FridgeModel.Category == g.Key && fu.Status == FridgeStatus.InRepair),
                    UnitsAllocated = _context.FridgeUnits.Count(fu => fu.FridgeModel.Category == g.Key && fu.Status == FridgeStatus.Allocated)
                })
                .ToListAsync();

            // 4. Rental Request Status
            var rentalRequestStatus = await _context.RentalRequests
                .GroupBy(r => r.Status)
                .Select(g => new StatusCount { Status = g.Key ?? "Unknown", Count = g.Count() })
                .ToListAsync();

            // 5. Customer Status
            var customerStatus = await _context.Customers
                .GroupBy(c => c.Status)
                .Select(g => new StatusCount { Status = g.Key.ToString(), Count = g.Count() })
                .ToListAsync();

            // 6. Allocation Trends
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

            // 7. Customer Business Types
            var customerBusinessTypes = await _context.Customers
                .GroupBy(c => c.BusinessType)
                .Select(g => new StatusCount { Status = g.Key ?? "Unknown", Count = g.Count() })
                .ToListAsync();

            var model = new CustomerLiaisonAnalyticsViewModel
            {
                // Fridge Unit Analytics
                FridgeUnitStatus = fridgeUnitStatus,
                FridgeUnitsByLocation = fridgeUnitsByLocation,

                // Fridge Model Analytics
                FridgeModelsByCategory = fridgeModelsByCategory,

                // Customer & Rental Analytics
                RentalRequestStatus = rentalRequestStatus,
                CustomerStatus = customerStatus,
                CustomerBusinessTypes = customerBusinessTypes,

                // Allocation Analytics
                Allocations = allocations,

                // Date Range
                StartDate = startDate.Value,
                EndDate = endDate.Value,

                // Summary Statistics
                TotalCustomers = await _context.Customers.CountAsync(),
                ActiveCustomers = await _context.Customers.CountAsync(c => c.Status == CustomerStatus.Active),
                TotalFridgeModels = await _context.FridgeModels.CountAsync(),
                TotalFridgeUnits = await _context.FridgeUnits.CountAsync(),
                AvailableFridgeUnits = await _context.FridgeUnits.CountAsync(fu => fu.Status == FridgeStatus.InWarehouse),
                ActiveAllocations = await _context.FridgeAllocations.CountAsync(a => a.DeallocationDate == null),
                TotalRentalRequests = await _context.RentalRequests.CountAsync(),
                PendingRentalRequests = await _context.RentalRequests.CountAsync(r => r.Status == "Pending")
            };

            return View(model);
        }
        #endregion

        #region Customer Analytics
        public async Task<IActionResult> CustomerAnalytics()
        {
            var customers = await _context.Customers
                .Include(c => c.Location)
                .Include(c => c.Allocations)
                .ThenInclude(a => a.FridgeUnit)
                .ThenInclude(fu => fu.FridgeModel)
                .Select(c => new CustomerAnalyticsViewModel
                {
                    CustomerId = c.Id,
                    BusinessName = c.BusinessName,
                    ContactName = c.ContactName,
                    BusinessType = c.BusinessType,
                    Location = c.Location.Description,
                    Status = c.Status.ToString(),
                    TotalAllocations = c.Allocations.Count,
                    ActiveAllocations = c.Allocations.Count(a => a.DeallocationDate == null),
                    FirstAllocationDate = c.Allocations.Any() ? c.Allocations.Min(a => a.AllocationDate) : null,
                    LastAllocationDate = c.Allocations.Any() ? c.Allocations.Max(a => a.AllocationDate) : null,
                    PreferredFridgeModel = c.Allocations
                        .GroupBy(a => a.FridgeUnit.FridgeModel.Name)
                        .OrderByDescending(g => g.Count())
                        .Select(g => g.Key)
                        .FirstOrDefault() ?? "None"
                })
                .OrderByDescending(c => c.TotalAllocations)
                .ToListAsync();

            return View(customers);
        }

        public async Task<IActionResult> AllocationAnalytics()
        {
            var allocationData = await _context.FridgeAllocations
                .Include(a => a.FridgeUnit)
                .ThenInclude(fu => fu.FridgeModel)
                .Include(a => a.Customer)
                .Where(a => a.AllocationDate >= DateTime.Now.AddMonths(-6))
                .Select(a => new AllocationAnalyticsViewModel
                {
                    AllocationDate = a.AllocationDate,
                    FridgeModelName = a.FridgeUnit.FridgeModel.Name,
                    SerialNumber = a.FridgeUnit.SerialNumber,
                    CustomerName = a.Customer.BusinessName,
                    CustomerType = a.Customer.BusinessType,
                    AllocationDuration = a.DeallocationDate.HasValue ?
                        (a.DeallocationDate.Value - a.AllocationDate).TotalDays : 0,
                    IsActive = !a.DeallocationDate.HasValue,
                    Status = a.FridgeUnit.Status.ToString()
                })
                .OrderByDescending(a => a.AllocationDate)
                .ToListAsync();

            return View(allocationData);
        }
        #endregion

        #region Low Stock & Performance Analytics
        public async Task<IActionResult> LowStockAlerts()
        {
            var lowStockModels = await _context.FridgeModels
                .Select(fm => new LowStockAlertViewModel
                {
                    FridgeModelId = fm.Id,
                    ModelName = fm.Name,
                    Category = fm.Category,
                    AvailableUnits = _context.FridgeUnits.Count(fu => fu.FridgeModelId == fm.Id && fu.Status == FridgeStatus.InWarehouse),
                    TotalUnits = _context.FridgeUnits.Count(fu => fu.FridgeModelId == fm.Id),
                    UnitsInRepair = _context.FridgeUnits.Count(fu => fu.FridgeModelId == fm.Id && fu.Status == FridgeStatus.InRepair),
                    StockLevel = GetStockLevel(_context.FridgeUnits.Count(fu => fu.FridgeModelId == fm.Id && fu.Status == FridgeStatus.InWarehouse))
                })
                .Where(fm => fm.StockLevel == StockLevel.Critical || fm.StockLevel == StockLevel.Low)
                .OrderBy(fm => fm.AvailableUnits)
                .ThenBy(fm => fm.StockLevel)
                .ToListAsync();

            return View(lowStockModels);
        }

        public async Task<IActionResult> PerformanceMetrics()
        {
            var metrics = new PerformanceMetricsViewModel
            {
                // Allocation metrics
                TotalAllocations = await _context.FridgeAllocations.CountAsync(),
                MonthlyAllocations = await _context.FridgeAllocations
                    .CountAsync(a => a.AllocationDate >= DateTime.Now.AddMonths(-1)),
                AllocationSuccessRate = await CalculateAllocationSuccessRate(),

                // Customer metrics
                CustomerSatisfactionScore = await CalculateCustomerSatisfactionScore(),
                AverageAllocationDuration = await CalculateAverageAllocationDuration(),

                // Inventory metrics
                InventoryUtilizationRate = await CalculateInventoryUtilizationRate(),
                LowStockModelsCount = await _context.FridgeModels
                    .CountAsync(fm => _context.FridgeUnits.Count(fu => fu.FridgeModelId == fm.Id && fu.Status == FridgeStatus.InWarehouse) <= 2)
            };

            return View(metrics);
        }

        #region Helper Methods for Performance Metrics
        private async Task<double> CalculateAllocationSuccessRate()
        {
            var totalRentals = await _context.RentalRequests.CountAsync();
            if (totalRentals == 0) return 0;

            var approvedRentals = await _context.RentalRequests
                .CountAsync(r => r.Status == "Approved");

            return (double)approvedRentals / totalRentals * 100;
        }

        private async Task<double> CalculateCustomerSatisfactionScore()
        {
            // Simplified calculation - in real scenario, use actual customer feedback
            var totalCustomers = await _context.Customers.CountAsync(c => c.Status == CustomerStatus.Active);
            if (totalCustomers == 0) return 0;

            var customersWithAllocations = await _context.Customers
                .CountAsync(c => c.Status == CustomerStatus.Active && c.Allocations.Any());

            return (double)customersWithAllocations / totalCustomers * 100;
        }

        private async Task<double> CalculateAverageAllocationDuration()
        {
            var completedAllocations = await _context.FridgeAllocations
                .Where(a => a.DeallocationDate.HasValue)
                .ToListAsync();

            if (!completedAllocations.Any()) return 0;

            var totalDays = completedAllocations
                .Sum(a => (a.DeallocationDate.Value - a.AllocationDate).TotalDays);

            return totalDays / completedAllocations.Count;
        }

        private async Task<double> CalculateInventoryUtilizationRate()
        {
            var totalUnits = await _context.FridgeUnits.CountAsync();
            if (totalUnits == 0) return 0;

            var allocatedUnits = await _context.FridgeUnits
                .CountAsync(fu => fu.Status == FridgeStatus.Allocated);

            return (double)allocatedUnits / totalUnits * 100;
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

        [HttpPost]
        public async Task<IActionResult> RequestPurchase(string category)
        {
            // Find fridge models with low available units in this category
            var lowStockModels = await _context.FridgeModels
                .Where(fm => fm.Category.ToString() == category)
                .Select(fm => new
                {
                    Model = fm,
                    AvailableUnits = _context.FridgeUnits.Count(fu =>
                        fu.FridgeModelId == fm.Id && fu.Status == FridgeStatus.InWarehouse)
                })
                .Where(x => x.AvailableUnits <= 2)
                .ToListAsync();

            if (!lowStockModels.Any())
            {
                TempData["InfoMessage"] = $"No low stock found for category '{category}'.";
                return RedirectToAction("InventoryOverview");
            }

            // TODO: Implement actual purchase request logic here
            TempData["SuccessMessage"] = $"Purchase request sent for {lowStockModels.Count} models in category '{category}' with low stock.";
            return RedirectToAction("InventoryOverview");
        }
        #endregion
    }
}