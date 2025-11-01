using Code_Vanta_Systems.Data;
using Code_Vanta_Systems.Models;
using Code_Vanta_Systems.Models.Enums;
using Code_Vanta_Systems.ViewModels.Shared;
using Code_Vanta_Systems.ViewModels.FaultTechnician;
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
            try
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

                // Total completed faults
                var totalCompleted = await assignedFaultsQuery
                    .CountAsync(f => f.Status == FaultStatus.Resolved || f.Status == FaultStatus.Scrapped);

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
                var viewModel = new FaultTechnicianDashboardViewModel
                {
                    TotalAssignedFaults = totalLoggedFaults,
                    TotalPendingFaults = totalPending,
                    TotalCompletedFaults = totalCompleted,
                    FaultsOverTime = faultsOverTime
                };

                return View(viewModel);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Error loading dashboard";
                return RedirectToAction("Index");
            }
        }

        // View Assigned Faults
        [HttpGet]
        public async Task<IActionResult> Index(string search, int pageIndex = 1)
        {
            try
            {
                const int pageSize = 10;

                // Get current logged-in technician
                var currentUser = await _userManager.GetUserAsync(User);

                // Base query: faults assigned to this technician
                var query = _context.FridgeFaults
                    .Include(f => f.FridgeUnit)
                    .ThenInclude(fu => fu.FridgeModel)
                    .Include(f => f.ReportedBy)
                    .Where(f => f.AssignedTechnicianId == currentUser.Id)
                    .AsQueryable();

                // Apply search if provided (search by fridge serial or description)
                if (!string.IsNullOrEmpty(search))
                {
                    search = search.ToLower();
                    query = query.Where(f =>
                        f.FridgeUnit.SerialNumber.ToLower().Contains(search) ||
                        f.FridgeUnit.FridgeModel.Name.ToLower().Contains(search) ||
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
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Error loading assigned faults";
                return RedirectToAction("Dashboard");
            }
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
                    .Include(f => f.FridgeUnit)
                    .ThenInclude(fu => fu.FridgeModel)
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
                    Message = $"Fridge Fault (Serial: {fault.FridgeUnit.SerialNumber}) status changed from '{oldStatus}' to '{status}' by technician '{currentUser.FullName}'.",
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
            try
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
                    .Include(f => f.FridgeUnit)
                    .ThenInclude(fu => fu.FridgeModel)
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
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Error loading fault details";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessRepair(ProcessRepairViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Add model errors to TempData for display on the Details page
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                TempData["ErrorMessage"] = $"Validation failed: {string.Join(", ", errors)}";
                return RedirectToAction(nameof(Details), new { id = model.FaultId });
            }

            try
            {
                var currentUser = await _userManager.GetUserAsync(User);

                // 1. Fetch the fault and related fridge unit
                var fault = await _context.FridgeFaults
                    .Include(f => f.FridgeUnit)
                    .FirstOrDefaultAsync(f => f.Id == model.FaultId && f.AssignedTechnicianId == currentUser.Id);

                if (fault == null)
                {
                    TempData["ErrorMessage"] = "Fault not found, or you are not assigned to this fault.";
                    return RedirectToAction(nameof(Index));
                }

                // 2. Process fault / Repair fridge unit (Update Fault Record)
                fault.Status = FaultStatus.Resolved;
                fault.ResolutionDate = DateTime.Now;
                fault.RepairDescription = model.RepairDescription;
                fault.PartsUsed = model.PartsUsed;

                // 3. Update fridge unit condition and status
                fault.FridgeUnit.Condition = model.NewFridgeCondition;

                // Update fridge unit status based on condition
                if (model.NewFridgeCondition == FridgeCondition.Operational)
                {
                    fault.FridgeUnit.Status = FridgeStatus.InWarehouse;
                }
                else if (model.NewFridgeCondition == FridgeCondition.RequireMaintenance)
                {
                    fault.FridgeUnit.Status = FridgeStatus.InRepair;
                }
                else if (model.NewFridgeCondition == FridgeCondition.Scrapped)
                {
                    fault.FridgeUnit.Status = FridgeStatus.Retired;
                }

                // Save changes and create an Audit Trail entry
                _context.Update(fault);
                _context.Update(fault.FridgeUnit);

                // Add Audit Trail for fault resolution
                var audit = new AuditTrail
                {
                    EntityName = "FridgeFault",
                    EntityId = fault.Id,
                    ChangeType = "Resolved & Repaired",
                    ChangeDetails = $"Fault resolved. Fridge unit condition updated to {model.NewFridgeCondition} with parts: {model.PartsUsed ?? "None"}.",
                    ChangeDate = DateTime.Now,
                    ChangedByUserId = currentUser.Id
                };
                _context.AuditTrails.Add(audit);

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Fault {model.FaultId} successfully resolved and fridge unit condition updated to {model.NewFridgeCondition}.";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while saving the repair details. Please check the database connection and try again.";
                return RedirectToAction(nameof(Details), new { id = model.FaultId });
            }

            return RedirectToAction(nameof(Details), new { id = model.FaultId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, FaultStatus newStatus)
        {
            try
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
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    TempData["ErrorMessage"] = "User not found. Please login again.";
                    return RedirectToAction(nameof(Index));
                }

                // Include FridgeUnit to allow condition update
                var fault = await _context.FridgeFaults
                    .Include(f => f.FridgeUnit)
                    .FirstOrDefaultAsync(f => f.Id == faultId && f.AssignedTechnicianId == currentUser.Id);

                if (fault == null || fault.FridgeUnit == null)
                {
                    TempData["ErrorMessage"] = "Fault not found or you are not assigned to this fault.";
                    return RedirectToAction(nameof(Index));
                }

                // 1. Update the Fault Status
                fault.Status = FaultStatus.Resolved;
                fault.ResolutionDate = DateTime.Now;
                fault.RepairDescription = resolutionNotes;

                // 2. Update the Fridge Unit Condition based on technician input
                if (requiresMaintenance)
                {
                    // Fridge needs maintenance
                    fault.FridgeUnit.Condition = FridgeCondition.RequireMaintenance;
                    fault.FridgeUnit.Status = FridgeStatus.InRepair;
                    TempData["SuccessMessage"] = $"Fault {faultId} resolved. Fridge unit flagged for Maintenance Oversight.";
                }
                else
                {
                    // Fault resolved, fridge unit is operational
                    fault.FridgeUnit.Condition = FridgeCondition.Operational;
                    fault.FridgeUnit.Status = FridgeStatus.InWarehouse;
                    TempData["SuccessMessage"] = $"Fault {faultId} resolved. Fridge unit marked as operational.";
                }

                // 3. Create Audit Trail Entry
                var audit = new AuditTrail
                {
                    EntityName = "FridgeFault",
                    EntityId = fault.Id,
                    ChangeType = "Fault Resolution",
                    ChangeDetails = $"Fault Resolved. Notes: {resolutionNotes}. Fridge Unit Condition set to {fault.FridgeUnit.Condition}.",
                    ChangeDate = DateTime.Now,
                    ChangedByUserId = currentUser.Id
                };

                _context.Update(fault);
                _context.Update(fault.FridgeUnit);
                _context.AuditTrails.Add(audit);
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred during fault resolution. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetFaultDetails(int id)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var fault = await _context.FridgeFaults
                    .Include(f => f.FridgeUnit)
                    .ThenInclude(fu => fu.FridgeModel)
                    .Include(f => f.ReportedBy)
                    .FirstOrDefaultAsync(f => f.Id == id && f.AssignedTechnicianId == currentUser.Id);

                if (fault == null)
                {
                    return Json(new { success = false, message = "Fault not found" });
                }

                var faultDetails = new
                {
                    success = true,
                    faultId = fault.Id,
                    fridgeModel = fault.FridgeUnit.FridgeModel.Name,
                    serialNumber = fault.FridgeUnit.SerialNumber,
                    faultDescription = fault.FaultDescription,
                    dateReported = fault.DateReported.ToString("yyyy-MM-dd"),
                    status = fault.Status.ToString(),
                    reportedBy = fault.ReportedBy.FullName
                };

                return Json(faultDetails);
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Error loading fault details" });
            }
        }
    }
}