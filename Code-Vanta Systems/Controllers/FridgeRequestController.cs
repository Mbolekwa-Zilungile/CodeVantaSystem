using Code_Vanta_Systems.Data;
using Code_Vanta_Systems.Models;
using Code_Vanta_Systems.Models.Enums;
using Code_Vanta_Systems.ViewModels.Log_Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Code_Vanta_Systems.Controllers
{
    [Authorize(Roles = "Customer")]
    public class FridgeRequestController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public FridgeRequestController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // List all fridge requests
        [HttpGet]
        public async Task<IActionResult> Index(int? pageIndex, string? search)
        {
            try
            {
                var query = _context.FridgeRequests
                    .Include(r => r.Customer)
                    .Include(r => r.Location)
                    .Include(r => r.RequestedBy)
                    .Where(r => r.Status != RequestStatus.Deleted)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(search))
                {
                    search = search.ToLower();
                    query = query.Where(r =>
                        r.Customer.ContactName.ToLower().Contains(search) ||
                        r.Location.Description.ToLower().Contains(search) ||
                        r.RequestedBy.FullName.ToLower().Contains(search) ||
                        r.Notes.ToLower().Contains(search)
                    );
                }

                query = query.OrderByDescending(r => r.RequestedAt);

                // Pagination
                pageIndex ??= 1;
                int pageSize = 10;
                int totalRecords = await query.CountAsync();
                int totalPages = (int)Math.Ceiling((decimal)totalRecords / pageSize);

                var requests = await query
                    .Skip((pageIndex.Value - 1) * pageSize)
                    .Take(pageSize)
                    .Select(r => new FridgeRequestViewModel
                    {
                        Id = r.Id,
                        CustomerId = r.CustomerId,
                        CustomerName = r.Customer.ContactName,
                        LocationId = r.LocationId,
                        LocationName = r.Location.Description,
                        RequestedByUserId = r.RequestedByUserId,
                        RequestedByFullName = r.RequestedBy.FullName,
                        RequestedAt = r.RequestedAt,
                        Notes = r.Notes,
                        Status = r.Status
                    })
                    .ToListAsync();

                ViewBag.Locations = await _context.Locations.ToListAsync();
                ViewBag.Fridges = await _context.Fridges
                    .Where(f => !f.IsDeleted)
                    .ToListAsync();

                ViewData["currentPage"] = pageIndex.Value;
                ViewData["totalPages"] = totalPages;
                ViewData["search"] = search ?? "";

                return View(requests);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Error loading fridge requests";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            try
            {
                // Load all locations
                ViewBag.Locations = await _context.Locations.ToListAsync();

                // Load all available fridges
                ViewBag.Fridges = await _context.Fridges
                    .Where(f => !f.IsDeleted)
                    .Select(f => new { f.Id, f.Model, f.SerialNumber })
                    .ToListAsync();

                return View();
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Error loading create form";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateFridgeRequestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please correct the errors in the form.";
                // Reload locations and fridges for the dropdowns
                ViewBag.Locations = await _context.Locations.ToListAsync();


                ViewBag.Fridges = await _context.Fridges
                    .Where(f => !f.IsDeleted)
                    .Select(f => new { f.Id, f.Model, f.SerialNumber })
                    .ToListAsync();

                return View(model);
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);
                // Get the customer's ID linked to the logged-in user
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == user.Id);
                if (customer == null)
                {
                    TempData["ErrorMessage"] = "Customer profile not found.";
                    return RedirectToAction(nameof(Create));
                }

                // Create the fridge request
                var request = new FridgeRequest
                {
                    CustomerId = customer.Id, // Current logged-in customer
                    RequestedByUserId = user.Id,
                    LocationId = model.LocationId,
                    RequestedAt = DateTime.UtcNow,
                    Notes = model.Notes,
                    Status = RequestStatus.Pending // Default status
                };

                _context.FridgeRequests.Add(request);
                await _context.SaveChangesAsync();

                // Audit trail
                var audit = new AuditTrail
                {
                    EntityName = "FridgeRequest",
                    EntityId = request.Id,
                    ChangedByUserId = user.Id,
                    ChangeType = "Create",
                    ChangeDate = DateTime.UtcNow,
                    ChangeDetails = $"Fridge request ID {request.Id} created by '{user.FullName}'."
                };
                _context.AuditTrails.Add(audit);

                // Notify all admins
                var admins = await _userManager.GetUsersInRoleAsync("Admin");
                foreach (var admin in admins)
                {
                    _context.Notifications.Add(new Notification
                    {
                        RecipientUserId = admin.Id,
                        Title = "New Fridge Request",
                        Message = $"Customer '{user.FullName}' submitted a new fridge request (ID {request.Id}).",
                        CreatedAt = DateTime.UtcNow,
                        IsRead = false,
                        TargetController = "FridgeRequests",
                        TargetAction = "Index"
                    });
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Fridge request submitted successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Error creating fridge request.";
                return RedirectToAction(nameof(Index));
            }
        }

        // Update the logged fridge request
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CustomerEditFridgeRequestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please correct the errors in the form.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);
                var request = await _context.FridgeRequests.FindAsync(model.Id);

                if (request == null)
                {
                    TempData["ErrorMessage"] = "Fridge request not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Update only allowed fields for customer
                request.LocationId = model.LocationId;
                request.Id = model.Id;
                request.Notes = model.Notes;

                await _context.SaveChangesAsync();

                // Audit trail
                var audit = new AuditTrail
                {
                    EntityName = "FridgeRequest",
                    EntityId = request.Id,
                    ChangedByUserId = user.Id,
                    ChangeType = "Update",
                    ChangeDate = DateTime.Now,
                    ChangeDetails = $"Customer '{user.FullName}' updated Fridge Request ID {request.Id}."
                };
                _context.AuditTrails.Add(audit);

                // Notify all admins
                var admins = await _userManager.GetUsersInRoleAsync("Admin");
                foreach (var admin in admins)
                {
                    _context.Notifications.Add(new Notification
                    {
                        RecipientUserId = admin.Id,
                        Title = "Fridge Request Updated",
                        Message = $"Fridge Request ID {request.Id} was updated by Customer '{user.FullName}'.",
                        CreatedAt = DateTime.UtcNow,
                        IsRead = false,
                        TargetController = "AdminRequests",
                        TargetAction = "Index"
                    });
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Fridge request updated successfully!";
            }
            catch
            {
                TempData["ErrorMessage"] = "Error updating fridge request.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var request = await _context.FridgeRequests.FindAsync(id);

                if (request == null)
                {
                    TempData["ErrorMessage"] = "Fridge request not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Hard delete
                _context.FridgeRequests.Remove(request);

                // Optional: Audit trail
                var audit = new AuditTrail
                {
                    EntityName = "FridgeRequest",
                    EntityId = request.Id,
                    ChangedByUserId = user.Id,
                    ChangeType = "Delete",
                    ChangeDate = DateTime.Now,
                    ChangeDetails = $"Fridge request ID {request.Id} was deleted by Customer '{user.FullName}'."
                };
                _context.AuditTrails.Add(audit);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Fridge request deleted successfully!";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Error deleting fridge request.";
            }

            return RedirectToAction(nameof(Index));
        }



    }
}
