using Code_Vanta_Systems.Data;
using Code_Vanta_Systems.ViewModels;
using Code_Vanta_Systems.Models.Enums;
using Code_Vanta_Systems.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using Code_Vanta_Systems.ViewModels.Customer;
using Code_Vanta_Systems.ViewModels.Log_Fault;

namespace Code_Vanta_Systems.Controllers
{
    public class CustomerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly UserManager<ApplicationUser> _userManager;

        public CustomerController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            var name = "test git";
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            // Load the customer record linked to this user
            var customer = await _context.Customers
                .Include(c => c.Location)
                .FirstOrDefaultAsync(c => c.UserId == user.Id);

            if (customer == null) return NotFound("Customer profile not found.");

            return View(customer);
        }


        public async Task<IActionResult> PendingPage()
        {
            var userId = _userManager.GetUserId(User); // safer way to get user Id

            var customer = await _context.Customers
        .FirstOrDefaultAsync(c => c.UserId == userId &&
                                 (c.Status == CustomerStatus.Pending || c.Status == CustomerStatus.Rejected));

            if (customer == null)
            {
                // Either the customer is active or doesn't exist, redirect to dashboard
                return RedirectToAction("Index", "Home");
            }

            return View(customer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PendingPage(IFormFile document)
        {
            if (document == null || document.Length == 0)
            {
                TempData["Error"] = "Please select a file to upload.";
                return RedirectToAction("PendingPage");
            }

            var userId = _userManager.GetUserId(User);

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.UserId == userId &&
                                         (c.Status == CustomerStatus.Pending || c.Status == CustomerStatus.Rejected));

            if (customer == null)
            {
                TempData["Error"] = "No pending or rejected customer found.";
                return RedirectToAction("PendingPage");
            }

            // ✅ Reset status if rejected
            if (customer.Status == CustomerStatus.Rejected)
            {
                customer.Status = CustomerStatus.Pending;
                TempData["Info"] = "Your account status has been reset to Pending for review.";
            }

            // 📁 Save uploaded document
            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads/business_docs");
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{customer.Id}_{Path.GetFileName(document.FileName)}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await document.CopyToAsync(fileStream);
            }

            // 📄 Update customer record
            customer.BusinessDocumentPath = $"/uploads/business_docs/{uniqueFileName}";
            _context.Customers.Update(customer);
            await _context.SaveChangesAsync();

            // ✅ 1. Get the "Customer Liaison" user(s) properly
            var liaisonUsers = await _userManager.GetUsersInRoleAsync("Customer Liaison");
            var liaisonUser = liaisonUsers.FirstOrDefault();

            if (liaisonUser != null)
            {
                // ✅ 2. Create notification
                var notification = new Notification
                {
                    RecipientUserId = liaisonUser.Id,
                    Title = "New Customer registered",
                    Message = $"Customer '{customer.BusinessName}' has uploaded a new business document for verification.",
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false,
                    TargetController = "CustomerLiaison",
                    TargetAction = "PendingCustomers"
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Your business document has been uploaded successfully! You will be notified when your account is approved or rejected.";

            return RedirectToAction("PendingPage");
        }



        public async Task<IActionResult> ActiveCustomers()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            // Load the customer record linked to this user
            var customer = await _context.Customers
                .Include(c => c.Location)
                .FirstOrDefaultAsync(c => c.UserId == user.Id);

            if (customer == null) return NotFound("Customer profile not found.");

            TempData["Success"] = "The customer has been approved";

            return View(customer);
        }
        // GET: CustomerDashboard/Rejected/5
        public async Task<IActionResult> Rejected()
        {
            var userId = _userManager.GetUserId(User);

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (customer == null)
                return NotFound();

            return View(customer);
        }



        public async Task<IActionResult> Rentals(string searchModel, int? category, int? status, int? supplier, int? location)
        {
            var fridgesQuery = _context.Fridges.AsQueryable().Where(f => !f.IsDeleted);

            if (!string.IsNullOrEmpty(searchModel))
                fridgesQuery = fridgesQuery.Where(f => f.Model.Contains(searchModel));

            if (category.HasValue)
                fridgesQuery = fridgesQuery.Where(f => (int)f.Category == category.Value);

            if (status.HasValue)
                fridgesQuery = fridgesQuery.Where(f => (int)f.Status == status.Value);

            if (supplier.HasValue)
                fridgesQuery = fridgesQuery.Where(f => f.SupplierId == supplier.Value);

            if (location.HasValue)
                fridgesQuery = fridgesQuery.Where(f => f.LocationId == location.Value);

            var model = await fridgesQuery
                .Select(f => new RentalViewModel
                {
                    Id = f.Id,
                    Model = f.Model,
                    SerialNumber = f.SerialNumber,
                    PricePerMonth = f.RentalPricePerMonth,
                    StockQuantity = f.StockQuantity,
                    Location = f.Description,
                    ImageUrl = f.ImageUrl
                })
                .ToListAsync();


            ViewBag.Categories = Enum.GetValues(typeof(FridgeCategory))
                                     .Cast<FridgeCategory>()
                                     .Select(c => new SelectListItem { Text = c.ToString(), Value = ((int)c).ToString() })
                                     .ToList();

            ViewBag.Statuses = Enum.GetValues(typeof(FridgeStatus))
                                   .Cast<FridgeStatus>()
                                   .Select(s => new SelectListItem { Text = s.ToString(), Value = ((int)s).ToString() })
                                   .ToList();

            ViewBag.Suppliers = await _context.Fridges
                                              .Select(f => f.Supplier)
                                              .Distinct()
                                              .Select(s => new SelectListItem { Text = s.Name, Value = s.Id.ToString() })
                                              .ToListAsync();

            ViewBag.Locations = await _context.Fridges
                                              .Select(f => f.Location)
                                              .Distinct()
                                              .Select(l => new SelectListItem { Text = l.Description, Value = l.Id.ToString() })
                                              .ToListAsync();

            return View(model);
        }

        // Submit rental request  
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout([FromBody] List<RentalViewModel> cartItems)
        {
            if (cartItems == null || !cartItems.Any())
                return Json(new { success = false, message = "Your cart is empty." });

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Json(new { success = false, message = "User not found." });

            foreach (var item in cartItems)
            {
                var rental = new RentalRequest
                {
                    FridgeId = item.Id,
                    Quantity = item.Quantity,
                    Status = "Pending",
                    RequestedByUserId = user.Id,
                    CreatedAt = DateTime.UtcNow
                };
                await _context.RentalRequests.AddAsync(rental);
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Rental request submitted! Waiting for approval." });
        }

        // List rental requests
        public async Task<IActionResult> MyRentalRequests()
        {
            var user = await _userManager.GetUserAsync(User);
            var rentals = await _context.RentalRequests
                .Include(r => r.Fridge)
                .Where(r => r.RequestedByUserId == user.Id)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(rentals);
        }

        // Active rented fridges  
        public async Task<IActionResult> MyRentedFridges()
        {
            var user = await _userManager.GetUserAsync(User);
            var rentals = await _context.RentalRequests
                .Include(r => r.Fridge)
                .Where(r => r.RequestedByUserId == user.Id && r.Status == "Approved")
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(rentals);
        }

        // New endpoints for the Customer Portal

        // Dashboard
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);

            var totalLoggedFaults = await _context.FridgeFaults
                .CountAsync(f => f.ReportedByUserId == user.Id);

            var totalPending = await _context.FridgeFaults
               .CountAsync(f => f.ReportedByUserId == user.Id &&
                (f.Status == FaultStatus.InProgress || f.Status == FaultStatus.Open));

            var totalFridgesAllocated = await _context.FridgeAllocations
                .CountAsync(a => a.Customer.UserId == user.Id);

            var totalPendingRequests = await _context.FridgeRequests
                .CountAsync(r => r.RequestedByUserId == user.Id && (r.Status == RequestStatus.Pending));

            // --- Faults Over Time (for chart) ---
            var faults = await _context.FridgeFaults
                .Where(f => f.ReportedByUserId == user.Id)
                .Select(f => f.DateReported)
                .ToListAsync();

            // Group faults by month for current year
            var groupedData = faults
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
                TotalAllocated = totalFridgesAllocated,
                FaultsOverTime = faultsOverTime
            };

            return View(viewModel);
        }

        // List of Logged Faults
        [HttpGet]
        public async Task<IActionResult> LoggedFault(int? pageIndex, string? search)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Unable to identify the current user.";
                    return RedirectToAction(nameof(LoggedFault));
                }

                // 2️⃣ Find the associated Customer record
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.UserId == user.Id);

                if (customer == null)
                {
                    TempData["ErrorMessage"] = "No customer profile is linked to your account.";
                    return RedirectToAction(nameof(LoggedFault));
                }

                var query = _context.FridgeFaults.Where(f => f.Status != FaultStatus.Cancelled)
                    .Include(f => f.Fridge)
                    .Include(f => f.ReportedBy)
                    .Include(f => f.AssignedTechnician)
                    .AsQueryable();

                // Search by Fridge Model, SerialNumber, or Fault Description
                if (!string.IsNullOrEmpty(search))
                {
                    search = search.ToLower();
                    query = query.Where(f =>
                        f.Fridge.Model.ToLower().Contains(search) ||
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
                    .Select(f => new FridgeFaultViewModel
                    {
                        Id = f.Id,
                        FridgeId = f.FridgeId,
                        FridgeModel = f.Fridge.Model,
                        FridgeSerialNumber = f.Fridge.SerialNumber,
                        ReportedByUserId = f.ReportedByUserId,
                        ReportedByFullName = f.ReportedBy.FullName,
                        AssignedTechnicianId = f.AssignedTechnicianId,
                        AssignedTechnicianName = f.AssignedTechnician != null ? f.AssignedTechnician.FullName : null,
                        DateReported = f.DateReported,
                        FaultDescription = f.FaultDescription,
                        Status = f.Status,
                        ResolutionDate = f.ResolutionDate
                    })
                    .ToListAsync();

                ViewData["currentPage"] = pageIndex.Value;
                ViewData["totalPages"] = totalPages;
                ViewData["search"] = search ?? "";

                // Load all fridges for dropdown
                var fridges = await _context.FridgeAllocations
                    .Include(fa => fa.Fridge)
                    .Where(fa => fa.CustomerId == customer.Id &&
                                 (fa.DeallocationDate == null || fa.DeallocationDate > DateTime.Now))
                    .Select(fa => new
                    {
                        Id = fa.Fridge.Id,
                        Name = $"{fa.Fridge.Model} - {fa.Fridge.SerialNumber}"
                    })
                    .ToListAsync();

                ViewBag.Fridges = new SelectList(fridges, "Id", "Name");

                return View(faults);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = $"Error loading logged faults";
                return RedirectToAction("LoggedFault");
            }
        }

        [HttpGet]
        public async Task<IActionResult> LogFault()
        {
            try
            {
                // 1️⃣ Get the currently logged-in user
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Unable to identify the current user.";
                    return RedirectToAction(nameof(LoggedFault));
                }

                // 2️⃣ Find the associated Customer record
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.UserId == user.Id);

                if (customer == null)
                {
                    TempData["ErrorMessage"] = "No customer profile is linked to your account.";
                    return RedirectToAction(nameof(LoggedFault));
                }

                // 3️⃣ Load only fridges allocated to this customer
                var fridges = await _context.FridgeAllocations
                    .Include(fa => fa.Fridge)
                    .Where(fa => fa.CustomerId == customer.Id &&
                                 (fa.DeallocationDate == null || fa.DeallocationDate > DateTime.Now))
                    .Select(fa => new
                    {
                        Id = fa.Fridge.Id,
                        Name = $"{fa.Fridge.Model} - {fa.Fridge.SerialNumber}"
                    })
                    .ToListAsync();

                ViewBag.Fridges = new SelectList(fridges, "Id", "Name");

                return View(new CreateFridgeFaultViewModel());
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Error loading form.";
                return RedirectToAction(nameof(LoggedFault));
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogFault(CreateFridgeFaultViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please correct the errors in the form.";
                return View(model);
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == user.Id);

                if (customer == null)
                {
                    TempData["ErrorMessage"] = "No customer profile is linked to your account.";
                    return RedirectToAction(nameof(LoggedFault));
                }

                // 1️⃣ Ensure the selected fridge belongs to this customer
                var allocation = await _context.FridgeAllocations
                    .Include(a => a.Fridge)
                    .FirstOrDefaultAsync(a => a.FridgeId == model.FridgeId && a.CustomerId == customer.Id);

                if (allocation == null)
                {
                    TempData["ErrorMessage"] = "Invalid fridge selection. You can only log faults for your allocated fridges.";
                    return RedirectToAction(nameof(LogFault));
                }

                // 2️⃣ Create the fridge fault
                var fault = new FridgeFault
                {
                    FridgeId = model.FridgeId,
                    ReportedByUserId = user.Id,
                    DateReported = DateTime.Now,
                    FaultDescription = model.FaultDescription,
                    Status = FaultStatus.Open
                };

                _context.FridgeFaults.Add(fault);
                await _context.SaveChangesAsync();

                // 3️⃣ Audit trail
                var systemAudit = new AuditTrail
                {
                    ChangeType = "Create",
                    EntityName = "FridgeFault",
                    EntityId = fault.Id,
                    ChangedByUserId = user.Id,
                    ChangeDate = DateTime.Now,
                    ChangeDetails = $"Fridge fault logged with ID {fault.Id} by user {user.FullName}."
                };
                _context.AuditTrails.Add(systemAudit);
                await _context.SaveChangesAsync();

                // 4️⃣ Notify all admins
                var admins = await _userManager.GetUsersInRoleAsync("Admin");
                foreach (var admin in admins)
                {
                    var notification = new Notification
                    {
                        RecipientUserId = admin.Id,
                        Title = "New Fridge Fault Reported",
                        Message = $"User '{user.FullName}' reported a new fridge fault (ID {fault.Id}).",
                        CreatedAt = DateTime.Now,
                        IsRead = false,
                        TargetController = "AdminFaults",
                        TargetAction = "Index"
                    };
                    _context.Notifications.Add(notification);
                }
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Fridge fault logged successfully!";
                return RedirectToAction(nameof(LoggedFault));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                TempData["ErrorMessage"] = "Error logging fridge fault. Please try again.";
                return View(model);
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditFault(CustomerEditFridgeFaultViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please correct the errors in the form.";
                return RedirectToAction(nameof(LoggedFault));
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);
                var fault = await _context.FridgeFaults.FindAsync(model.Id);

                if (fault == null)
                {
                    TempData["ErrorMessage"] = "Fridge fault not found.";
                    return RedirectToAction(nameof(LoggedFault));
                }

                // Update only allowed fields
                fault.FridgeId = model.FridgeId;
                fault.FaultDescription = model.FaultDescription;

                await _context.SaveChangesAsync();

                // Audit trail
                var audit = new AuditTrail
                {
                    EntityName = "FridgeFault",
                    EntityId = fault.Id,
                    ChangedByUserId = user.Id,
                    ChangeType = "Update",
                    ChangeDate = DateTime.Now,
                    ChangeDetails = $"Customer '{user.FullName}' updated Fridge Fault ID {fault.Id}."
                };
                _context.AuditTrails.Add(audit);
                await _context.SaveChangesAsync();

                // Notification to Admin
                var admin = await _userManager.GetUsersInRoleAsync("Admin");
                foreach (var a in admin)
                {
                    _context.Notifications.Add(new Notification
                    {
                        RecipientUserId = a.Id,
                        Title = "Fridge Fault Updated",
                        Message = $"Fridge Fault ID {fault.Id} was updated by Customer '{user.FullName}'.",
                        CreatedAt = DateTime.UtcNow,
                        IsRead = false,
                        TargetController = "AdminFaults",
                        TargetAction = "LoggedFault"
                    });
                }
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Fridge fault updated successfully!";
            }
            catch
            {
                TempData["ErrorMessage"] = "Error updating fridge fault.";
            }

            return RedirectToAction(nameof(LoggedFault));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var fault = await _context.FridgeFaults.FindAsync(id);

                if (fault == null)
                {
                    TempData["ErrorMessage"] = "Fridge fault not found.";
                    return RedirectToAction(nameof(LoggedFault));
                }

                // Soft delete by setting status to Cancelled
                fault.Status = FaultStatus.Cancelled;

                // Update fault
                _context.FridgeFaults.Update(fault);

                // Create audit trail
                var audit = new AuditTrail
                {
                    EntityName = "FridgeFault",
                    EntityId = fault.Id,
                    ChangedByUserId = user.Id,
                    ChangeType = "Delete",
                    ChangeDate = DateTime.Now,
                    ChangeDetails = $"Fridge fault ID {fault.Id} was cancelled by Customer '{user.FullName}'."
                };
                _context.AuditTrails.Add(audit);

                // Notify all admins
                var admins = await _userManager.GetUsersInRoleAsync("Admin");
                foreach (var admin in admins)
                {
                    _context.Notifications.Add(new Notification
                    {
                        RecipientUserId = admin.Id,
                        Title = "Fridge Fault Cancelled",
                        Message = $"Fridge fault ID {fault.Id} was cancelled by Customer '{user.FullName}'.",
                        CreatedAt = DateTime.UtcNow,
                        IsRead = false,
                        TargetController = "AdminFaults",
                        TargetAction = "LoggedFault"
                    });
                }

                // Save all changes in one transaction
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Fridge fault has been cancelled successfully!";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Error cancelling fridge fault.";
            }

            return RedirectToAction(nameof(LoggedFault));
        }
    }
}
