using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Code_Vanta_Systems.Data;
using Code_Vanta_Systems.Models;
using Code_Vanta_Systems.ViewModels;

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
        public IActionResult Index()
        {
            return View();
        }
        #endregion

        #region Inventory Analytics
        public async Task<IActionResult> InventoryOverview(DateTime? startDate, DateTime? endDate)
        {
             
            endDate ??= DateTime.UtcNow;
            startDate ??= endDate.Value.AddDays(-1);

         
            var fridgeStatus = await _context.Fridges
                .GroupBy(f => f.Status)
                .Select(g => new StatusCount { Status = g.Key.ToString(), Count = g.Count() })
                .ToListAsync();

             
            var fridgeRegion = await _context.Fridges
                .Include(f => f.Location)
                .GroupBy(f => f.Location.Description)
                .Select(g => new StatusCount { Status = g.Key, Count = g.Count() })
                .ToListAsync();

         
            var fridgeCategories = await _context.Fridges
                .GroupBy(f => f.Category)
                .Select(g => new CategoryStockViewModel
                {
                    Category = g.Key.ToString(),
                    TotalStock = g.Sum(f => f.StockQuantity),
                    HighStock = g.Count(f => f.StockQuantity >= 15),
                    MediumStock = g.Count(f => f.StockQuantity >= 5 && f.StockQuantity < 15),
                    LowStock = g.Count(f => f.StockQuantity < 5 && f.StockQuantity > 0),
                    CriticalStock = g.Count(f => f.StockQuantity == 0)
                })
                .ToListAsync();

           
            var orderStatus = await _context.RentalRequests
                .GroupBy(r => r.Status)
                .Select(g => new StatusCount { Status = g.Key.ToString(), Count = g.Count() })
                .ToListAsync();

      
            var customerStatus = await _context.Customers
                .GroupBy(c => c.Status)
                .Select(g => new StatusCount { Status = g.Key.ToString(), Count = g.Count() })
                .ToListAsync();

  
            var allocations = await _context.FridgeAllocations
                .Where(a => a.AllocationDate >= startDate && a.AllocationDate <= endDate)
                .GroupBy(a => new { a.AllocationDate.Date, a.AllocationDate.Hour })
                .Select(g => new AllocationTrend
                {
                    Date = g.Key.Date,
                    Hour = g.Key.Hour,
                    Count = g.Count()
                })
                .OrderBy(g => g.Date)
                .ThenBy(g => g.Hour)
                .ToListAsync();

            var model = new AnalyticsDashboardViewModel
            {
                FridgeStatus = fridgeStatus,
                FridgeRegion = fridgeRegion,
                FridgeCategories = fridgeCategories,
                OrderStatus = orderStatus,
                CustomerStatus = customerStatus,
                Allocations = allocations,
                StartDate = startDate.Value,
                EndDate = endDate.Value
            };

            return View(model);
        }
        #endregion

        #region Actions for Low Stock
        [HttpPost]
        public async Task<IActionResult> RequestPurchase(string category)
        {
            var fridges = await _context.Fridges
                .Where(f => f.Category.ToString() == category && f.StockQuantity == 0)
                .ToListAsync();

            if (!fridges.Any()) return NotFound();

            // TODO: Implement actual purchase request logic here
            TempData["SuccessMessage"] = $"Purchase request sent for category '{category}' ({fridges.Count} fridges).";
            return RedirectToAction("InventoryOverview");
        }
        #endregion
    }
}
