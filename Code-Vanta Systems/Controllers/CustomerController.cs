using Code_Vanta_Systems.Data;
using Code_Vanta_Systems.Models;
using Code_Vanta_Systems.Models.Enums;
using Code_Vanta_Systems.ViewModels;
using Code_Vanta_Systems.ViewModels.Customer;
using Code_Vanta_Systems.ViewModels.Log_Fault;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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
        #region Index

        public async Task<IActionResult> Index(string searchModel, int? category, int? status, int? supplier, int? location)
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
        #endregion


        #region Pendig Page
        public async Task<IActionResult> PendingPage()
        {
            var userId = _userManager.GetUserId(User);

            var customer = await _context.Customers
        .FirstOrDefaultAsync(c => c.UserId == userId &&
                                 (c.Status == CustomerStatus.Pending || c.Status == CustomerStatus.Rejected));

            if (customer == null)
            {

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
        #endregion


        #region Active Customers
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
        #endregion


        #region Rejected Customers
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
        #endregion

        #region My Profile
        public async Task<IActionResult> MyProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            // Load customer
            var customer = await _context.Customers
                .Include(c => c.Location)
                .FirstOrDefaultAsync(c => c.UserId == user.Id);

            if (customer == null) return NotFound("Customer profile not found.");

            // Compute stats from RentalRequests
            var rentalsCount = await _context.RentalRequests
                .CountAsync(r => r.RequestedByUserId == user.Id);

            var pendingOrders = await _context.RentalRequests
                .CountAsync(r => r.RequestedByUserId == user.Id && r.Status == "Pending");

            var rejectedOrders = await _context.RentalRequests
                .CountAsync(r => r.RequestedByUserId == user.Id && r.Status == "Rejected");

            var approvedOrders = await _context.RentalRequests
                .CountAsync(r => r.RequestedByUserId == user.Id && r.Status == "Approved");

            var paidOrders = await _context.RentalRequests
                .CountAsync(r => r.RequestedByUserId == user.Id && r.IsPaid == true);

            var notPaidOrders = await _context.RentalRequests
                .CountAsync(r => r.RequestedByUserId == user.Id && r.IsPaid == false);


            // Map to ViewModel
            var model = new ProfileViewModel
            {
                BusinessName = customer.BusinessName,
                BusinessType = customer.BusinessType,
                ContactName = customer.ContactName,
                Phone = customer.Phone,
                Email = customer.Email,
                PhysicalAddress = customer.PhysicalAddress,
                Location = customer.Location,
                RentalsCount = rentalsCount,
                PendingOrders = pendingOrders,
                PaidOrders = paidOrders,
                UnPaidOrders = notPaidOrders,
                RejectedOrders = rejectedOrders,
                ApprovedOrders = approvedOrders
            };

            return View(model);
        }
        #endregion

        #region Rentals
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

            // Rentals (Approved & Paid)
            var rentalFridges = await _context.RentalRequests
                .Include(r => r.Fridge)
                .Where(r => r.RequestedByUserId == user.Id && r.Status == "Approved" && r.IsPaid)
                .Select(r => new FridgeAllocationDisplayViewModel
                {
                    FridgeModel = r.Fridge.Model,
                    ImageUrl = r.Fridge.ImageUrl,
                    Quantity = r.Quantity,
                    AllocatedOn = r.UpdatedAt ?? r.CreatedAt,
                    Source = "Rental",
                    RentalId = r.Id
                })
                .ToListAsync();

            // Manual allocations
            var manualAllocations = await _context.FridgeAllocations
                .Include(a => a.Fridge)
                .Include(a => a.Customer)
                .Where(a => a.Customer.UserId == user.Id)
                .Select(a => new FridgeAllocationDisplayViewModel
                {
                    FridgeModel = a.Fridge.Model,
                    ImageUrl = a.Fridge.ImageUrl,
                    Quantity = a.Quantity,
                    AllocatedOn = a.AllocationDate,
                    Source = "Manual",
                    RentalId = null
                })
                .ToListAsync();

            // ✅ Merge both safely
            var combined = rentalFridges.Concat(manualAllocations)
                .OrderByDescending(x => x.AllocatedOn)
                .ToList();

            return View(combined);
        }

        #endregion

        // New endpoints for the Customer Portal
        #region Fault 

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

           // ---Build ViewModel-- -
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

                // 1️⃣ MODIFICATION: Filter faults to only show those reported by the current user
                var query = _context.FridgeFaults.Where(f => f.ReportedByUserId == user.Id && f.Status != FaultStatus.Cancelled)
                    .Include(f => f.Fridge)
                    // 🚨 IMPORTANT: Include FridgeUnit if you update the model to link faults to units.
                    // .Include(f => f.FridgeUnit) 
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
                        // 🚨 FRIDGE UNIT FIX: Use the unit's serial if linked via FridgeUnitId
                        // FridgeSerialNumber = f.FridgeUnit.SerialNumber,
                        FridgeSerialNumber = f.Fridge.SerialNumber, // Keeping old logic for now
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

                // 2️⃣ FIX: Load specific FRIDGE UNITS allocated to this customer for the dropdown
                var uniqueUnits = await _context.FridgeUnits
                    .Include(u => u.Fridge)
                    // 🚨 ASSUMPTION: FridgeUnit model has a CustomerId FK field
                    .Where(u => u.CustomerId == customer.Id)
                    .Select(u => new
                    {
                        Id = u.Id, // The Unit's unique ID
                        Name = $"{u.Fridge.Model} - {u.SerialNumber}" // The Unit's unique serial
                    })
                    .ToListAsync();

                // The SelectList must use the unique FridgeUnit ID
                ViewBag.Fridges = new SelectList(uniqueUnits, "Id", "Name");

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

                // 3️⃣ FIX: Load specific FRIDGE UNITS allocated to this customer
                var uniqueUnits = await _context.FridgeUnits
                    .Include(u => u.Fridge)
                    // 🚨 ASSUMPTION: FridgeUnit model has a CustomerId FK field
                    .Where(u => u.CustomerId == customer.Id)
                    .Select(u => new
                    {
                        Id = u.Id, // The Unit's unique ID
                        Name = $"{u.Fridge.Model} - {u.SerialNumber}" // The Unit's unique serial
                    })
                    .ToListAsync();

                // The SelectList must use the unique FridgeUnit ID
                ViewBag.Fridges = new SelectList(uniqueUnits, "Id", "Name");

                // Note: The view model must be updated to use FridgeUnitId
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
            // The model property containing the FridgeUnit.Id should ideally be renamed to FridgeUnitId.
            // For this fix, we assume the incoming unit ID is stored in model.FridgeId.

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please correct the errors in the form.";

                // ⚠️ Reload dropdowns before returning the view on failure (Keep this block)
                var userOnFail = await _userManager.GetUserAsync(User);
                var customerOnFail = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userOnFail.Id);
                if (customerOnFail != null)
                {
                    var uniqueUnitsOnFail = await _context.FridgeUnits
                        .Include(u => u.Fridge)
                        .Where(u => u.CustomerId == customerOnFail.Id)
                        .Select(u => new { Id = u.Id, Name = $"{u.Fridge.Model} - {u.SerialNumber}" })
                        .ToListAsync();
                    ViewBag.Fridges = new SelectList(uniqueUnitsOnFail, "Id", "Name");
                }
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

                // 1️⃣ Find the selected FRIDGE UNIT using the ID from the form (model.FridgeId)
                var allocatedUnit = await _context.FridgeUnits
                    .Include(u => u.Fridge)
                    // model.FridgeId now holds the FridgeUnit.Id
                    .FirstOrDefaultAsync(u => u.Id == model.FridgeId && u.CustomerId == customer.Id);

                if (allocatedUnit == null)
                {
                    TempData["ErrorMessage"] = "Invalid fridge unit selection. You can only log faults for your allocated units.";
                    return RedirectToAction(nameof(LogFault));
                }

                // 2️⃣ Create the fridge fault
                var fault = new FridgeFault
                {
                    // This is the ID of the general fridge Model/Type (required for old reports)
                    FridgeId = allocatedUnit.FridgeId,

                    // ✅ CRITICAL FIX: Set the required FridgeUnitId field
                    // The value is the ID of the specific unit selected in the dropdown.
                    FridgeUnitId = allocatedUnit.Id, // <-- Pass the FridgeUnit ID here

                    ReportedByUserId = user.Id,
                    DateReported = DateTime.Now,
                    FaultDescription = model.FaultDescription,
                    Status = FaultStatus.Open
                };

                _context.FridgeFaults.Add(fault);
                await _context.SaveChangesAsync(); // This should now succeed

                // 3️⃣ Audit trail (Retained logic)
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

                // 4️⃣ Notify all admins (Retained logic)
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

                // ⚠️ Reload dropdowns before returning the view on failure (Keep this block)
                var userOnFail = await _userManager.GetUserAsync(User);
                var customerOnFail = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userOnFail.Id);
                if (customerOnFail != null)
                {
                    var uniqueUnitsOnFail = await _context.FridgeUnits
                        .Include(u => u.Fridge)
                        .Where(u => u.CustomerId == customerOnFail.Id)
                        .Select(u => new { Id = u.Id, Name = $"{u.Fridge.Model} - {u.SerialNumber}" })
                        .ToListAsync();
                    ViewBag.Fridges = new SelectList(uniqueUnitsOnFail, "Id", "Name");
                }
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
#endregion

        #region Checkout Logic
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout([FromBody] List<RentalViewModel> cartItems)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Json(new { success = false, message = "User not found." });

            if (cartItems == null || !cartItems.Any())
                return Json(new { success = false, message = "Your cart is empty." });

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == user.Id);
            if (customer == null)
                return Json(new { success = false, message = "Customer profile not found." });

            if (customer.Status != CustomerStatus.Active)
                return Json(new { success = false, message = "Your account is not verified. Please wait for verification before making rental requests." });


            var rentalsToSave = cartItems.Select(item => new RentalRequest
            {
                FridgeId = item.Id,
                Quantity = item.Quantity,
                Status = "Pending",
                RequestedByUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
                IsPaid = false
            }).ToList();

            _context.RentalRequests.AddRange(rentalsToSave);
            await _context.SaveChangesAsync();


            var rentalIds = rentalsToSave.Select(r => r.Id).ToList();

            var redirectUrl = Url.Action("PayLater", new { rentalIds = string.Join(",", rentalIds) });

            return Json(new
            {
                success = true,
                message = "Rental request submitted! You can pay later if you want.",
                redirectUrl
            });
        }
        #endregion

        #region Mock Payment Page
        [HttpGet]
        public IActionResult Payment(string rentalIds)
        {
            if (string.IsNullOrEmpty(rentalIds))
                return RedirectToAction("Rentals");

            ViewBag.RentalIds = rentalIds;
            return View();
        }
        #endregion

        #region Confirm Payment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPayment(string rentalIds)
        {
            if (string.IsNullOrEmpty(rentalIds))
                return RedirectToAction("Rentals");

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Rentals");

            var ids = rentalIds.Split(',')
                               .Where(id => int.TryParse(id, out _))
                               .Select(int.Parse)
                               .ToList();

            var rentals = await _context.RentalRequests
                .Where(r => ids.Contains(r.Id) && r.RequestedByUserId == user.Id)
                .ToListAsync();

            if (!rentals.Any())
                return RedirectToAction("Rentals");

            foreach (var rental in rentals)
            {

                if (!rental.IsPaid)
                {
                    rental.IsPaid = true;
                    rental.Status = "Awaiting Approval";
                    rental.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("PaymentSuccess");
        }
        #endregion

        #region Payment Success
        public IActionResult PaymentSuccess(string rentalIds = "")
        {
            ViewBag.RentalIds = rentalIds.Split(',')
                                          .Where(id => int.TryParse(id, out _))
                                          .Select(int.Parse)
                                          .ToList();
            return View();
        }
        #endregion

        #region Pay Later
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayLater([FromBody] dynamic data)
        {
            if (data == null || data.rentalId == null)
                return Json(new { success = false, message = "Invalid request." });

            int rentalId = (int)data.rentalId;

            var rental = await _context.RentalRequests.FirstOrDefaultAsync(r => r.Id == rentalId);
            if (rental == null)
                return Json(new { success = false, message = "Rental not found." });

            if (rental.IsPaid)
                return Json(new { success = false, message = "This rental is already paid." });

            rental.Status = "Pay Later";
            rental.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Rental marked as Pay Later." });
        }
        #endregion


        #region Pay Later Page

        [HttpGet]
        public async Task<IActionResult> PayLater(string rentalIds)
        {
            if (string.IsNullOrEmpty(rentalIds))
                return RedirectToAction("Rentals");

            var ids = rentalIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                               .Select(id => int.TryParse(id.Trim(), out var val) ? val : (int?)null)
                               .Where(id => id.HasValue)
                               .Select(id => id.Value)
                               .ToList();

            var rentals = await _context.RentalRequests
                .Include(r => r.Fridge)
                .Where(r => ids.Contains(r.Id) && !r.IsPaid)
                .ToListAsync();

            if (!rentals.Any())
                return RedirectToAction("Rentals");

            return View(rentals);
        }
        #endregion

        #region Confirm Pay Later 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPayLater(string rentalIds)
        {
            if (string.IsNullOrEmpty(rentalIds))
                return RedirectToAction("Rentals");

            var ids = rentalIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                               .Select(id => int.TryParse(id.Trim(), out var val) ? val : (int?)null)
                               .Where(id => id.HasValue)
                               .Select(id => id.Value)
                               .ToList();

            if (!ids.Any())
                return RedirectToAction("Rentals");

            var rentals = await _context.RentalRequests
                .Where(r => ids.Contains(r.Id))
                .ToListAsync();

            foreach (var rental in rentals)
            {
                rental.IsPaid = false;
                rental.Status = "Pay Later Requested";
                rental.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Rentals are now marked as 'Pay Later'. You will be notified once they are approved.";
            return RedirectToAction("MyRentals");
        }
        #endregion

        #region Pay Single Rental
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayRental(int id)
        {
            var rental = await _context.RentalRequests.FindAsync(id);
            if (rental == null)
                return Json(new { success = false, message = "Rental not found." });

            if (rental.IsPaid)
                return Json(new { success = false, message = "Rental is already paid." });

            rental.IsPaid = true;
            rental.Status = "Awaiting Approval";
            rental.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Payment successful!" });
        }
        #endregion


        #region Fridge more info
        public async Task<IActionResult> FridgeMoreInfo(int id)
        {
            var fridge = await _context.Fridges
                .Include(f => f.Supplier)
                .Include(f => f.Location)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (fridge == null)
                return NotFound();

            return View("FridgeMoreInfo", fridge);
        }
        #endregion

        #region Edit Profile
        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var customer = await _context.Customers
                .Include(c => c.Location)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (customer == null)
            {
                TempData["ErrorMessage"] = "Customer profile not found.";
                return RedirectToAction("MyProfile");
            }

            var locations = await _context.Locations
                .Select(l => new SelectListItem
                {
                    Value = l.Id.ToString(),
                    Text = $"{l.Description} ({l.City}, {l.Province})"
                })
                .ToListAsync();

            var model = new UpdateCustomerViewModel
            {
                Id = customer.Id,
                BusinessName = customer.BusinessName,
                ContactName = customer.ContactName,
                Phone = customer.Phone,
                Email = customer.Email,
                BusinessType = customer.BusinessType,
                PhysicalAddress = customer.PhysicalAddress,
                LocationId = customer.LocationId,
                AvailableLocations = locations
            };

            ///ViewData["Title"] = "Edit My Profile";
            return View(model);
        }

        // POST: Customer/EditProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(UpdateCustomerViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var customer = await _context.Customers.Include(c => c.User)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (customer == null)
            {
                TempData["ErrorMessage"] = "Unable to find your profile.";
                return RedirectToAction("MyProfile");
            }

            if (!ModelState.IsValid)
            {
                model.AvailableLocations = await _context.Locations
                    .Select(l => new SelectListItem
                    {
                        Value = l.Id.ToString(),
                        Text = $"{l.Description} ({l.City}, {l.Province})"
                    })
                    .ToListAsync();

                return View(model);
            }

            // ✅ Update profile fields
            customer.BusinessName = model.BusinessName;
            customer.ContactName = model.ContactName;
            customer.Phone = model.Phone;
            customer.Email = model.Email;
            customer.BusinessType = model.BusinessType;
            customer.PhysicalAddress = model.PhysicalAddress;
            customer.LocationId = model.LocationId;

            // ✅ Update linked ApplicationUser
            if (customer.User != null)
            {
                customer.User.Email = model.Email;
                customer.User.UserName = model.Email;
                customer.User.FullName = model.ContactName;
                _context.Users.Update(customer.User);
            }

            _context.Customers.Update(customer);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Your profile has been updated successfully!";
            return RedirectToAction("MyProfile");
        }

        #endregion

    }
}
