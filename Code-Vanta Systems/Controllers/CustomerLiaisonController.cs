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

        #region Dashboard
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            ViewBag.Notifications = await _context.Notifications
                .Where(n => n.RecipientUserId == userId && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return View();
        }

        //public async Task<IActionResult> Dashboard()
        //{
        //    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        //    ViewBag.Notifications = await _context.Notifications
        //        .Where(n => n.RecipientUserId == userId && !n.IsRead)
        //        .OrderByDescending(n => n.CreatedAt)
        //        .ToListAsync();

        //    return View();
        //}
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
            ViewBag.RejectCustomerCount = await _context.Customers.CountAsync(c => c.Status == CustomerStatus.Rejected);

            return View(pendingCustomers);
        }

        public async Task<IActionResult> ActiveCustomers()
        {
            var activeCustomers = await _context.Customers
                .Include(c => c.Location)
                .Where(c => c.Status == CustomerStatus.Active)
                .ToListAsync();

            ViewBag.PendingCustomerCount = await _context.Customers.CountAsync(c => c.Status == CustomerStatus.Pending);
            ViewBag.ActiveCustomerCount = await _context.Customers.CountAsync(c => c.Status == CustomerStatus.Active);
            ViewBag.RejectCustomerCount = await _context.Customers.CountAsync(c => c.Status == CustomerStatus.Rejected);

            return View(activeCustomers);
        }

        public async Task<IActionResult> RejectCustomers()
        {
            var rejectedCustomers = await _context.Customers
                .Include(c => c.Location)
                .Where(c => c.Status == CustomerStatus.Rejected)
                .ToListAsync();

            ViewBag.PendingCustomerCount = await _context.Customers.CountAsync(c => c.Status == CustomerStatus.Pending);
            ViewBag.ActiveCustomerCount = await _context.Customers.CountAsync(c => c.Status == CustomerStatus.Active);
            ViewBag.RejectCustomerCount = await _context.Customers.CountAsync(c => c.Status == CustomerStatus.Rejected);

            return View(rejectedCustomers);
        }

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
            return RedirectToAction("RejectCustomers");
        }

        #endregion


        #region Rental Requests

        public async Task<IActionResult> Requests()
        {
            var requests = await _context.RentalRequests
                .Include(r => r.Fridge)
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
                // 1️⃣ Load rental request with fridge and user info
                var rental = await _context.RentalRequests
                    .Include(r => r.Fridge)
                    .Include(r => r.RequestedByUser)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (rental == null)
                    return Json(new { success = false, message = "Rental not found." });

                if (rental.Fridge == null)
                    return Json(new { success = false, message = "Fridge not found." });

                // 2️⃣ Find the corresponding numeric Customer ID
                var customer = await _context.Customers
     .FirstOrDefaultAsync(c => c.UserId == rental.RequestedByUserId);

                if (customer == null)
                    return Json(new { success = false, message = "Customer record not found." });

                // 3️⃣ Check if enough stock is available
                if (rental.Fridge.StockQuantity < rental.Quantity)
                    return Json(new { success = false, message = "Not enough stock available." });

                // 4️⃣ Find available fridge units to allocate
                var availableUnits = await _context.FridgeUnits
                    .Where(fu => fu.FridgeId == rental.FridgeId
                              && !fu.IsRented
                              && !fu.IsDeleted
                              && fu.Status == FridgeStatus.Available)
                    .Take(rental.Quantity)
                    .ToListAsync();

                if (availableUnits.Count < rental.Quantity)
                    return Json(new { success = false, message = "Not enough units available to allocate." });

                // 5️⃣ Assign units to the customer
                foreach (var unit in availableUnits)
                {
                    unit.CustomerId = customer.Id;          // integer ID
                    unit.IsRented = true;                    // mark as rented
                    unit.Status = FridgeStatus.Allocated;   // set status
                    unit.UpdatedAt = DateTime.UtcNow;       // timestamp
                }

                // 6️⃣ Update stock and rental status
                rental.Fridge.StockQuantity -= rental.Quantity;
                rental.Status = "Approved"; // you could use an enum if you have one
                rental.UpdatedAt = DateTime.UtcNow;

                // 7️⃣ Notify the customer
                var notification = new Notification
                {
                    Title = "Rental Approved",
                    RecipientUserId = rental.RequestedByUserId,
                    Message = $"Your rental request for {rental.Fridge.Model} ({rental.Quantity}) has been approved and units allocated.",
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false,
                    TargetController = "Customer",
                    TargetAction = "MyRentalRequests"
                };
                _context.Notifications.Add(notification);

                // 8️⃣ Save all changes to database
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Rental approved and units allocated!" });
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

                var customer = await userManager.GetUsersInRoleAsync("Customer");
                var liaisonUser = customer.FirstOrDefault();

                if (liaisonUser != null)
                {
                    var notification = new Notification
                    {
                        RecipientUserId = liaisonUser.Id,
                        Title = "Fridge Request Rejected",
                        Message = $"Your rental request for {rental.Fridge.Model} ({rental.Quantity}) has been rejected.",
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

        #endregion


        #region Allocations

        // GET: Create allocation from rental
        [HttpGet]
        [ActionName("CreateAllocationFromRental")]
        public async Task<IActionResult> CreateAllocationFromRental(int rentalId)
        {
            var rental = await _context.RentalRequests
                .Include(r => r.Fridge)
                .Include(r => r.RequestedByUser)
                .ThenInclude(u => u.CustomerProfile)
                .FirstOrDefaultAsync(r => r.Id == rentalId);

            if (rental == null) return NotFound("Rental request not found.");
            if (!rental.IsPaid || rental.Status != "Approved")
                return BadRequest("Rental must be approved and paid to allocate.");

            var model = new AllocationViewModel
            {
                FridgeId = rental.FridgeId,
                CustomerId = rental.RequestedByUser.CustomerProfile?.Id ?? 0,
                Quantity = rental.Quantity
            };

            ViewBag.Customers = await _context.Customers
                .Where(c => c.Status == CustomerStatus.Active)
                .Select(c => new SelectListItem { Text = c.BusinessName, Value = c.Id.ToString() })
                .ToListAsync();

            ViewBag.Fridges = await _context.Fridges
                .Select(f => new SelectListItem { Text = f.Model, Value = f.Id.ToString() })
                .ToListAsync();

            ViewBag.RentalId = rental.Id;

            return View("CreateAllocation", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("CreateAllocationFromRental")]
        public async Task<IActionResult> CreateAllocationFromRentalPost(int rentalId, AllocationViewModel model)
        {
            if (!ModelState.IsValid) return View("CreateAllocation", model);

            var rental = await _context.RentalRequests
                .Include(r => r.Fridge)
                .FirstOrDefaultAsync(r => r.Id == rentalId);

            if (rental == null)
            {
                ModelState.AddModelError("", "Rental request not found.");
                return View("CreateAllocation", model);
            }

            var fridge = await _context.Fridges.FindAsync(model.FridgeId);
            if (fridge == null)
            {
                ModelState.AddModelError("", "Fridge not found.");
                return View("CreateAllocation", model);
            }

            if (fridge.StockQuantity < model.Quantity)
            {
                ModelState.AddModelError("", "Not enough stock.");
                return View("CreateAllocation", model);
            }

            fridge.StockQuantity -= model.Quantity;

            var allocation = new FridgeAllocation
            {
                FridgeId = model.FridgeId,
                CustomerId = model.CustomerId,
                Quantity = model.Quantity,
                AllocationDate = DateTime.UtcNow,
                CreatedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            };

            _context.FridgeAllocations.Add(allocation);
             

            rental.Allocation = allocation;
            rental.AllocationId = allocation.Id;
            rental.UpdatedAt = DateTime.UtcNow;

            _context.Notifications.Add(new Notification
            {
                Title = "Fridge Allocated",
                RecipientUserId = rental.RequestedByUserId,
                Message = $"Your rental request for {rental.Fridge.Model} ({rental.Quantity}) has been allocated.",
                CreatedAt = DateTime.UtcNow,
                IsRead = false,
                TargetController = "Customer",
                TargetAction = "MyRentals"
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Fridge allocated successfully!";
            return RedirectToAction("Requests");
        }

        #endregion


        #region Fridge Tracking

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

        //public async Task<IActionResult> FridgeTracking()
        //{
        //    var trackedFridges = await _context.FridgeAllocations
        //        .Include(a => a.Fridge)
        //        .Include(a => a.Customer)  
        //        .Include(a => a.CreatedBy)
        //        .OrderByDescending(a => a.AllocationDate)
        //        .ToListAsync();

        //    return View(trackedFridges);
        //}








        //[HttpGet]
        //public async Task<IActionResult> CreateAllocationManually()
        //{

        //    ViewBag.Customers = await _context.Customers
        //         .Include(c => c.Location)
        //       .Where(c => c.Status == CustomerStatus.Active)
        //        .Select(c => new SelectListItem { Text = c.BusinessName, Value = c.Id.ToString() })
        //        .ToListAsync();

        //    ViewBag.Fridges = await _context.Fridges
        //        .Select(f => new SelectListItem { Text = f.Model, Value = f.Id.ToString() })
        //        .ToListAsync();

        //    return View();
        //}

        //[HttpPost]
        //public async Task<IActionResult> CreateAllocationManually(AllocationViewModel model)
        //{
        //    if (!ModelState.IsValid) return View(model);

        //    var fridge = await _context.Fridges.FindAsync(model.FridgeId);
        //    if (fridge == null) { ModelState.AddModelError("", "Fridge not found."); return View(model); }
        //    if (fridge.StockQuantity < model.Quantity) { ModelState.AddModelError("", "Not enough stock."); return View(model); }

        //    fridge.StockQuantity -= model.Quantity;

        //    var allocation = new FridgeAllocation
        //    {
        //        CustomerId = model.CustomerId,
        //        FridgeId = model.FridgeId,
        //        Quantity = model.Quantity,
        //        AllocationDate = DateTime.UtcNow,
        //        CreatedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
        //    };

        //    await _context.FridgeAllocations.AddAsync(allocation);
        //    await _context.SaveChangesAsync();

        //    return RedirectToAction("Allocations");
        //}




        [HttpGet]
        public async Task<IActionResult> CreateAllocationManually()
        {
            ViewBag.Customers = await _context.Customers
                .Include(c => c.Location)
                .Where(c => c.Status == CustomerStatus.Active)
                .Select(c => new SelectListItem
                {
                    Text = c.BusinessName,
                    Value = c.Id.ToString()
                })
                .ToListAsync();

            ViewBag.Fridges = await _context.Fridges
                .Select(f => new SelectListItem
                {
                    Text = f.Model + " (" + f.StockQuantity + " available)",
                    Value = f.Id.ToString()
                })
                .ToListAsync();

            return View();  
        }

       

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAllocationManually(AllocationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync();  
                return View("CreateAllocation", model);
            }

            var fridge = await _context.Fridges.FindAsync(model.FridgeId);
            if (fridge == null)
            {
                ModelState.AddModelError("", "Fridge not found.");
                await LoadDropdownsAsync();
                return View("CreateAllocation", model);
            }

            if (fridge.StockQuantity < model.Quantity)
            {
                ModelState.AddModelError("", "Not enough stock available.");
                await LoadDropdownsAsync();
                return View("CreateAllocation", model);
            }

            var customer = await _context.Customers
                .Include(c => c.User)  
                .FirstOrDefaultAsync(c => c.Id == model.CustomerId);

            if (customer == null)
            {
                ModelState.AddModelError("", "Customer not found.");
                await LoadDropdownsAsync();
                return View("CreateAllocation", model);
            }

 
            fridge.StockQuantity -= model.Quantity;

 
            var allocation = new FridgeAllocation
            {
                CustomerId = model.CustomerId,
                FridgeId = model.FridgeId,
                Quantity = model.Quantity,
                AllocationDate = DateTime.UtcNow,
                CreatedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            };

            _context.FridgeAllocations.Add(allocation);

            var units = await _context.FridgeUnits
        .Where(fu => fu.FridgeId == model.FridgeId
                  && !fu.IsRented
                  && !fu.IsDeleted
                  && fu.Status == FridgeStatus.Available)
        .Take(model.Quantity)
        .ToListAsync();

            foreach (var unit in units)
            {
                unit.CustomerId = model.CustomerId;
                unit.IsRented = true;
                unit.Status = FridgeStatus.Allocated;
                unit.UpdatedAt = DateTime.UtcNow;
            }

            _context.Notifications.Add(new Notification
            {
                Title = "Fridge Allocated",
                RecipientUserId = customer.UserId,  
                Message = $"A fridge ({fridge.Model}) has been allocated to your account. Quantity: {model.Quantity}.",
                CreatedAt = DateTime.UtcNow,
                IsRead = false,
                TargetController = "Customer",
                TargetAction = "MyAllocations" 

            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Fridge allocated successfully!";
            return RedirectToAction("Allocations");
        }



        private async Task LoadDropdownsAsync()
        {
            ViewBag.Customers = await _context.Customers
                .Where(c => c.Status == CustomerStatus.Active)
                .Select(c => new SelectListItem { Text = c.BusinessName, Value = c.Id.ToString() })
                .ToListAsync();

            ViewBag.Fridges = await _context.Fridges
                .Select(f => new SelectListItem { Text = f.Model, Value = f.Id.ToString() })
                .ToListAsync();
        }











        public async Task<IActionResult> FridgeTracking()
        {
            try
            {

                var trackedFridges = await _context.FridgeUnits
                    .Where(fu => fu.CustomerId != null && !fu.IsDeleted)
                    .Include(fu => fu.Fridge)             // Fridge info
                    .Include(fu => fu.Customer)           // Customer info
                        .ThenInclude(c => c.Location)     // Customer's location
                    .Include(fu => fu.Customer)           // for allocated by if needed
                    .OrderByDescending(fu => fu.CreatedAt)
                    .Select(fu => new FridgeTrackingViewModel
                    {
                        FridgeModel = fu.Fridge.Model,
                        UnitSerialNumber = fu.SerialNumber,
                        CustomerName = fu.Customer.BusinessName,
                        CustomerAddress = fu.Customer.PhysicalAddress ?? "Address not provided",
                        Status = fu.Status.ToString(),
                        Condition = fu.Condition.ToString(),
                        AllocatedAt = fu.UpdatedAt ?? fu.CreatedAt
                    })
                    .ToListAsync();

                if (!trackedFridges.Any())
                {
                    TempData["InfoMessage"] = "No assigned fridge units found.";
                }

                return View(trackedFridges);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Something went wrong while loading the fridge tracking data.";
                return View(new List<FridgeTrackingViewModel>());
            }
        }






        #endregion
    }
}
