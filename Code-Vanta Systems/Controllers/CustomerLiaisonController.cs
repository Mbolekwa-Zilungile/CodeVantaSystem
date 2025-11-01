using Code_Vanta_Systems.Data;
using Code_Vanta_Systems.Models;
using Code_Vanta_Systems.Models.Enums;
using Code_Vanta_Systems.Services;
using Code_Vanta_Systems.ViewModels.Allocation;
using Code_Vanta_Systems.ViewModels.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Code_Vanta_Systems.Controllers
{
    [Authorize(Roles = "Customer Liaison")]
    public class CustomerLiaisonController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;
        private readonly UserManager<ApplicationUser> _userManager;

        public CustomerLiaisonController(ApplicationDbContext context, EmailService emailService, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _emailService = emailService;
            _userManager = userManager;
        }

        #region Dashboard
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            ViewBag.Notifications = await _context.Notifications
                .Where(n => n.RecipientUserId == userId && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            // Dashboard statistics using new structure
            ViewBag.PendingCustomerCount = await _context.Customers.CountAsync(c => c.Status == CustomerStatus.Pending);
            ViewBag.ActiveCustomerCount = await _context.Customers.CountAsync(c => c.Status == CustomerStatus.Active);
            ViewBag.RejectedCustomerCount = await _context.Customers.CountAsync(c => c.Status == CustomerStatus.Rejected);
            ViewBag.PendingRentalRequests = await _context.RentalRequests.CountAsync(r => r.Status == "Pending");
            ViewBag.ActiveAllocations = await _context.FridgeAllocations.CountAsync(a => a.DeallocationDate == null);
            ViewBag.AvailableFridgeUnits = await _context.FridgeUnits.CountAsync(fu => fu.Status == FridgeStatus.InWarehouse);
            ViewBag.TotalFridgeModels = await _context.FridgeModels.CountAsync();

            return View();
        }
        #endregion

        #region Customers
        public async Task<IActionResult> PendingCustomers()
        {
            var pendingCustomers = await _context.Customers
                .Include(c => c.Location)
                .Where(c => c.Status == CustomerStatus.Pending)
                .ToListAsync();

            ViewBag.PendingCustomerCount = await _context.Customers.CountAsync(c => c.Status == CustomerStatus.Pending);
            ViewBag.ActiveCustomerCount = await _context.Customers.CountAsync(c => c.Status == CustomerStatus.Active);
            ViewBag.RejectedCustomerCount = await _context.Customers.CountAsync(c => c.Status == CustomerStatus.Rejected);

            return View(pendingCustomers);
        }

        public async Task<IActionResult> ActiveCustomers()
        {
            var activeCustomers = await _context.Customers
                .Include(c => c.Location)
                .Include(c => c.Allocations)
                .ThenInclude(a => a.FridgeUnit)
                .ThenInclude(fu => fu.FridgeModel)
                .Where(c => c.Status == CustomerStatus.Active)
                .ToListAsync();

            ViewBag.PendingCustomerCount = await _context.Customers.CountAsync(c => c.Status == CustomerStatus.Pending);
            ViewBag.ActiveCustomerCount = await _context.Customers.CountAsync(c => c.Status == CustomerStatus.Active);
            ViewBag.RejectedCustomerCount = await _context.Customers.CountAsync(c => c.Status == CustomerStatus.Rejected);

            return View(activeCustomers);
        }

        public async Task<IActionResult> RejectedCustomers()
        {
            var rejectedCustomers = await _context.Customers
                .Include(c => c.Location)
                .Where(c => c.Status == CustomerStatus.Rejected)
                .ToListAsync();

            ViewBag.PendingCustomerCount = await _context.Customers.CountAsync(c => c.Status == CustomerStatus.Pending);
            ViewBag.ActiveCustomerCount = await _context.Customers.CountAsync(c => c.Status == CustomerStatus.Active);
            ViewBag.RejectedCustomerCount = await _context.Customers.CountAsync(c => c.Status == CustomerStatus.Rejected);

            return View(rejectedCustomers);
        }

        public async Task<IActionResult> ViewCustomer(int id)
        {
            if (id == 0) return NotFound();

            var customer = await _context.Customers
                .Include(c => c.Location)
                .Include(c => c.Allocations)
                .ThenInclude(a => a.FridgeUnit)
                .ThenInclude(fu => fu.FridgeModel)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (customer == null) return NotFound();

            return View(customer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveCustomer(int id)
        {
            var customer = await _context.Customers
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == id && c.Status == CustomerStatus.Pending);

            if (customer == null) return NotFound("Customer not found or not pending.");

            customer.Status = CustomerStatus.Active;
            _context.Customers.Update(customer);
            await _context.SaveChangesAsync();

            if (customer.User != null)
            {
                try
                {
                    await _emailService.SendEmailAsync(
                        customer.User.Email,
                        "Account Approved - CodeVanta System",
                        $"Hello {customer.ContactName},<br><br>Your account '{customer.BusinessName}' has been approved. You can now log in to your dashboard.<br><br>Best regards,<br>CodeVanta Team"
                    );
                }
                catch { /* log if necessary */ }
            }

            TempData["Success"] = $"Customer '{customer.BusinessName}' has been approved and emailed.";
            return RedirectToAction("ActiveCustomers");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectCustomer(int id)
        {
            var customer = await _context.Customers
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == id && c.Status == CustomerStatus.Pending);

            if (customer == null) return NotFound("Customer not found or not pending.");

            customer.Status = CustomerStatus.Rejected;
            _context.Customers.Update(customer);
            await _context.SaveChangesAsync();

            if (customer.User != null)
            {
                try
                {
                    await _emailService.SendEmailAsync(
                        customer.User.Email,
                        "Account Rejected - CodeVanta Systems",
                        $"Hello {customer.ContactName},<br><br>Your account '{customer.BusinessName}' has been rejected.<br><br>Best regards,<br>CodeVanta Team"
                    );
                }
                catch { /* log if necessary */ }
            }

            TempData["Success"] = $"Customer '{customer.BusinessName}' has been rejected and emailed.";
            return RedirectToAction("RejectedCustomers");
        }
        #endregion

        #region Rental Requests
        public async Task<IActionResult> Requests()
        {
            var requests = await _context.RentalRequests
                .Include(r => r.FridgeModel)
                .Include(r => r.RequestedByUser)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(requests);
        }

        [HttpPost]
        public async Task<IActionResult> ApproveRental(int id)
        {
            try
            {
                var rental = await _context.RentalRequests
                    .Include(r => r.FridgeModel)
                    .Include(r => r.RequestedByUser)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (rental == null)
                    return Json(new { success = false, message = "Rental not found." });

                // Check available fridge units for this model
                var availableUnits = await _context.FridgeUnits
                    .CountAsync(fu => fu.FridgeModelId == rental.FridgeModelId &&
                                     fu.Status == FridgeStatus.InWarehouse);

                if (availableUnits < rental.Quantity)
                    return Json(new { success = false, message = $"Only {availableUnits} units available. Requested: {rental.Quantity}" });

                rental.Status = "Approved";
                rental.UpdatedAt = DateTime.UtcNow;

                var notification = new Notification
                {
                    Title = "Rental Approved",
                    RecipientUserId = rental.RequestedByUserId,
                    Message = $"Your rental request for {rental.FridgeModel.Name} ({rental.Quantity}) has been approved.",
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false,
                    TargetController = "Customer",
                    TargetAction = "MyRentalRequests"
                };
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Rental approved!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Failed to approve rental: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RejectRental(int id)
        {
            try
            {
                var rental = await _context.RentalRequests
                    .Include(r => r.FridgeModel)
                    .Include(r => r.RequestedByUser)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (rental == null)
                    return Json(new { success = false, message = "Rental not found." });

                rental.Status = "Rejected";
                rental.UpdatedAt = DateTime.UtcNow;

                var notification = new Notification
                {
                    RecipientUserId = rental.RequestedByUserId,
                    Title = "Rental Request Rejected",
                    Message = $"Your rental request for {rental.FridgeModel.Name} ({rental.Quantity}) has been rejected.",
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false,
                    TargetController = "Customer",
                    TargetAction = "MyRentalRequests"
                };
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Rental rejected!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Failed to reject rental: {ex.Message}" });
            }
        }
        #endregion

        #region Allocations
        [HttpGet]
        public async Task<IActionResult> CreateAllocationFromRental(int rentalId)
        {
            var rental = await _context.RentalRequests
                .Include(r => r.FridgeModel)
                .Include(r => r.RequestedByUser)
                .ThenInclude(u => u.CustomerProfile)
                .FirstOrDefaultAsync(r => r.Id == rentalId);

            if (rental == null) return NotFound("Rental request not found.");
            if (!rental.IsPaid || rental.Status != "Approved")
                return BadRequest("Rental must be approved and paid to allocate.");

            var model = new AllocationViewModel
            {
                FridgeModelId = rental.FridgeModelId,
                CustomerId = rental.RequestedByUser.CustomerProfile?.Id ?? 0,
                Quantity = rental.Quantity
            };

            await LoadAllocationDropdownsAsync();
            ViewBag.RentalId = rental.Id;

            return View("CreateAllocation", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAllocationFromRental(int rentalId, AllocationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await LoadAllocationDropdownsAsync();
                return View("CreateAllocation", model);
            }

            var rental = await _context.RentalRequests
                .Include(r => r.FridgeModel)
                .FirstOrDefaultAsync(r => r.Id == rentalId);

            if (rental == null)
            {
                ModelState.AddModelError("", "Rental request not found.");
                await LoadAllocationDropdownsAsync();
                return View("CreateAllocation", model);
            }

            // Find available fridge units to allocate
            var availableUnits = await _context.FridgeUnits
                .Where(fu => fu.FridgeModelId == model.FridgeModelId &&
                            fu.Status == FridgeStatus.InWarehouse)
                .Take(model.Quantity)
                .ToListAsync();

            if (availableUnits.Count < model.Quantity)
            {
                ModelState.AddModelError("", $"Only {availableUnits.Count} units available. Requested: {model.Quantity}");
                await LoadAllocationDropdownsAsync();
                return View("CreateAllocation", model);
            }

            var customer = await _context.Customers
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == model.CustomerId);

            if (customer == null)
            {
                ModelState.AddModelError("", "Customer not found.");
                await LoadAllocationDropdownsAsync();
                return View("CreateAllocation", model);
            }

            // Create allocations for each unit
            foreach (var fridgeUnit in availableUnits)
            {
                var allocation = new FridgeAllocation
                {
                    FridgeUnitId = fridgeUnit.Id,
                    CustomerId = model.CustomerId,
                    AllocationDate = DateTime.UtcNow,
                    CreatedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                };

                // Update fridge unit status
                fridgeUnit.Status = FridgeStatus.Allocated;
                fridgeUnit.CurrentCustomerId = model.CustomerId;

                _context.FridgeAllocations.Add(allocation);
            }

            // Update rental request
            rental.UpdatedAt = DateTime.UtcNow;

            // Send notification
            _context.Notifications.Add(new Notification
            {
                Title = "Fridge Allocated",
                RecipientUserId = customer.UserId,
                Message = $"{model.Quantity} fridge(s) ({rental.FridgeModel.Name}) have been allocated to your account.",
                CreatedAt = DateTime.UtcNow,
                IsRead = false,
                TargetController = "Customer",
                TargetAction = "MyAllocations"
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"{model.Quantity} fridge(s) allocated successfully!";
            return RedirectToAction("Requests");
        }

        [HttpGet]
        public async Task<IActionResult> CreateAllocationManually()
        {
            await LoadAllocationDropdownsAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAllocationManually(AllocationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await LoadAllocationDropdownsAsync();
                return View(model);
            }

            // Find available fridge units
            var availableUnits = await _context.FridgeUnits
                .Where(fu => fu.FridgeModelId == model.FridgeModelId &&
                            fu.Status == FridgeStatus.InWarehouse)
                .Take(model.Quantity)
                .ToListAsync();

            if (availableUnits.Count < model.Quantity)
            {
                ModelState.AddModelError("", $"Only {availableUnits.Count} units available. Requested: {model.Quantity}");
                await LoadAllocationDropdownsAsync();
                return View(model);
            }

            var customer = await _context.Customers
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == model.CustomerId);

            if (customer == null)
            {
                ModelState.AddModelError("", "Customer not found.");
                await LoadAllocationDropdownsAsync();
                return View(model);
            }

            var fridgeModel = await _context.FridgeModels.FindAsync(model.FridgeModelId);

            // Create allocations for each unit
            foreach (var fridgeUnit in availableUnits)
            {
                var allocation = new FridgeAllocation
                {
                    FridgeUnitId = fridgeUnit.Id,
                    CustomerId = model.CustomerId,
                    AllocationDate = DateTime.UtcNow,
                    CreatedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                };

                // Update fridge unit status
                fridgeUnit.Status = FridgeStatus.Allocated;
                fridgeUnit.CurrentCustomerId = model.CustomerId;

                _context.FridgeAllocations.Add(allocation);
            }

            // Send notification
            _context.Notifications.Add(new Notification
            {
                Title = "Fridge Allocated",
                RecipientUserId = customer.UserId,
                Message = $"{model.Quantity} fridge(s) ({fridgeModel.Name}) have been allocated to your account.",
                CreatedAt = DateTime.UtcNow,
                IsRead = false,
                TargetController = "Customer",
                TargetAction = "MyAllocations"
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"{model.Quantity} fridge(s) allocated successfully!";
            return RedirectToAction("Allocations");
        }

        public async Task<IActionResult> Allocations()
        {
            var allocations = await _context.FridgeAllocations
                .Include(a => a.FridgeUnit)
                .ThenInclude(fu => fu.FridgeModel)
                .Include(a => a.Customer)
                .Include(a => a.CreatedBy)
                .OrderByDescending(a => a.AllocationDate)
                .ToListAsync();

            return View(allocations);
        }

        public async Task<IActionResult> FridgeTracking()
        {
            try
            {
                var trackedFridges = await _context.FridgeAllocations
                    .Include(a => a.FridgeUnit)
                    .ThenInclude(fu => fu.FridgeModel)
                    .Include(a => a.Customer)
                    .Include(a => a.CreatedBy)
                    .Where(a => a.DeallocationDate == null) // Active allocations only
                    .OrderByDescending(a => a.AllocationDate)
                    .Select(a => new FridgeTrackingViewModel
                    {
                        FridgeModel = a.FridgeUnit.FridgeModel.Name,
                        SerialNumber = a.FridgeUnit.SerialNumber,
                        CustomerName = a.Customer.BusinessName,
                        CustomerAddress = a.Customer.PhysicalAddress ?? "Address not provided",
                        AllocatedBy = a.CreatedBy != null ? a.CreatedBy.UserName : "System",
                        AllocationDate = a.AllocationDate,
                        Status = a.FridgeUnit.Status.ToString()
                    })
                    .ToListAsync();

                if (!trackedFridges.Any())
                {
                    TempData["InfoMessage"] = "No active fridge allocations found.";
                }

                return View(trackedFridges);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Something went wrong while loading the fridge tracking data.";
                return View(new List<FridgeTrackingViewModel>());
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeallocateFridge(int allocationId)
        {
            try
            {
                var allocation = await _context.FridgeAllocations
                    .Include(a => a.FridgeUnit)
                    .Include(a => a.Customer)
                    .FirstOrDefaultAsync(a => a.Id == allocationId);

                if (allocation == null)
                    return Json(new { success = false, message = "Allocation not found." });

                if (allocation.DeallocationDate.HasValue)
                    return Json(new { success = false, message = "Fridge already deallocated." });

                // Update allocation
                allocation.DeallocationDate = DateTime.UtcNow;

                // Update fridge unit status
                allocation.FridgeUnit.Status = FridgeStatus.InWarehouse;
                allocation.FridgeUnit.CurrentCustomerId = null;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Fridge deallocated successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Failed to deallocate: {ex.Message}" });
            }
        }

        private async Task LoadAllocationDropdownsAsync()
        {
            ViewBag.Customers = await _context.Customers
                .Where(c => c.Status == CustomerStatus.Active)
                .Select(c => new SelectListItem
                {
                    Text = $"{c.BusinessName} - {c.ContactName}",
                    Value = c.Id.ToString()
                })
                .ToListAsync();

            ViewBag.FridgeModels = await _context.FridgeModels
                .Select(fm => new SelectListItem
                {
                    Text = $"{fm.Name} - {fm.Category} (Available: {_context.FridgeUnits.Count(fu => fu.FridgeModelId == fm.Id && fu.Status == FridgeStatus.InWarehouse)})",
                    Value = fm.Id.ToString()
                })
                .ToListAsync();
        }
        #endregion
    }
}