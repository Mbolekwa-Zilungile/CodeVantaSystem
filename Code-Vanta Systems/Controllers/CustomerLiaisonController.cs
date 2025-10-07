using Code_Vanta_Systems.Data;
using Code_Vanta_Systems.Models;
using Code_Vanta_Systems.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Code_Vanta_Systems.Controllers
{
    [Authorize(Roles = "Customer Liaison")]
    public class CustomerLiaisonController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CustomerLiaisonController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

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
            {
                return NotFound("Customer not found or not pending.");
            }

            // Change status to Active
            customer.Status = CustomerStatus.Active;
            _context.Customers.Update(customer);

            // Send notification to the customer
            if (customer.User != null)
            {
                _context.Notifications.Add(new Notification
                {
                    RecipientUserId = customer.User.Id,
                    Message = $"Congratulations! Your account has been approved and is now active.",
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Customer '{customer.BusinessName}' has been approved and notified.";
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
            {
                return NotFound("Customer not found or not pending.");
            }

            // Change status to Rejected
            customer.Status = CustomerStatus.Rejected;
            _context.Customers.Update(customer);

            // Send notification
            if (customer.User != null)
            {
                _context.Notifications.Add(new Notification
                {
                    RecipientUserId = customer.User.Id,
                    Message = $"Sorry! Your account has been Rejected",
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Customer '{customer.BusinessName}' has been rejected and notified.";
            return RedirectToAction("RejectCustomers");
        }
        //Esethu 

        // GET: Allocate Fridge to Customer
        public async Task<IActionResult> AllocateFridge(int customerId)
        {
            var customer = await _context.Customers
                .Include(c => c.Location)
                .FirstOrDefaultAsync(c => c.Id == customerId);

            if (customer == null)
                return NotFound("Customer not found.");

            // Get all available fridges
            var availableFridges = await _context.Fridges
                .Where(f => f.Status == FridgeStatus.Available)
                .ToListAsync();

            ViewBag.Fridges = availableFridges;
            ViewBag.CustomerName = customer.BusinessName;
            return View(new FridgeAllocation { CustomerId = customerId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AllocateFridge(FridgeAllocation allocation)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Fridges = await _context.Fridges
                    .Where(f => f.Status == FridgeStatus.Available)
                    .ToListAsync();
                return View(allocation);
            }

            var fridge = await _context.Fridges.FindAsync(allocation.FridgeId);
            var customer = await _context.Customers.FindAsync(allocation.CustomerId);

            if (fridge == null || customer == null)
                return NotFound("Invalid fridge or customer.");

            if (fridge.Status != FridgeStatus.Available)
            {
                TempData["Error"] = "This fridge is no longer available.";
                return RedirectToAction("ActiveCustomers");
            }

            // Create allocation record
            allocation.AllocationDate = DateTime.UtcNow;
            _context.FridgeAllocations.Add(allocation);

            // Update fridge status
            fridge.Status = FridgeStatus.Allocated;
            _context.Fridges.Update(fridge);

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Fridge '{fridge.SerialNumber}' successfully allocated to {customer.BusinessName}.";
            return RedirectToAction("ViewCustomer", new { id = allocation.CustomerId });
        }


    }
}
