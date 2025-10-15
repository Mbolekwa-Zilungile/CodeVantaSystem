using Code_Vanta_Systems.Data;
using Code_Vanta_Systems.Models;
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
        public async Task<IActionResult> Index(int? pageIndex, string? search)
        {
            try
            {
                // Base query: include related entities
                var query = _context.FridgeFaults
                    .Include(f => f.Fridge)
                    .Include(f => f.ReportedBy)
                    .Include(f => f.AssignedTechnician)
                    .AsQueryable();

                // Apply search if provided
                if (!string.IsNullOrEmpty(search))
                {
                    search = search.ToLower();
                    query = query.Where(f =>
                        f.Fridge.SerialNumber.ToLower().Contains(search) ||
                        f.FaultDescription.ToLower().Contains(search) ||
                        f.ReportedBy.FullName.ToLower().Contains(search) ||
                        (f.AssignedTechnician != null && f.AssignedTechnician.FullName.ToLower().Contains(search))
                    );
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

                // Get all Fault Technicians
                var technicians = await _context.Employees
                    .Include(e => e.User)
                    .Where(e => e.Position == "Fault Technician" && e.IsActive)
                    .Select(e => new
                    {
                        TechnicianId = e.Id,
                        e.ApplicationUserId,
                        FullName = e.User.FullName
                    })
                    .ToListAsync();

                ViewData["Technicians"] = technicians;
                ViewData["currentPage"] = pageIndex.Value;
                ViewData["totalPages"] = totalPages;
                ViewData["search"] = search ?? "";

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
        public async Task<IActionResult> AssignTechnician(int faultId, int technicianId)
        {
            try
            {
                // 1️ Find the fault and include Fridge
                var fault = await _context.FridgeFaults
                    .Include(f => f.Fridge)
                    .Include(f => f.ReportedBy)
                    .Include(f => f.AssignedTechnician)
                    .FirstOrDefaultAsync(f => f.Id == faultId);

                if (fault == null)
                {
                    TempData["ErrorMessage"] = "The selected fault could not be found.";
                    return RedirectToAction(nameof(Index));
                }

                // 2️ Find the technician
                var technician = await _context.Employees
                    .Include(e => e.User)
                    .FirstOrDefaultAsync(e => e.Id == technicianId && e.Position == "Fault Technician");

                if (technician == null)
                {
                    TempData["ErrorMessage"] = "Technician not found or not a valid Fault Technician.";
                    return RedirectToAction(nameof(Index));
                }

                // 3️ Assign the technician
                fault.AssignedTechnicianId = technician.ApplicationUserId;
                _context.FridgeFaults.Update(fault);
                await _context.SaveChangesAsync();

                // 4️  Get the current logged-in admin (Identity)
                var currentUser = await _userManager.GetUserAsync(User);

                // 5️  Create AuditTrail entry (safe null checks)
                var audit = new AuditTrail
                {
                    EntityName = "FridgeFault",
                    EntityId = fault.Id,
                    ChangedByUserId = currentUser?.Id ?? "System",
                    ChangeType = "Update",
                    ChangeDate = DateTime.Now,
                    ChangeDetails = $"Technician '{technician?.User?.FullName ?? "Unknown"}' assigned to Fridge (Serial: {fault?.Fridge?.SerialNumber ?? "N/A"}) by '{currentUser?.FullName ?? "System"}'."
                };

                _context.AuditTrails.Add(audit);
                await _context.SaveChangesAsync();

                // 6️ Create Notification for assigned technician
                var notification = new Notification
                {
                    RecipientUserId = technician.ApplicationUserId,
                    Title = "New Fridge Fault Assigned",
                    Message = $"You have been assigned to resolve Fridge Fault (Serial: {fault?.Fridge?.SerialNumber ?? "N/A"}). Please review the fault details.",
                    CreatedAt = DateTime.Now,
                    IsRead = false,
                    TargetController = "TechnicianFaults",
                    TargetAction = "Details"
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Technician '{technician.User.FullName}' has been successfully assigned to the fault for fridge with Serial Number '{fault.Fridge.SerialNumber}'.";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = $"An error occurred while assigning the technician.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
