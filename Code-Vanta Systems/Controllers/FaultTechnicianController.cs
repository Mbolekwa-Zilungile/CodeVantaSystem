using Code_Vanta_Systems.Data;
using Code_Vanta_Systems.Models;
using Code_Vanta_Systems.Models.Enums;
using Code_Vanta_Systems.ViewModels.Customer;
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
    }
}
