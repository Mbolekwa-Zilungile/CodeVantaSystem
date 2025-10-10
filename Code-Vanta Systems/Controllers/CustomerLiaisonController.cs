using Code_Vanta_Systems.Data;
using Code_Vanta_Systems.Models;
using Code_Vanta_Systems.Models.Enums;
using Code_Vanta_Systems.Services;
using Code_Vanta_Systems.ViewModels;
using Microsoft.AspNetCore.Authorization;
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

        public CustomerLiaisonController(ApplicationDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
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
            var rental = await _context.RentalRequests
                .Include(r => r.Fridge)
                .Include(r => r.RequestedByUser)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (rental == null) return Json(new { success = false, message = "Rental not found." });

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == rental.RequestedByUserId);
            if (customer == null) return Json(new { success = false, message = "Customer not found." });

            if (rental.Fridge.StockQuantity < rental.Quantity)
                return Json(new { success = false, message = "Not enough stock to approve." });

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                rental.Fridge.StockQuantity -= rental.Quantity;
                rental.Status = "Approved";
                rental.UpdatedAt = DateTime.UtcNow;

                var allocation = new FridgeAllocation
                {
                    CustomerId = customer.Id,
                    FridgeId = rental.FridgeId,
                    Quantity = rental.Quantity,
                    AllocationDate = DateTime.UtcNow,
                    CreatedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                };
                await _context.FridgeAllocations.AddAsync(allocation);
                await _context.SaveChangesAsync();

                rental.AllocationId = allocation.Id;

                var notification = new Notification
                {
                    RecipientUserId = rental.RequestedByUserId,
                    Message = $"Your rental request for {rental.Fridge.Model} ({rental.Quantity}) has been approved.",
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };
                await _context.Notifications.AddAsync(notification);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { success = true, message = "Rental approved!", rentalId = rental.Id });
            }
            catch
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = "Failed to approve rental." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RejectRental(int id)
        {
            var rental = await _context.RentalRequests
                .Include(r => r.Fridge)
                .Include(r => r.RequestedByUser)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (rental == null) return Json(new { success = false, message = "Rental not found." });

            rental.Status = "Rejected";
            rental.UpdatedAt = DateTime.UtcNow;

            var notification = new Notification
            {
                RecipientUserId = rental.RequestedByUserId,
                Message = $"Your rental request for {rental.Fridge.Model} has been rejected.",
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };
            await _context.Notifications.AddAsync(notification);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Rental rejected!", rentalId = rental.Id });
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

        // Fridge Tracking
        public async Task<IActionResult> FridgeTracking()
        {
            var trackedFridges = await _context.FridgeAllocations
                .Include(a => a.Fridge)
                .Include(a => a.Customer)
                .Include(a => a.CreatedBy)
                .OrderByDescending(a => a.AllocationDate)
                .ToListAsync();

            return View(trackedFridges);
        }

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
    }
}
