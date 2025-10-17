using Code_Vanta_Systems.Data;
using Code_Vanta_Systems.Models;
using Code_Vanta_Systems.Models.Enums;
using Code_Vanta_Systems.Services;
using Code_Vanta_Systems.ViewModels;
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
        private readonly UserManager<ApplicationUser> userManager;

        public CustomerLiaisonController(ApplicationDbContext context, EmailService emailService, UserManager<ApplicationUser> userManager)
        {

            _context = context;
            _emailService = emailService;
            this.userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            ViewBag.Notifications = await _context.Notifications
                .Where(n => n.RecipientUserId == userId && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return View();
        }
        #region Customers
        // GET: Customer/Pending
        public async Task<IActionResult> PendingCustomers()
        {
            // Get all pending customers
            var pendingCustomers = await _context.Customers
                .Include(c => c.Location)
                .Where(c => c.Status == CustomerStatus.Pending)
                .ToListAsync();

            ViewBag.PendingCustomerCount = await _context.Customers
                 .CountAsync(c => c.Status == CustomerStatus.Pending);
            ViewBag.ActiveCustomerCount = await _context.Customers
                .CountAsync(c => c.Status == CustomerStatus.Active);
            ViewBag.RejectCustomerCount = await _context.Customers
               .CountAsync(c => c.Status == CustomerStatus.Rejected);

            return View(pendingCustomers);
        }

        public async Task<IActionResult> ActiveCustomers()
        {
            // Get all pending customers
            var activeCustomers = await _context.Customers
                .Include(c => c.Location)
                .Where(c => c.Status == CustomerStatus.Active)
                .ToListAsync();

            ViewBag.PendingCustomerCount = await _context.Customers
                 .CountAsync(c => c.Status == CustomerStatus.Pending);
            ViewBag.ActiveCustomerCount = await _context.Customers
                .CountAsync(c => c.Status == CustomerStatus.Active);
            ViewBag.RejectCustomerCount = await _context.Customers
               .CountAsync(c => c.Status == CustomerStatus.Rejected);

            return View(activeCustomers);
        }

        public async Task<IActionResult> RejectCustomers()
        {
            // Get all pending customers
            var rejectedCustomers = await _context.Customers
                .Include(c => c.Location)
                .Where(c => c.Status == CustomerStatus.Rejected)
                .ToListAsync();
            ViewBag.PendingCustomerCount = await _context.Customers
                            .CountAsync(c => c.Status == CustomerStatus.Pending);
            ViewBag.ActiveCustomerCount = await _context.Customers
                .CountAsync(c => c.Status == CustomerStatus.Active);
            ViewBag.RejectCustomerCount = await _context.Customers
               .CountAsync(c => c.Status == CustomerStatus.Rejected);

            return View(rejectedCustomers);
        }

        // GET: CustomerDashboard/ViewCustomer/5
        public async Task<IActionResult> ViewCustomer(int id)
        {
            if (id == 0) return NotFound();

            var customer = await _context.Customers
                .Include(c => c.Location)
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

            if (customer == null)
                return NotFound("Customer not found or not pending.");

            // ✅ Change status to Active
            customer.Status = CustomerStatus.Active;
            _context.Customers.Update(customer);

            await _context.SaveChangesAsync(); // Save the status change first

            // ✅ Send approval email
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
                catch (Exception ex)
                {
                    // Log the email failure
                    Console.WriteLine($"Failed to send email to {customer.User.Email}: {ex.Message}");
                }
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

            if (customer == null)
                return NotFound("Customer not found or not pending.");

            // ✅ Change status to Rejected
            customer.Status = CustomerStatus.Rejected;
            _context.Customers.Update(customer);

            await _context.SaveChangesAsync(); // Save status change first

            // ✅ Send rejection email
            if (customer.User != null)
            {
                try
                {
                    await _emailService.SendEmailAsync(
                        customer.User.Email,
                        "Account Rejected - CodeVanta Systems",
                        $"Hello {customer.ContactName},<br><br>We regret to inform you that your account '{customer.BusinessName}' has been rejected.<br>" +
                        $"Please contact support if you believe this is a mistake or for further assistance.<br><br>Best regards,<br>CodeVanta Team"
                    );
                }
                catch (Exception ex)
                {
                    // Log email failure
                    Console.WriteLine($"Failed to send rejection email to {customer.User.Email}: {ex.Message}");
                }
            }

            TempData["Success"] = $"Customer '{customer.BusinessName}' has been rejected and emailed.";
            return RedirectToAction("RejectCustomers");
        }
        #endregion
        //Esethu 



        public async Task<IActionResult> PendingRentals()
        {
            var rentals = await _context.RentalRequests
                .Include(r => r.Fridge)
                .Include(r => r.RequestedByUser)
                .Where(r => r.Status == "Pending")
                .OrderBy(r => r.CreatedAt)
                .ToListAsync();

            return View(rentals);
        }
        [HttpPost]
        public async Task<IActionResult> ApproveRental(int id)
        {
            try
            {
                var rental = await _context.RentalRequests
                    .Include(r => r.Fridge)
                    .Include(r => r.RequestedByUser)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (rental == null)
                    return Json(new { success = false, message = "Rental not found." });

                if (rental.Fridge == null)
                    return Json(new { success = false, message = "Fridge not found." });


                if (rental.Fridge.StockQuantity < rental.Quantity)
                    return Json(new { success = false, message = "Not enough stock available." });

                rental.Fridge.StockQuantity -= rental.Quantity;


                rental.Status = "Approved";
                rental.UpdatedAt = DateTime.UtcNow;

                var fridgeModel = rental.Fridge?.Model ?? "fridge";


                var notification = new Notification
                {
                    Title = "Rental Approved",
                    RecipientUserId = rental.RequestedByUserId,
                    Message = $"Your rental request for {fridgeModel} ({rental.Quantity}) has been approved.",
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false,
                    TargetController = "Customer",
                    TargetAction = "MyRentalRequests"
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Rental approved and stock updated!" });
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                return Json(new { success = false, message = $"Failed to approve rental: {inner}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RejectRental(int id)
        {
            try
            {
                var rental = await _context.RentalRequests
                    .Include(r => r.Fridge)
                    .Include(r => r.RequestedByUser)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (rental == null)
                    return Json(new { success = false, message = "Rental not found." });

                rental.Status = "Rejected";
                rental.UpdatedAt = DateTime.UtcNow;

                var fridgeModel = rental.Fridge?.Model ?? "fridge";

                var customer = await userManager.GetUsersInRoleAsync("Customer");
                var liaisonUser = customer.FirstOrDefault();

                if (liaisonUser != null)
                {
                    // ✅ 2. Create notification
                    var notification = new Notification
                    {
                        RecipientUserId = liaisonUser.Id,
                        Title = "Fridge Request Rejected",
                        Message = $"Your rental request for {fridgeModel} ({rental.Quantity}) has been rejected.",
                        CreatedAt = DateTime.UtcNow,
                        IsRead = false,
                        TargetController = "Customer",
                        TargetAction = "MyRentalRequests"
                    };

                    _context.Notifications.Add(notification);
                    await _context.SaveChangesAsync();
                }




                return Json(new { success = true, message = "Rental rejected!" });
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                return Json(new { success = false, message = $"Failed to reject rental: {inner}" });
            }
        }


        // Allocations
        public async Task<IActionResult> Allocations()
        {
            var allocations = await _context.FridgeAllocations
                .Include(a => a.Fridge)
                .Include(a => a.Customer)
                .Include(a => a.CreatedBy)
                .OrderByDescending(a => a.AllocationDate)
                .ToListAsync();

            return View(allocations);
        }

        [HttpGet]
        public async Task<IActionResult> CreateAllocation()
        {

            ViewBag.Customers = await _context.Customers
                 .Include(c => c.Location)
               .Where(c => c.Status == CustomerStatus.Active)
                .Select(c => new SelectListItem { Text = c.BusinessName, Value = c.Id.ToString() })
                .ToListAsync();

            ViewBag.Fridges = await _context.Fridges
                .Select(f => new SelectListItem { Text = f.Model, Value = f.Id.ToString() })
                .ToListAsync();

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateAllocation(AllocationViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var fridge = await _context.Fridges.FindAsync(model.FridgeId);
            if (fridge == null) { ModelState.AddModelError("", "Fridge not found."); return View(model); }
            if (fridge.StockQuantity < model.Quantity) { ModelState.AddModelError("", "Not enough stock."); return View(model); }

            fridge.StockQuantity -= model.Quantity;

            var allocation = new FridgeAllocation
            {
                CustomerId = model.CustomerId,
                FridgeId = model.FridgeId,
                Quantity = model.Quantity,
                AllocationDate = DateTime.UtcNow,
                CreatedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            };

            await _context.FridgeAllocations.AddAsync(allocation);
            await _context.SaveChangesAsync();

            return RedirectToAction("Allocations");
        }

        [HttpGet]
        public async Task<IActionResult> EditAllocation(int id)
        {
            var allocation = await _context.FridgeAllocations.FindAsync(id);
            if (allocation == null) return NotFound();

            ViewBag.Customers = await _context.Customers
                .Select(c => new SelectListItem { Text = c.BusinessName, Value = c.Id.ToString() })
                .ToListAsync();

            ViewBag.Fridges = await _context.Fridges
                .Select(f => new SelectListItem { Text = f.Model, Value = f.Id.ToString() })
                .ToListAsync();

            return View(allocation);
        }

        [HttpPost]
        public async Task<IActionResult> EditAllocation(int id, AllocationViewModel model)
        {
            var allocation = await _context.FridgeAllocations.FindAsync(id);
            if (allocation == null) return NotFound();

            var fridge = await _context.Fridges.FindAsync(model.FridgeId);
            if (fridge == null) { ModelState.AddModelError("", "Fridge not found."); return View(model); }

            fridge.StockQuantity += allocation.Quantity; // return old
            if (fridge.StockQuantity < model.Quantity) { ModelState.AddModelError("", "Not enough stock."); return View(model); }
            fridge.StockQuantity -= model.Quantity;

            allocation.CustomerId = model.CustomerId;
            allocation.FridgeId = model.FridgeId;
            allocation.Quantity = model.Quantity;

            await _context.SaveChangesAsync();
            return RedirectToAction("Allocations");
        }

        //// Fridge Tracking
        //public async Task<IActionResult> FridgeTracking()
        //{
        //    var trackedFridges = await _context.FridgeAllocations
        //        .Include(a => a.Fridge)
        //        .Include(a => a.Customer)
        //        .Include(a => a.CreatedBy)
        //         .Where(a => a.Fridge != null && a.Customer != null)
        //        .OrderByDescending(a => a.AllocationDate)
        //        .ToListAsync();

        //    return View(trackedFridges);
        //}
        //Combined view

        public async Task<IActionResult> FridgeTracking()
        {
            // 1️⃣ Fetch approved rental requests
            var approvedRequests = await _context.RentalRequests
                .Include(r => r.Fridge)
                .Include(r => r.RequestedByUser)
                .Include(r => r.RequestedByUser)
                .Where(r => r.Status == "Approved")
                .Select(r => new FridgeTrackingViewModel
                {
                    FridgeModel = r.Fridge.Model,
                    SerialNumber = r.Fridge.SerialNumber,
                    CustomerName = r.RequestedByUser.FullName ?? r.RequestedByUser.UserName,
                    PhysicalAddress = "" ,// not always known until allocation
                    Quantity = r.Quantity,
                    Status = "Approved Request",
                    AllocatedBy = "Pending Allocation",
                    Date = r.UpdatedAt
                })
                .ToListAsync();

            // 2️⃣ Fetch actual fridge allocations
            var allocations = await _context.FridgeAllocations
                .Include(a => a.Fridge)
                .Include(a => a.Customer)
                .Include(a => a.CreatedBy)
                .Select(a => new FridgeTrackingViewModel
                {
                    FridgeModel = a.Fridge.Model,
                    SerialNumber = a.Fridge.SerialNumber,
                    CustomerName = a.Customer.BusinessName,
                    PhysicalAddress = a.Customer.PhysicalAddress, // ✅ use physical address
                    Quantity = a.Quantity,
                    Status = "Allocated",
                    AllocatedBy = a.CreatedBy.FullName ?? a.CreatedBy.UserName,
                    Date = a.AllocationDate
                })
                .ToListAsync();

            // 3️⃣ Merge both lists
            var combined = approvedRequests.Concat(allocations)
                .OrderByDescending(x => x.Date)
                .ToList();

            return View(combined);
        }

        ////new fridge tracking
        //public async Task<IActionResult> FridgeTracking()
        //{
        //    var trackedFridges = await _context.FridgeAllocations
        //        .Include(a => a.Fridge)
        //        .Include(a => a.Customer)
        //        .Include(a => a.CreatedBy)
        //        .Where(a => a.Fridge != null && a.Customer != null)
        //        .OrderByDescending(a => a.AllocationDate)
        //        .ToListAsync();

        //    return View(trackedFridges);
        //}
        // Requests
        public async Task<IActionResult> Requests()
        {
            var requests = await _context.RentalRequests
                .Include(r => r.Fridge)
                .Include(r => r.RequestedByUser)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(requests);
        }

        //Adding delete and edit in the active customer table
        // GET: CustomerLiaison/EditCustomer/5
        [HttpGet]
        public async Task<IActionResult> EditCustomer(int id)
        {
            var customer = await _context.Customers
                .Include(c => c.Location)
                .FirstOrDefaultAsync(c => c.Id == id && c.Status == CustomerStatus.Active);

            if (customer == null)
            {
                TempData["ErrorMessage"] = "Customer not found or inactive.";
                return RedirectToAction(nameof(ActiveCustomers));
            }

            var viewModel = new EditCustomerViewModel
            {
                Id = customer.Id,
                BusinessName = customer.BusinessName,
                ContactName = customer.ContactName,
                Phone = customer.Phone,
                Email = customer.Email,
                BusinessType = customer.BusinessType,
                PhysicalAddress = customer.PhysicalAddress,
                LocationId = customer.LocationId,
                CurrentLocationDescription = customer.Location?.Description,
                AvailableLocations = await _context.Locations
                    .Select(l => new SelectListItem
                    {
                        Value = l.Id.ToString(),
                        Text = l.Description
                    }).ToListAsync()
            };

            return View(viewModel);
        }

        // POST: CustomerLiaison/EditCustomer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCustomer(EditCustomerViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AvailableLocations = await _context.Locations
                    .Select(l => new SelectListItem
                    {
                        Value = l.Id.ToString(),
                        Text = l.Description
                    }).ToListAsync();
                return View(model);
            }

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == model.Id);
            if (customer == null)
            {
                TempData["ErrorMessage"] = "Customer not found.";
                return RedirectToAction(nameof(ActiveCustomers));
            }

            customer.BusinessName = model.BusinessName;
            customer.ContactName = model.ContactName;
            customer.Phone = model.Phone;
            customer.Email = model.Email;
            customer.BusinessType = model.BusinessType;
            customer.PhysicalAddress = model.PhysicalAddress;
            customer.LocationId = model.LocationId;

            _context.Customers.Update(customer);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Customer '{model.BusinessName}' updated successfully.";
            return RedirectToAction(nameof(ActiveCustomers));
        }

        // POST: CustomerLiaison/SoftDeleteCustomer/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SoftDeleteCustomer(int id)
        {
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == id);
            if (customer == null)
            {
                TempData["ErrorMessage"] = "Customer not found.";
                return RedirectToAction(nameof(ActiveCustomers));
            }

            customer.IsActive = false;
            customer.Status = CustomerStatus.Rejected; // Optional: mark as rejected or inactive
            _context.Customers.Update(customer);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Customer '{customer.BusinessName}' deactivated successfully.";
            return RedirectToAction(nameof(ActiveCustomers));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReactivateCustomer(int id)
        {
            var customer = await _context.Customers
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (customer == null)
            {
                TempData["ErrorMessage"] = "Customer not found.";
                return RedirectToAction(nameof(RejectCustomers)); // Redirect to inactive/rejected list
            }

            var user = customer.User;

            // 1️⃣ Reactivate customer record
            customer.IsActive = true;
            customer.Status = CustomerStatus.Active;

            // 2️⃣ Remove account lockout (allow login again)
            if (user != null)
            {
                await userManager.SetLockoutEndDateAsync(user, null);

                // Ensure Customer role exists
                const string roleName = "Customer";
                if (await userManager.IsInRoleAsync(user, roleName) == false)
                {
                    if (await _context.Roles.AnyAsync(r => r.Name == roleName))
                    {
                        await userManager.AddToRoleAsync(user, roleName);
                    }
                    else
                    {
                        TempData["WarningMessage"] = $"Customer '{user.Email}' reactivated but role '{roleName}' not found. Please assign manually.";
                    }
                }
            }

            _context.Customers.Update(customer);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Customer '{customer.BusinessName}' has been successfully reactivated.";
            return RedirectToAction(nameof(ActiveCustomers)); // Back to the active list
        }
    }
}
