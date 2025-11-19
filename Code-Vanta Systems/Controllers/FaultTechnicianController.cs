using Code_Vanta_Systems.Data;
using Code_Vanta_Systems.Models;
using Code_Vanta_Systems.Models.Enums;
using Code_Vanta_Systems.ViewModels.Customer;
using Code_Vanta_Systems.ViewModels.FaultTechnician;
using Code_Vanta_Systems.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ChartDataPoint = Code_Vanta_Systems.ViewModels.ChartDataPoint;


namespace Code_Vanta_Systems.Controllers
{
    [Authorize(Roles = "Fault Technician")]
    public class FaultTechnicianController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public FaultTechnicianController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found. Please login again.";
                return RedirectToAction("Index", "Home");
            }

            var currentDate = DateTime.Now;
            var firstDayOfMonth = new DateTime(currentDate.Year, currentDate.Month, 1);
            var firstDayOfPreviousMonth = firstDayOfMonth.AddMonths(-1);

            // Base query for technician's assigned faults
            var assignedFaultsQuery = _context.FridgeFaults
                .Include(f => f.FridgeUnit)
                .ThenInclude(u => u.Customer)
                .Where(f => f.AssignedTechnicianId == user.Id);

            // Summary Statistics
            var totalAssignedFaults = await assignedFaultsQuery.CountAsync();
            var totalPendingFaults = await assignedFaultsQuery
                .CountAsync(f => f.Status == FaultStatus.Open || f.Status == FaultStatus.InProgress);
            var totalCompletedFaults = await assignedFaultsQuery
                .CountAsync(f => f.Status == FaultStatus.Repaired);
            var totalScrappedUnits = await assignedFaultsQuery
                .CountAsync(f => f.Status == FaultStatus.Scrapped);

            var replacementRequests = await _context.FridgeRequests
                .CountAsync(r => r.RequestedByUserId == user.Id &&
                                r.Notes.Contains("REPLACEMENT NEEDED"));

            // Performance Metrics
            var completedFaults = await assignedFaultsQuery
                .Where(f => f.Status == FaultStatus.Repaired && f.ResolutionDate.HasValue)
                .ToListAsync();

            var averageResolutionTime = completedFaults.Any() ?
                completedFaults.Average(f => (f.ResolutionDate.Value - f.DateReported).TotalHours) : 0;

            var currentMonthCompleted = await assignedFaultsQuery
                .CountAsync(f => f.Status == FaultStatus.Repaired &&
                                f.ResolutionDate >= firstDayOfMonth);

            var previousMonthCompleted = await assignedFaultsQuery
                .CountAsync(f => f.Status == FaultStatus.Repaired &&
                                f.ResolutionDate >= firstDayOfPreviousMonth &&
                                f.ResolutionDate < firstDayOfMonth);

            var completionRate = totalAssignedFaults > 0 ?
                (double)totalCompletedFaults / totalAssignedFaults * 100 : 0;

            // Fault Status Distribution (Pie Chart)
            var statusDistribution = await assignedFaultsQuery
                .GroupBy(f => f.Status)
                .Select(g => new
                {
                    Status = g.Key.ToString(),
                    Count = g.Count()
                })
                .ToListAsync();

            var statusChartData = statusDistribution.Select(s => new ViewModels.ChartDataPoint
            {
                Label = s.Status,
                Value = s.Count,
                Color = GetStatusColor(s.Status)
            }).ToList();

            // Severity Distribution (Pie Chart)
            var severityDistribution = await assignedFaultsQuery
                .GroupBy(f => f.Severity)
                .Select(g => new
                {
                    Severity = g.Key.ToString(),
                    Count = g.Count()
                })
                .ToListAsync();

            var severityChartData = severityDistribution.Select(s => new ViewModels.ChartDataPoint
            {
                Label = s.Severity,
                Value = s.Count,
                Color = GetSeverityColor(s.Severity)
            }).ToList();

            // Monthly Trends (Bar Chart)
            var monthlyFaults = await assignedFaultsQuery
                .Where(f => f.DateReported.Year == currentDate.Year)
                .GroupBy(f => f.DateReported.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    Count = g.Count(),
                    Completed = g.Count(f => f.Status == FaultStatus.Repaired)
                })
                .ToListAsync();

            var monthlyTrends = Enumerable.Range(1, 12)
                .Select(month => new ViewModels.ChartDataPoint
                {
                    Label = new DateTime(currentDate.Year, month, 1).ToString("MMM"),
                    Value = monthlyFaults.Where(m => m.Month == month).Sum(m => m.Count),
                    Color = "#3b82f6"
                })
                .ToList();

            // Monthly Performance (Line Chart)
            var monthlyPerformance = Enumerable.Range(1, 12)
                .Select(month => new ChartDataPoint
                {
                    Label = new DateTime(currentDate.Year, month, 1).ToString("MMM"),
                    Value = monthlyFaults.Where(m => m.Month == month).Sum(m => m.Completed),
                    Color = "#10b981"
                })
                .ToList();

            // Recent Faults
            var recentFaults = await assignedFaultsQuery
                .OrderByDescending(f => f.DateReported)
                .Take(5)
                .Select(f => new RecentFault
                {
                    Id = f.Id,
                    SerialNumber = f.FridgeUnit.SerialNumber,
                    Customer = f.FridgeUnit.Customer.BusinessName,
                    Status = f.Status.ToString(),
                    Severity = f.Severity.ToString(),
                    DateReported = f.DateReported,
                    DaysOpen = (int)(DateTime.Now - f.DateReported).TotalDays
                })
                .ToListAsync();

            // Performance Metrics
            var performanceMetrics = new List<PerformanceMetric>
    {
        new PerformanceMetric
        {
            Metric = "Completion Rate",
            Value = $"{completionRate:0.0}%",
            Trend = completionRate >= 80 ? "up" : completionRate >= 60 ? "stable" : "down",
            TrendValue = "Target: 80%"
        },
        new PerformanceMetric
        {
            Metric = "Avg Resolution Time",
            Value = $"{averageResolutionTime:0.0} hours",
            Trend = averageResolutionTime < 24 ? "up" : averageResolutionTime < 48 ? "stable" : "down",
            TrendValue = "Target: <24h"
        },
        new PerformanceMetric
        {
            Metric = "Monthly Completed",
            Value = currentMonthCompleted.ToString(),
            Trend = currentMonthCompleted > previousMonthCompleted ? "up" :
                   currentMonthCompleted < previousMonthCompleted ? "down" : "stable",
            TrendValue = $"Prev: {previousMonthCompleted}"
        }
    };

            // Top Issue Type
            //var topIssue = await assignedFaultsQuery
            //    .Where(f => !string.IsNullOrEmpty(f.FaultDescription))
            //    .GroupBy(f => f.FaultDescription.Split(' ').FirstOrDefault() ?? "Unknown")
            //    .Select(g => new { Issue = g.Key, Count = g.Count() })
            //    .OrderByDescending(g => g.Count)
            //    .FirstOrDefaultAsync();

            // Build ViewModel
            var viewModel = new TechnicianDashboardViewModel
            {
                TotalAssignedFaults = totalAssignedFaults,
                TotalPendingFaults = totalPendingFaults,
                TotalCompletedFaults = totalCompletedFaults,
                TotalScrappedUnits = totalScrappedUnits,
                ReplacementRequests = replacementRequests,
                AverageResolutionTimeHours = averageResolutionTime,

                FaultsOverTime = monthlyTrends,
                StatusDistribution = statusChartData,
                SeverityDistribution = severityChartData,
                MonthlyPerformance = monthlyPerformance,

                RecentFaults = recentFaults,
                PerformanceMetrics = performanceMetrics,

                CurrentMonthCompleted = currentMonthCompleted,
                PreviousMonthCompleted = previousMonthCompleted,
                CompletionRate = completionRate
            };

            return View(viewModel);
        }

        // Helper methods for chart colors
        private string GetStatusColor(string status)
        {
            return status switch
            {
                "Open" => "#ef4444",
                "InProgress" => "#f59e0b",
                "Repaired" => "#10b981",
                "Scrapped" => "#6b7280",
                _ => "#9ca3af"
            };
        }

        private string GetSeverityColor(string severity)
        {
            return severity switch
            {
                "Low" => "#10b981",
                "Medium" => "#f59e0b",
                "High" => "#ef4444",
                "Critical" => "#7c2d12",
                _ => "#9ca3af"
            };
        }

        //=========================================================
        // VIEW FAULTS (INDEX) - Technician's Task List
        //=========================================================

        // View faults
        [HttpGet]
        public async Task<IActionResult> Index(string search, int pageIndex = 1)
        {
            const int pageSize = 10;

            // Get current logged-in technician
            var currentUser = await _userManager.GetUserAsync(User);

            // Base query: faults assigned to this technician
            var query = _context.FridgeFaults
                // CRITICAL FIX: Include FridgeUnit and Customer for context
                .Include(f => f.FridgeUnit)
                    .ThenInclude(u => u.Customer)
                .Include(f => f.ReportedBy)
                .Where(f => f.AssignedTechnicianId == currentUser.Id)
                .AsQueryable();

            // Apply search if provided (search by unit serial or description)
            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                query = query.Where(f =>
                    // Search using the Unit's serial number
                    f.FridgeUnit.SerialNumber.ToLower().Contains(search) ||
                    f.FaultDescription.ToLower().Contains(search)
                );
            }

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Apply pagination
            var faults = await query
                .OrderByDescending(f => f.DateReported)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Calculate total pages
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            // Pass pagination info to view
            ViewData["currentPage"] = pageIndex;
            ViewData["totalPages"] = totalPages;
            ViewData["search"] = search;

            return View(faults);
        }

        //=========================================================
        // FAULT DETAILS & PROCESSING
        //=========================================================

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            // Get current logged-in technician
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                TempData["ErrorMessage"] = "User not found. Please login again.";
                return RedirectToAction(nameof(Index));
            }

            // Find the fault assigned to this technician
            var fault = await _context.FridgeFaults
                // CRITICAL FIX: Include FridgeUnit and its Customer/Fridge Model for full context
                .Include(f => f.FridgeUnit)
                    .ThenInclude(u => u.Customer)
                .Include(f => f.FridgeUnit)
                    .ThenInclude(u => u.Fridge)
                .Include(f => f.ReportedBy)
                .Include(f => f.AssignedTechnician)
                .FirstOrDefaultAsync(f => f.Id == id && f.AssignedTechnicianId == currentUser.Id);

            if (fault == null)
            {
                TempData["ErrorMessage"] = "Fault not found or you are not assigned to this fault.";
                return RedirectToAction(nameof(Index));
            }

            // Optionally, load audit trail for this fault
            var audits = await _context.AuditTrails
                .Where(a => a.EntityName == "FridgeFault" && a.EntityId == fault.Id)
                .OrderByDescending(a => a.ChangeDate)
                .ToListAsync();

            ViewData["Audits"] = audits;

            return View(fault);
        }

        // Process fault / Repair fridge / Update fridge condition
        [HttpPost]
        [ValidateAntiForgeryToken]
        // Process fault / Repair fridge / Update fridge condition
        
        public async Task<IActionResult> ProcessRepair(ProcessRepairViewModel model)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            // 1. Fetch the fault, related fridge, AND the specific FridgeUnit
            var fault = await _context.FridgeFaults
                .Include(f => f.Fridge)
                .Include(f => f.FridgeUnit)
                .ThenInclude(u => u.Customer)
                .Include(f => f.FridgeUnit)
                .ThenInclude(u => u.Fridge.Location)
                .FirstOrDefaultAsync(f => f.Id == model.FaultId && f.AssignedTechnicianId == currentUser.Id);

            if (fault == null || fault.FridgeUnit == null)
            {
                TempData["ErrorMessage"] = "Fault not found, or the associated fridge unit is missing.";
                return RedirectToAction(nameof(Index));
            }

            // 2. Update the Fault Record
            fault.RepairDescription = model.RepairDescription;
            fault.PartsUsed = model.PartsUsed;
            fault.Status = FaultStatus.Scrapped; // Set to Scrapped status
            fault.ResolutionDate = DateTime.Now;

            // 3. Update the FRIDGE UNIT Condition
            fault.FridgeUnit.Condition = model.NewFridgeCondition;
            fault.FridgeUnit.TechnicianUserId = null; // Clear technician from the Unit

            // Also update the unit's status if it's no longer 'Faulty'
            if (model.NewFridgeCondition == FridgeCondition.Operational)
            {
                fault.FridgeUnit.Status = FridgeStatus.Operational;
            }
            else if (model.NewFridgeCondition == FridgeCondition.Scrap)
            {
                fault.FridgeUnit.Status = FridgeStatus.Scrapped;
            }

            // 4. Create Audit Trail
            var audit = new AuditTrail
            {
                EntityName = "FridgeFault",
                EntityId = fault.Id,
                ChangedByUserId = currentUser.Id,
                ChangeType = "Repair/Resolution",
                ChangeDate = DateTime.Now,
                ChangeDetails = $"Fault Resolved. Unit condition updated to '{model.NewFridgeCondition}'. Notes: {model.RepairDescription}."
            };

            // 5. Save changes
            try
            {
                _context.Update(fault);
                _context.Update(fault.FridgeUnit);
                _context.AuditTrails.Add(audit);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Fault {model.FaultId} successfully resolved. Unit marked as {model.NewFridgeCondition}.";

                // 6. If unit is scrapped, redirect to replacement booking
                if (model.NewFridgeCondition == FridgeCondition.Scrap)
                {
                    return RedirectToAction(nameof(BookReplacement), new { faultId = fault.Id });
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred during repair processing: " + ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id = model.FaultId });
        }

        //=========================================================
        // QUICK STATUS UPDATE
        //=========================================================

        // This action handles quick status changes (e.g., from Open to InProgress)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, FaultStatus newStatus)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                TempData["ErrorMessage"] = "User not found. Please log in again.";
                return RedirectToAction("Index", "Home");
            }

            var fault = await _context.FridgeFaults
                .Include(f => f.FridgeUnit) // Include unit for context
                .FirstOrDefaultAsync(f => f.Id == id && f.AssignedTechnicianId == currentUser.Id);

            if (fault == null)
            {
                TempData["ErrorMessage"] = "Fault not found or you are not assigned to this fault.";
                return RedirectToAction(nameof(Index));
            }

            // Prevent changing status of already resolved/scrapped faults via quick update
            if (fault.Status == FaultStatus.Repaired || fault.Status == FaultStatus.Scrapped)
            {
                TempData["ErrorMessage"] = "Cannot change the status of a resolved or scrapped fault.";
                return RedirectToAction(nameof(Index));
            }

            // 1. Update the status
            var oldStatus = fault.Status;
            fault.Status = newStatus;

            // 2. Add Audit Trail entry
            var audit = new AuditTrail
            {
                EntityName = "FridgeFault",
                EntityId = fault.Id,
                ChangeType = "Status Update",
                ChangeDetails = $"Status changed from {oldStatus} to {newStatus} via quick update.",
                ChangeDate = DateTime.Now,
                ChangedByUserId = currentUser.Id
            };

            // 3. Optional: Assign the Technician to the Unit if status is 'InProgress'
            if (newStatus == FaultStatus.InProgress)
            {
                if (fault.FridgeUnit != null)
                {
                    fault.FridgeUnit.TechnicianUserId = currentUser.Id;
                    _context.Update(fault.FridgeUnit);
                }
            }


            try
            {
                _context.Update(fault);
                _context.AuditTrails.Add(audit);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Status for Fault {id} successfully updated to {newStatus}.";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = $"An error occurred while updating the status. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }


        [HttpGet]
        public async Task<IActionResult> BookReplacement(int faultId)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            // Get the scrapped fault details with proper includes
            var fault = await _context.FridgeFaults
                .Include(f => f.FridgeUnit)
                .ThenInclude(u => u.Customer)
                .Include(f => f.FridgeUnit)
                .ThenInclude(u => u.Fridge.Location)
                .FirstOrDefaultAsync(f => f.Id == faultId && f.AssignedTechnicianId == currentUser.Id && f.FridgeUnit.Condition == FridgeCondition.Scrap);

            if (fault == null)
            {
                TempData["ErrorMessage"] = "Fault not found or unit is not marked as scrap.";
                return RedirectToAction(nameof(Index));
            }

            // CRITICAL: Ensure the Customer and Location exist
            if (fault.FridgeUnit.Customer == null || fault.FridgeUnit.Fridge.Location == null)
            {
                TempData["ErrorMessage"] = "Customer or location information is missing for this fault.";
                return RedirectToAction(nameof(Details), new { id = faultId });
            }

            var viewModel = new ReplacementBookingViewModel
            {
                FaultId = fault.Id,
                CustomerId = (int)fault.FridgeUnit.CustomerId,
                LocationId = fault.FridgeUnit.Fridge.LocationId,
                CustomerName = fault.FridgeUnit.Customer.BusinessName,
                LocationName = fault.FridgeUnit.Fridge.Location.Address,
                ScrappedUnitSerial = fault.FridgeUnit.SerialNumber
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
       
        public async Task<IActionResult> BookReplacement(ReplacementBookingViewModel model)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Verify the fault exists and is assigned to current technician
            var fault = await _context.FridgeFaults
                .Include(f => f.FridgeUnit)
                .ThenInclude(u => u.Customer)
                .Include(f => f.FridgeUnit)
                .ThenInclude(u => u.Fridge.Location)
                .FirstOrDefaultAsync(f => f.Id == model.FaultId && f.AssignedTechnicianId == currentUser.Id);

            if (fault == null)
            {
                TempData["ErrorMessage"] = "Fault not found or you are not assigned to this fault.";
                return RedirectToAction(nameof(Index));
            }

            // CRITICAL FIX: Validate that the Customer exists
            var customerExists = await _context.Customers.AnyAsync(c => c.Id == model.CustomerId);
            if (!customerExists)
            {
                TempData["ErrorMessage"] = "Invalid customer. Please try again.";
                return View(model);
            }

            // CRITICAL FIX: Validate that the Location exists
            var locationExists = await _context.Locations.AnyAsync(l => l.Id == model.LocationId);
            if (!locationExists)
            {
                TempData["ErrorMessage"] = "Invalid location. Please try again.";
                return View(model);
            }

            try
            {
                // Create replacement fridge request
                var replacementRequest = new FridgeRequest
                {
                    CustomerId = (int)fault.FridgeUnit.CustomerId, // Use the CustomerId from the fault's FridgeUnit
                    LocationId = fault.FridgeUnit.Fridge.LocationId, // Use the LocationId from the fault's FridgeUnit
                    RequestedByUserId = currentUser.Id,
                    RequestedAt = DateTime.Now,
                    Notes = $"REPLACEMENT NEEDED for scrapped unit: {fault.FridgeUnit.SerialNumber}. {model.Notes}",
                    Status = RequestStatus.Pending
                };

                _context.FridgeRequests.Add(replacementRequest);
                await _context.SaveChangesAsync();

                // Create audit trail for the replacement booking
                var audit = new AuditTrail
                {
                    EntityName = "FridgeFault",
                    EntityId = fault.Id,
                    ChangedByUserId = currentUser.Id,
                    ChangeType = "Replacement Booked",
                    ChangeDate = DateTime.Now,
                    ChangeDetails = $"Replacement fridge requested for scrapped unit {fault.FridgeUnit.SerialNumber}. Request ID: {replacementRequest.Id}"
                };
                _context.AuditTrails.Add(audit);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Replacement request successfully submitted! Request ID: {replacementRequest.Id}";
                return RedirectToAction(nameof(Details), new { id = model.FaultId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while submitting the replacement request: " + ex.Message;
                return View(model);
            }
        }

    }
}