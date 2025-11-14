using Code_Vanta_Systems.Data;
using Code_Vanta_Systems.Models;
using Code_Vanta_Systems.Models.Enums;
using Code_Vanta_Systems.ViewModels.Customer;
using Code_Vanta_Systems.ViewModels.FaultTechnician;
using Code_Vanta_Systems.ViewModels.Maintenance_Schedule;
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

        // View Assigned Faults
        [HttpGet]
        public async Task<IActionResult> Index(string search, int pageIndex = 1)
        {
            const int pageSize = 10;

            // Get current logged-in technician
            var currentUser = await _userManager.GetUserAsync(User);

            // Base query: faults assigned to this technician
            var query = _context.FridgeFaults
                .Include(f => f.Fridge)
                .Include(f => f.ReportedBy)
                .Where(f => f.AssignedTechnicianId == currentUser.Id)
                .AsQueryable();

            // Apply search if provided (search by fridge serial or description)
            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                query = query.Where(f =>
                    f.Fridge.SerialNumber.ToLower().Contains(search) ||
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

        [HttpPost]
        public async Task<IActionResult> EditStatus(int id, FaultStatus status)
        {
            try
            {
                // 1️⃣ Get current technician
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    TempData["ErrorMessage"] = "User not found. Please login again.";
                    return RedirectToAction(nameof(Index));
                }

                // 2️⃣ Find the fault
                var fault = await _context.FridgeFaults
                    .Include(f => f.Fridge)
                    .Include(f => f.ReportedBy)
                    .Include(f => f.AssignedTechnician)
                    .FirstOrDefaultAsync(f => f.Id == id && f.AssignedTechnicianId == currentUser.Id);

                if (fault == null)
                {
                    TempData["ErrorMessage"] = "Fault not found or you are not assigned to this fault.";
                    return RedirectToAction(nameof(Index));
                }

                // 3️⃣ Update status
                var oldStatus = fault.Status;
                fault.Status = status;

                // Optionally set ResolutionDate if status is Resolved
                if (status == FaultStatus.Resolved)
                {
                    fault.ResolutionDate = DateTime.Now;
                }

                _context.FridgeFaults.Update(fault);
                await _context.SaveChangesAsync();

                // 4️⃣ Create AuditTrail
                var audit = new AuditTrail
                {
                    EntityName = "FridgeFault",
                    EntityId = fault.Id,
                    ChangedByUserId = currentUser.Id,
                    ChangeType = "Update",
                    ChangeDate = DateTime.Now,
                    ChangeDetails = $"Status changed from '{oldStatus}' to '{status}' by technician '{currentUser.FullName}'."
                };

                _context.AuditTrails.Add(audit);
                await _context.SaveChangesAsync();

                // 5️⃣ Create notification to Admin or Reporting User
                var notification = new Notification
                {
                    RecipientUserId = fault.ReportedByUserId, // notify user who reported the fault
                    Title = "Fridge Fault Status Updated",
                    Message = $"Fridge Fault (Serial: {fault.Fridge.SerialNumber}) status changed from '{oldStatus}' to '{status}' by technician '{currentUser.FullName}'.",
                    CreatedAt = DateTime.Now,
                    IsRead = false,
                    TargetController = "AdminFaults",
                    TargetAction = "Index"
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Status updated to '{status}' successfully.";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while updating the status. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

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
                .Include(f => f.Fridge)
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessRepair(ProcessRepairViewModel model)
        {
            // ... (Validation and current user retrieval remain the same) ...
            var currentUser = await _userManager.GetUserAsync(User);

            // 1. Fetch the fault, related fridge, AND the specific FridgeUnit
            var fault = await _context.FridgeFaults
                .Include(f => f.Fridge)
                .Include(f => f.FridgeUnit) // <-- MUST include the FridgeUnit
                .FirstOrDefaultAsync(f => f.Id == model.FaultId && f.AssignedTechnicianId == currentUser.Id);

            if (fault == null || fault.FridgeUnit == null) // <-- Check for Unit existence
            {
                TempData["ErrorMessage"] = "Fault not found, or the associated fridge unit is missing.";
                return RedirectToAction(nameof(Index));
            }

            // 2. Process fault / Repair unit (Update Fault Record)
            // ... (Fault updates remain the same) ...

            // 3. Update unit condition (CRITICAL FIX)
            fault.FridgeUnit.Condition = model.NewFridgeCondition;
            // Clear the technician from the Unit since the repair is 'processed'
            fault.FridgeUnit.TechnicianUserId = null;

            // Save changes and create an Audit Trail entry
            try
            {
                _context.Update(fault);
                _context.Update(fault.FridgeUnit); // <-- Update the specific Unit entity
                                                   // If necessary, also update the generic fault.Fridge if it still tracks state
                                                   // _context.Update(fault.Fridge); 

                // Add Audit Trail for fault resolution (Remains the same)
                // ...

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Fault {model.FaultId} successfully resolved and unit condition updated to {model.NewFridgeCondition}.";
            }
            catch (Exception)
            {
                // ... (Error handling remains the same) ...
            }

            return RedirectToAction(nameof(Details), new { id = model.FaultId });
        }

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
                .FirstOrDefaultAsync(f => f.Id == id && f.AssignedTechnicianId == currentUser.Id);

            if (fault == null)
            {
                TempData["ErrorMessage"] = "Fault not found or you are not assigned to this fault.";
                return RedirectToAction(nameof(Index));
            }

            // Only allow update if the fault is not already resolved/scrapped.
            if (fault.Status == FaultStatus.Resolved || fault.Status == FaultStatus.Scrapped)
            {
                TempData["ErrorMessage"] = "Cannot change the status of a resolved or scrapped fault. Use the Details page if re-opening.";
                return RedirectToAction(nameof(Index));
            }

            // 1. Update the status
            fault.Status = newStatus;

            // 2. Add Audit Trail entry for the quick status change
            var audit = new AuditTrail
            {
                EntityName = "FridgeFault",
                EntityId = fault.Id,
                ChangeType = "Status Update",
                ChangeDetails = $"Status changed to {newStatus} via quick update.",
                ChangeDate = DateTime.Now,
                ChangedByUserId = currentUser.Id
            };

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResolveFault(int faultId, bool requiresMaintenance, string resolutionNotes)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                TempData["ErrorMessage"] = "User not found. Please login again.";
                return RedirectToAction(nameof(Index));
            }

            // CRITICAL: Include Fridge and FridgeUnit to allow condition update
            var fault = await _context.FridgeFaults
                .Include(f => f.Fridge)
                .Include(f => f.FridgeUnit) // Include the specific unit associated with the fault
                .FirstOrDefaultAsync(f => f.Id == faultId && f.AssignedTechnicianId == currentUser.Id);

            if (fault == null || fault.FridgeUnit == null)
            {
                TempData["ErrorMessage"] = "Fault not found or the associated fridge unit is missing.";
                return RedirectToAction(nameof(Index));
            }

            // 1. Update the Fault Status
            fault.Status = FaultStatus.Resolved;
            fault.ResolutionDate = DateTime.Now;

            // 2. Prepare Audit Trail Entry (Declared before try/catch to resolve CS0103)
            FridgeCondition newUnitCondition;

            // 3. Update the FRIDGE UNIT Condition based on technician input
            if (requiresMaintenance)
            {
                // Unit needs maintenance. It's still at the customer.
                newUnitCondition = FridgeCondition.RequireMaintenance;
                fault.FridgeUnit.Condition = newUnitCondition;
                fault.FridgeUnit.TechnicianUserId = null; // Clear technician from unit, making it available for Maintenance Oversight

                TempData["SuccessMessage"] = $"Fault {faultId} resolved. Unit flagged for Maintenance Oversight.";
            }
            else if (fault.Fridge.Status == FridgeStatus.ReturnedToStock)
            {
                // Unit is being returned. Clear allocation and set a clean condition.
                newUnitCondition = FridgeCondition.Operational; // Ready to be checked back into stock
                fault.FridgeUnit.Condition = newUnitCondition;
                fault.FridgeUnit.CustomerId = null; // This is correct here
                fault.FridgeUnit.TechnicianUserId = null;
            }
            else
            {
                // Fault resolved, unit remains operational at customer site.
                newUnitCondition = FridgeCondition.Operational;
                fault.FridgeUnit.Condition = newUnitCondition;
                fault.FridgeUnit.TechnicianUserId = null;
            }

            // Create Audit Trail Entry after determining the final condition
            var audit = new AuditTrail
            {
                EntityName = "FridgeFault",
                EntityId = fault.Id,
                ChangeType = "Fault Resolution",
                ChangeDetails = $"Fault Resolved. Notes: {resolutionNotes}. Unit Condition set to {newUnitCondition}.",
                ChangeDate = DateTime.Now,
                ChangedByUserId = currentUser.Id
            };

            try
            {
                _context.Update(fault);
                _context.Update(fault.FridgeUnit); // Ensure the Unit object changes are saved
                _context.AuditTrails.Add(audit);
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred during fault resolution. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
