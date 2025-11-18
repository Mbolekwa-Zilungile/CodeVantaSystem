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

        //=========================================================
        // DASHBOARD
        //=========================================================

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found. Please login again.";
                return RedirectToAction("Index", "Home");
            }

            // --- Filter faults assigned to this technician ---
            var assignedFaultsQuery = _context.FridgeFaults
                .Where(f => f.AssignedTechnicianId == user.Id);

            // Total faults assigned to this technician
            var totalLoggedFaults = await assignedFaultsQuery.CountAsync();

            // Pending faults (Open or InProgress) assigned to this technician
            var totalPending = await assignedFaultsQuery
                .CountAsync(f => f.Status == FaultStatus.Open || f.Status == FaultStatus.InProgress);

            // Total pending fridge requests (if applicable, not technician-specific)
            var totalPendingRequests = await _context.FridgeRequests
                .CountAsync(p => p.Status == RequestStatus.Pending);

            // --- Faults Over Time (for chart) ---
            var faultsDates = await assignedFaultsQuery
                .Select(f => f.DateReported)
                .ToListAsync();

            // Group faults by month for current year
            var groupedData = faultsDates
                .Where(d => d.Year == DateTime.Now.Year)
                .GroupBy(d => d.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    Count = g.Count()
                })
                .ToList();

            // Fill 12 months, even if no data (for consistent chart)
            var faultsOverTime = Enumerable.Range(1, 12)
                .Select(m => new ChartDataPoint
                {
                    Label = new DateTime(DateTime.Now.Year, m, 1).ToString("MMM"),
                    Value = groupedData
                        .Where(g => g.Month == m)
                        .Select(g => g.Count)
                        .FirstOrDefault()
                })
                .ToList();

            // --- Build ViewModel ---
            var viewModel = new CustomerDashboardViewModel
            {
                TotalLogged = totalLoggedFaults,
                TotalRequested = totalPendingRequests,
                TotalPending = totalPending,
                FaultsOverTime = faultsOverTime
            };

            return View(viewModel);
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
        public async Task<IActionResult> ProcessRepair(ProcessRepairViewModel model)
        {
            // ... (User and validation checks) ...
            var currentUser = await _userManager.GetUserAsync(User);

            // 1. Fetch the fault, related fridge, AND the specific FridgeUnit
            var fault = await _context.FridgeFaults
                .Include(f => f.Fridge)
                .Include(f => f.FridgeUnit) // <-- CRITICAL: Include the FridgeUnit
                .FirstOrDefaultAsync(f => f.Id == model.FaultId && f.AssignedTechnicianId == currentUser.Id);

            if (fault == null || fault.FridgeUnit == null)
            {
                TempData["ErrorMessage"] = "Fault not found, or the associated fridge unit is missing.";
                return RedirectToAction(nameof(Index));
            }

            // 2. Update the Fault Record
            fault.RepairDescription = model.RepairDescription;
            fault.PartsUsed = model.PartsUsed;
            fault.Status = FaultStatus.Repaired; // Set to the definitive "Repaired" status
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

                TempData["SuccessMessage"] = $"Fault {model.FaultId} successfully resolved, repaired, and unit condition updated to {model.NewFridgeCondition}.";
            }
            catch (Exception ex)
            {
                // Log the exception (not implemented here, but good practice)
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

        // The ResolveFault action is removed as its logic is merged into ProcessRepair,
        // which now handles the entire fault resolution process (repair, notes, and condition update).
    
    }
}