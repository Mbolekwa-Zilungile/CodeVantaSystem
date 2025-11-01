using Code_Vanta_Systems.Data;
using Code_Vanta_Systems.Models;
using Code_Vanta_Systems.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Code_Vanta_Systems.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminFaultsController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public AdminFaultsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }

        // Get All Logged Faults
        [HttpGet]
        public async Task<IActionResult> Index(int? pageIndex, string? search, FaultStatus? status)
        {
            try
            {
                // Base query: include related entities with new data model
                var query = _context.FridgeFaults
                    .Include(f => f.FridgeUnit)
                    .ThenInclude(fu => fu.FridgeModel)
                    .Include(f => f.ReportedBy)
                    .Include(f => f.AssignedTechnician)
                    .AsQueryable();

                // Apply search if provided
                if (!string.IsNullOrEmpty(search))
                {
                    search = search.ToLower();
                    query = query.Where(f =>
                        f.FridgeUnit.SerialNumber.ToLower().Contains(search) ||
                        f.FridgeUnit.FridgeModel.Name.ToLower().Contains(search) ||
                        f.FaultDescription.ToLower().Contains(search) ||
                        f.ReportedBy.FullName.ToLower().Contains(search) ||
                        (f.AssignedTechnician != null && f.AssignedTechnician.FullName.ToLower().Contains(search))
                    );
                }

                // Apply status filter if provided
                if (status.HasValue)
                {
                    query = query.Where(f => f.Status == status.Value);
                }

                // Order by most recent first
                query = query.OrderByDescending(f => f.DateReported);

                // Pagination
                pageIndex ??= 1;
                int pageSize = 10;
                int totalRecords = await query.CountAsync();
                int totalPages = (int)Math.Ceiling((decimal)totalRecords / pageSize);

                var faults = await query
                    .Skip((pageIndex.Value - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Get all Fault Technicians (using UserManager to get users in role)
                var technicianUsers = await _userManager.GetUsersInRoleAsync("Fault Technician");
                var technicians = technicianUsers.Select(u => new
                {
                    TechnicianId = u.Id,
                    FullName = u.FullName,
                    Email = u.Email
                }).ToList();

                ViewData["Technicians"] = technicians;
                ViewData["currentPage"] = pageIndex.Value;
                ViewData["totalPages"] = totalPages;
                ViewData["search"] = search ?? "";
                ViewData["selectedStatus"] = status?.ToString();
                ViewData["FaultStatuses"] = Enum.GetValues(typeof(FaultStatus))
                    .Cast<FaultStatus>()
                    .Select(s => new { Value = (int)s, Text = s.ToString() })
                    .ToList();

                return View(faults);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Error loading fridge faults";
                return RedirectToAction("Index");
            }
        }

        // Assign Technician
        [HttpPost]
        public async Task<IActionResult> AssignTechnician(int faultId, string technicianId)
        {
            try
            {
                // 1️ Find the fault and include FridgeUnit with FridgeModel
                var fault = await _context.FridgeFaults
                    .Include(f => f.FridgeUnit)
                    .ThenInclude(fu => fu.FridgeModel)
                    .Include(f => f.ReportedBy)
                    .Include(f => f.AssignedTechnician)
                    .FirstOrDefaultAsync(f => f.Id == faultId);

                if (fault == null)
                {
                    TempData["ErrorMessage"] = "The selected fault could not be found.";
                    return RedirectToAction(nameof(Index));
                }

                // 2️ Verify the technician exists and is in the Fault Technician role
                var technicianUser = await _userManager.FindByIdAsync(technicianId);
                if (technicianUser == null)
                {
                    TempData["ErrorMessage"] = "Technician not found.";
                    return RedirectToAction(nameof(Index));
                }

                var isFaultTechnician = await _userManager.IsInRoleAsync(technicianUser, "Fault Technician");
                if (!isFaultTechnician)
                {
                    TempData["ErrorMessage"] = "Selected user is not a Fault Technician.";
                    return RedirectToAction(nameof(Index));
                }

                // 3️ Assign the technician
                var previousTechnicianId = fault.AssignedTechnicianId;
                fault.AssignedTechnicianId = technicianUser.Id;
                fault.Status = FaultStatus.InProgress; // Update status when assigned

                _context.FridgeFaults.Update(fault);
                await _context.SaveChangesAsync();

                // 4️ Get the current logged-in admin
                var currentUser = await _userManager.GetUserAsync(User);

                // 5️ Create AuditTrail entry
                var audit = new AuditTrail
                {
                    EntityName = "FridgeFault",
                    EntityId = fault.Id,
                    ChangedByUserId = currentUser?.Id ?? "System",
                    ChangeType = "Update",
                    ChangeDate = DateTime.Now,
                    ChangeDetails = $"Technician '{technicianUser.FullName}' assigned to Fridge Unit (Serial: {fault.FridgeUnit.SerialNumber}, Model: {fault.FridgeUnit.FridgeModel.Name}) by '{currentUser?.FullName ?? "System"}'."
                };

                _context.AuditTrails.Add(audit);
                await _context.SaveChangesAsync();

                // 6️ Create Notification for assigned technician
                var notification = new Notification
                {
                    RecipientUserId = technicianUser.Id,
                    Title = "New Fridge Fault Assigned",
                    Message = $"You have been assigned to resolve Fridge Fault (Serial: {fault.FridgeUnit.SerialNumber}, Model: {fault.FridgeUnit.FridgeModel.Name}). Please review the fault details.",
                    CreatedAt = DateTime.Now,
                    IsRead = false,
                    TargetController = "FaultTechnician",
                    TargetAction = "Details",
                    TargetId = fault.Id
                };

                _context.Notifications.Add(notification);

                // 7️ If there was a previous technician, notify them they've been unassigned
                if (!string.IsNullOrEmpty(previousTechnicianId) && previousTechnicianId != technicianUser.Id)
                {
                    var unassignNotification = new Notification
                    {
                        RecipientUserId = previousTechnicianId,
                        Title = "Fault Assignment Removed",
                        Message = $"You have been unassigned from Fridge Fault (Serial: {fault.FridgeUnit.SerialNumber}).",
                        CreatedAt = DateTime.Now,
                        IsRead = false,
                        TargetController = "FaultTechnician",
                        TargetAction = "Index"
                    };
                    _context.Notifications.Add(unassignNotification);
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Technician '{technicianUser.FullName}' has been successfully assigned to the fault for fridge unit '{fault.FridgeUnit.FridgeModel.Name} - {fault.FridgeUnit.SerialNumber}'.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while assigning the technician: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // Fault Details
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var fault = await _context.FridgeFaults
                    .Include(f => f.FridgeUnit)
                    .ThenInclude(fu => fu.FridgeModel)
                    .Include(f => f.ReportedBy)
                    .Include(f => f.AssignedTechnician)
                    .FirstOrDefaultAsync(f => f.Id == id);

                if (fault == null)
                {
                    TempData["ErrorMessage"] = "Fault not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Get audit trail for this fault
                var audits = await _context.AuditTrails
                    .Where(a => a.EntityName == "FridgeFault" && a.EntityId == id)
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

        // Update Fault Status
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, FaultStatus status)
        {
            try
            {
                var fault = await _context.FridgeFaults
                    .Include(f => f.FridgeUnit)
                    .ThenInclude(fu => fu.FridgeModel)
                    .FirstOrDefaultAsync(f => f.Id == id);

                if (fault == null)
                {
                    TempData["ErrorMessage"] = "Fault not found.";
                    return RedirectToAction(nameof(Index));
                }

                var oldStatus = fault.Status;
                fault.Status = status;

                if (status == FaultStatus.Resolved || status == FaultStatus.Scrapped)
                {
                    fault.ResolutionDate = DateTime.Now;
                }

                _context.FridgeFaults.Update(fault);

                // Create audit trail
                var currentUser = await _userManager.GetUserAsync(User);
                var audit = new AuditTrail
                {
                    EntityName = "FridgeFault",
                    EntityId = fault.Id,
                    ChangedByUserId = currentUser?.Id ?? "System",
                    ChangeType = "Status Update",
                    ChangeDate = DateTime.Now,
                    ChangeDetails = $"Status changed from '{oldStatus}' to '{status}' by Admin '{currentUser?.FullName ?? "System"}'."
                };
                _context.AuditTrails.Add(audit);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Fault status updated to '{status}' successfully.";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Error updating fault status";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // Unassign Technician
        [HttpPost]
        public async Task<IActionResult> UnassignTechnician(int id)
        {
            try
            {
                var fault = await _context.FridgeFaults
                    .Include(f => f.FridgeUnit)
                    .ThenInclude(fu => fu.FridgeModel)
                    .Include(f => f.AssignedTechnician)
                    .FirstOrDefaultAsync(f => f.Id == id);

                if (fault == null)
                {
                    TempData["ErrorMessage"] = "Fault not found.";
                    return RedirectToAction(nameof(Index));
                }

                if (fault.AssignedTechnicianId == null)
                {
                    TempData["InfoMessage"] = "No technician was assigned to this fault.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var previousTechnicianId = fault.AssignedTechnicianId;
                var previousTechnicianName = fault.AssignedTechnician?.FullName;

                fault.AssignedTechnicianId = null;
                fault.Status = FaultStatus.Open; // Reset to open when unassigned

                _context.FridgeFaults.Update(fault);

                // Create audit trail
                var currentUser = await _userManager.GetUserAsync(User);
                var audit = new AuditTrail
                {
                    EntityName = "FridgeFault",
                    EntityId = fault.Id,
                    ChangedByUserId = currentUser?.Id ?? "System",
                    ChangeType = "Unassign Technician",
                    ChangeDate = DateTime.Now,
                    ChangeDetails = $"Technician '{previousTechnicianName}' unassigned from fault by Admin '{currentUser?.FullName ?? "System"}'."
                };
                _context.AuditTrails.Add(audit);

                // Notify the unassigned technician
                if (!string.IsNullOrEmpty(previousTechnicianId))
                {
                    var notification = new Notification
                    {
                        RecipientUserId = previousTechnicianId,
                        Title = "Fault Assignment Removed",
                        Message = $"You have been unassigned from Fridge Fault (Serial: {fault.FridgeUnit.SerialNumber}).",
                        CreatedAt = DateTime.Now,
                        IsRead = false,
                        TargetController = "FaultTechnician",
                        TargetAction = "Index"
                    };
                    _context.Notifications.Add(notification);
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Technician '{previousTechnicianName}' has been unassigned from the fault.";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Error unassigning technician";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // Get Fault Statistics for Dashboard
        [HttpGet]
        public async Task<IActionResult> GetFaultStatistics()
        {
            try
            {
                var totalFaults = await _context.FridgeFaults.CountAsync();
                var openFaults = await _context.FridgeFaults.CountAsync(f => f.Status == FaultStatus.Open);
                var inProgressFaults = await _context.FridgeFaults.CountAsync(f => f.Status == FaultStatus.InProgress);
                var resolvedFaults = await _context.FridgeFaults.CountAsync(f => f.Status == FaultStatus.Resolved);
                var scrappedFaults = await _context.FridgeFaults.CountAsync(f => f.Status == FaultStatus.Scrapped);

                var unassignedFaults = await _context.FridgeFaults.CountAsync(f => f.AssignedTechnicianId == null);

                var statistics = new
                {
                    TotalFaults = totalFaults,
                    OpenFaults = openFaults,
                    InProgressFaults = inProgressFaults,
                    ResolvedFaults = resolvedFaults,
                    ScrappedFaults = scrappedFaults,
                    UnassignedFaults = unassignedFaults
                };

                return Json(statistics);
            }
            catch (Exception)
            {
                return Json(new { error = "Error loading statistics" });
            }
        }
    }
}