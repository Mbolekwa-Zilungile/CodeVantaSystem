using Code_Vanta_Systems.Data;
using Code_Vanta_Systems.Models;
using Code_Vanta_Systems.Models.Enums;
using Code_Vanta_Systems.ViewModels.Maintenance_Schedule;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Code_Vanta_Systems.Controllers
{    
    public class MaintenanceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MaintenanceController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Maintenance List with Pagination
        [Authorize(Roles = "Fault Technician")]
        [HttpGet]
        public async Task<IActionResult> Index(int? pageIndex, string? search)
        {
            var query = _context.FridgeMaintenances
                .Include(m => m.Fridge)
                .Include(c => c.Customer)
                .Include(m => m.Technician)
                .OrderByDescending(m => m.CreatedDate)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                query = query.Where(m =>
                    m.Fridge.Model.ToLower().Contains(search) ||
                    m.Technician.FullName.ToLower().Contains(search) ||
                    m.MaintenanceDescription.ToLower().Contains(search)
                );
            }

            // Pagination setup
            pageIndex ??= 1;
            int pageSize = 10;
            int totalRecords = await query.CountAsync();
            int totalPages = (int)Math.Ceiling((decimal)totalRecords / pageSize);

            var maintenances = await query
                .Skip((pageIndex.Value - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new MaintenanceViewModel
                {
                    Id = m.Id,
                    FridgeId = m.FridgeId,
                    FridgeModel = m.Fridge.Model,
                    CustomerId = m.CustomerId,
                    BusinessName = m.Customer.BusinessName,
                    TechnicianId = m.TechnicianId,
                    TechnicianName = m.Technician.FullName,
                    MaintenanceDate = m.MaintenanceDate,
                    ActualCompletionDate = m.ActualCompletionDate,
                    MaintenanceDescription = m.MaintenanceDescription,
                    Status = m.Status
                })
                .ToListAsync();

            ViewData["currentPage"] = pageIndex.Value;
            ViewData["totalPages"] = totalPages;
            ViewData["search"] = search ?? "";

            ViewBag.Fridges = await _context.Fridges
                .Where(f => !f.IsDeleted)
                .ToListAsync();

            return View(maintenances);
        }

        // GET: Create
        [Authorize(Roles = "Fault Technician")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Fridges = await _context.Fridges
                .Where(f => !f.IsDeleted)
                .Select(f => new { f.Id, f.Model, f.SerialNumber })
                .ToListAsync();

            return View();
        }

        // POST: Create Maintenance Record
        [Authorize(Roles = "Fault Technician")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateMaintenanceViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please correct the errors in the form.";
                ViewBag.Fridges = await _context.Fridges
                    .Where(f => !f.IsDeleted)
                    .Select(f => new { f.Id, f.Model, f.SerialNumber })
                    .ToListAsync();
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Technician not found.";
                return RedirectToAction(nameof(Index));
            }

            // ✅ Load the fridge allocation including Customer and Fridge
            var allocation = await _context.FridgeAllocations
                .Include(a => a.Customer)
                .Include(a => a.Fridge)
                .FirstOrDefaultAsync(a => a.FridgeId == model.FridgeId && a.DeallocationDate == null);

            if (allocation == null || allocation.Customer == null)
            {
                TempData["ErrorMessage"] = "No active customer found for this fridge.";
                return RedirectToAction(nameof(Index));
            }

            // ✅ Create the maintenance record
            var maintenance = new FridgeMaintenance
            {
                FridgeId = model.FridgeId,
                CustomerId = allocation.Customer.Id,
                TechnicianId = user.Id,
                CreatedDate = DateTime.UtcNow,
                MaintenanceDate = model.MaintenanceDate,
                MaintenanceDescription = model.MaintenanceDescription,
                Status = model.Status
            };

            _context.FridgeMaintenances.Add(maintenance);

            // ✅ Audit trail
            _context.AuditTrails.Add(new AuditTrail
            {
                EntityName = "FridgeMaintenance",
                EntityId = maintenance.Id,
                ChangedByUserId = user.Id,
                ChangeType = "Create",
                ChangeDate = DateTime.Now,
                ChangeDetails = $"Technician '{user.FullName}' scheduled maintenance for fridge ID {model.FridgeId}."
            });

            // ✅ Notify the customer if they have a linked User account
            if (!string.IsNullOrEmpty(allocation.Customer.UserId))
            {
                _context.Notifications.Add(new Notification
                {
                    RecipientUserId = allocation.Customer.UserId,
                    Title = "Upcoming Fridge Maintenance",
                    Message = $"Maintenance has been scheduled for your fridge (Model: {allocation.Fridge?.Model ?? "Unknown"}, Serial: {allocation.Fridge?.SerialNumber ?? "Unknown"}) on {model.MaintenanceDate:dd MMM yyyy}.",
                    CreatedAt = DateTime.Now,
                    IsRead = false,
                    TargetController = "Maintenance",
                    TargetAction = "Index"
                });
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Maintenance scheduled successfully and the customer has been notified.";
            return RedirectToAction(nameof(Index));
        }


        // POST: Edit Maintenance (used by modal)
        [Authorize(Roles = "Fault Technician")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditMaintenanceViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Invalid data.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.GetUserAsync(User);
            var maintenance = await _context.FridgeMaintenances
                .Include(m => m.Fridge)
                .FirstOrDefaultAsync(m => m.Id == model.Id);

            if (maintenance == null)
            {
                TempData["ErrorMessage"] = "Maintenance record not found.";
                return RedirectToAction(nameof(Index));
            }

            // Track changes
            var oldStatus = maintenance.Status;

            // Update fields
            maintenance.MaintenanceDate = model.MaintenanceDate;
            maintenance.MaintenanceDescription = model.MaintenanceDescription;
            maintenance.Status = model.Status;
            maintenance.ActualCompletionDate = model.ActualCompletionDate;

            await _context.SaveChangesAsync();

            // Audit Trail
            _context.AuditTrails.Add(new AuditTrail
            {
                EntityName = "FridgeMaintenance",
                EntityId = maintenance.Id,
                ChangedByUserId = user.Id,
                ChangeType = "Update",
                ChangeDate = DateTime.Now,
                ChangeDetails = $"Maintenance ID {maintenance.Id} updated by '{user.FullName}'. Status changed from {oldStatus} to {maintenance.Status}."
            });

            // Notify all customers with the same fridge
            var allocations = await _context.FridgeAllocations
                .Include(a => a.Customer)
                .Include(a => a.Fridge)
                .Where(a => a.FridgeId == maintenance.FridgeId && a.DeallocationDate == null)
                .ToListAsync();

            foreach (var allocation in allocations)
            {
                var customerUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == allocation.Customer.UserId);
                if (customerUser != null)
                {
                    string statusMessage = maintenance.Status switch
                    {
                        MaintenanceStatus.Completed => $"Maintenance for your fridge (Model: {allocation.Fridge.Model}) has been completed on {maintenance.ActualCompletionDate?.ToString("dd MMM yyyy")}.",
                        MaintenanceStatus.Scheduled => $"Maintenance for your fridge (Model: {allocation.Fridge.Model}) is currently in progress.",
                        MaintenanceStatus.Cancelled => $"Maintenance for your fridge (Model: {allocation.Fridge.Model}) has been cancelled.",
                        MaintenanceStatus.Overdue => $"Maintenance for your fridge (Model: {allocation.Fridge.Model}) has been rescheduled to {maintenance.MaintenanceDate:dd MMM yyyy}.",
                        _ => $"The status of your fridge maintenance (Model: {allocation.Fridge.Model}) has been updated to {maintenance.Status}."
                    };

                    _context.Notifications.Add(new Notification
                    {
                        RecipientUserId = customerUser.Id,
                        Title = "Maintenance Update",
                        Message = statusMessage,
                        CreatedAt = DateTime.Now,
                        IsRead = false,
                        TargetController = "Maintenance",
                        TargetAction = "Index"
                    });
                }
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Maintenance record updated and customers notified successfully!";
            return RedirectToAction(nameof(Index));
        }

        // POST: Delete Maintenance (used by modal)
        [Authorize(Roles = "Fault Technician")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var maintenance = await _context.FridgeMaintenances.FindAsync(id);

            if (maintenance == null)
            {
                TempData["ErrorMessage"] = "Maintenance record not found.";
                return RedirectToAction(nameof(Index));
            }

            _context.FridgeMaintenances.Remove(maintenance);

            _context.AuditTrails.Add(new AuditTrail
            {
                EntityName = "FridgeMaintenance",
                EntityId = maintenance.Id,
                ChangedByUserId = user.Id,
                ChangeType = "Delete",
                ChangeDate = DateTime.Now,
                ChangeDetails = $"Maintenance ID {maintenance.Id} deleted by '{user.FullName}'."
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Maintenance record deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Customer")]
        [HttpGet]
        public async Task<IActionResult> CustomerSchedules(int? pageIndex, string? search)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Customer not found.";
                return RedirectToAction("Index", "Home");
            }

            // Get all fridge IDs allocated to this customer
            var customerFridgeIds = await _context.FridgeAllocations
                .Where(a => a.Customer.UserId == user.Id && a.DeallocationDate == null)
                .Select(a => a.FridgeId)
                .ToListAsync();

            var query = _context.FridgeMaintenances
                .Include(m => m.Fridge)
                .Include(m => m.Technician)
                .Where(m => customerFridgeIds.Contains(m.FridgeId))
                .OrderByDescending(m => m.CreatedDate)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                query = query.Where(m =>
                    m.Fridge.Model.ToLower().Contains(search) ||
                    m.Technician.FullName.ToLower().Contains(search) ||
                    m.MaintenanceDescription.ToLower().Contains(search)
                );
            }

            // Pagination setup
            pageIndex ??= 1;
            int pageSize = 10;
            int totalRecords = await query.CountAsync();
            int totalPages = (int)Math.Ceiling((decimal)totalRecords / pageSize);

            var schedules = await query
                .Skip((pageIndex.Value - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new MaintenanceViewModel
                {
                    Id = m.Id,
                    FridgeId = m.FridgeId,
                    FridgeModel = m.Fridge.Model,
                    TechnicianId = m.TechnicianId,
                    TechnicianName = m.Technician.FullName,
                    MaintenanceDate = m.MaintenanceDate,
                    ActualCompletionDate = m.ActualCompletionDate,
                    MaintenanceDescription = m.MaintenanceDescription,
                    Status = m.Status
                })
                .ToListAsync();

            ViewData["currentPage"] = pageIndex.Value;
            ViewData["totalPages"] = totalPages;
            ViewData["search"] = search ?? "";

            return View(schedules);
        }
    }
}
