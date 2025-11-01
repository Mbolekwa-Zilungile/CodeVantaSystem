using Code_Vanta_Systems.Data;
using Code_Vanta_Systems.Models;
using Code_Vanta_Systems.Models.Enums;
using Code_Vanta_Systems.ViewModels;
using Code_Vanta_Systems.ViewModels.Customer;
using Code_Vanta_Systems.ViewModels.Log_Fault;
using Code_Vanta_Systems.ViewModels.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Code_Vanta_Systems.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CustomerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly UserManager<ApplicationUser> _userManager;

        public CustomerController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }

        #region Index - Browse Available Fridge Models
        public async Task<IActionResult> Index(string searchModel, int? category, int? supplier, int? location)
        {
            try
            {
                var fridgeModelsQuery = _context.FridgeModels
                    .Include(fm => fm.Supplier)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(searchModel))
                    fridgeModelsQuery = fridgeModelsQuery.Where(fm => fm.Name.Contains(searchModel));

                if (category.HasValue)
                {
                    var categoryEnum = (FridgeCategory)category.Value;
                    fridgeModelsQuery = fridgeModelsQuery.Where(fm => fm.Category == categoryEnum);
                }

                if (supplier.HasValue)
                    fridgeModelsQuery = fridgeModelsQuery.Where(fm => fm.SupplierId == supplier.Value);

                var model = await fridgeModelsQuery
                    .Select(fm => new FridgeCatalogViewModel
                    {
                        Id = fm.Id,
                        Name = fm.Name,
                        Category = fm.Category,
                        PricePerMonth = fm.RentalPricePerMonth,
                        Description = fm.Description,
                        ImageUrl = fm.ImageUrl,
                        SupplierName = fm.Supplier.Name,
                        AvailableUnits = _context.FridgeUnits.Count(fu => fu.FridgeModelId == fm.Id && fu.Status == FridgeStatus.InWarehouse)
                    })
                    .Where(fm => fm.AvailableUnits > 0)
                    .ToListAsync();

                ViewBag.Categories = Enum.GetValues(typeof(FridgeCategory))
                                         .Cast<FridgeCategory>()
                                         .Select(c => new SelectListItem { Text = c.ToString(), Value = ((int)c).ToString() })
                                         .ToList();

                ViewBag.Suppliers = await _context.Suppliers
                                                  .Select(s => new SelectListItem { Text = s.Name, Value = s.Id.ToString() })
                                                  .ToListAsync();

                ViewBag.Locations = await _context.Locations
                                                  .Select(l => new SelectListItem { Text = l.Description, Value = l.Id.ToString() })
                                                  .ToListAsync();

                return View(model);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Error loading available fridge models";
                return RedirectToAction("Dashboard");
            }
        }
        #endregion

        #region Dashboard
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == user.Id);

                if (customer == null)
                {
                    TempData["ErrorMessage"] = "Customer profile not found.";
                    return RedirectToAction("Index", "Home");
                }

                var totalLoggedFaults = await _context.FridgeFaults
                    .CountAsync(f => f.ReportedByUserId == user.Id);

                var totalPendingFaults = await _context.FridgeFaults
                   .CountAsync(f => f.ReportedByUserId == user.Id &&
                    (f.Status == FaultStatus.InProgress || f.Status == FaultStatus.Open));

                var totalFridgesAllocated = await _context.FridgeAllocations
                    .CountAsync(a => a.CustomerId == customer.Id && a.DeallocationDate == null);

                var totalPendingRequests = await _context.RentalRequests
                    .CountAsync(r => r.CustomerId == customer.Id && r.Status == "Pending");

                // Rentals stats
                var rentalsCount = await _context.RentalRequests
                    .CountAsync(r => r.CustomerId == customer.Id);

                var pendingOrders = await _context.RentalRequests
                    .CountAsync(r => r.CustomerId == customer.Id && r.Status == "Pending");

                var rejectedOrders = await _context.RentalRequests
                    .CountAsync(r => r.CustomerId == customer.Id && r.Status == "Rejected");

                var approvedOrders = await _context.RentalRequests
                    .CountAsync(r => r.CustomerId == customer.Id && r.Status == "Approved");

                var paidOrders = await _context.RentalRequests
                    .CountAsync(r => r.CustomerId == customer.Id && r.IsPaid == true);

                var notPaidOrders = await _context.RentalRequests
                    .CountAsync(r => r.CustomerId == customer.Id && r.IsPaid == false);

                // Faults Over Time (for chart)
                var faults = await _context.FridgeFaults
                    .Where(f => f.ReportedByUserId == user.Id)
                    .Select(f => f.DateReported)
                    .ToListAsync();

                var groupedData = faults
                    .Where(d => d.Year == DateTime.Now.Year)
                    .GroupBy(d => d.Month)
                    .Select(g => new
                    {
                        Month = g.Key,
                        Count = g.Count()
                    })
                    .ToList();

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

                var viewModel = new CustomerDashboardViewModel
                {
                    TotalLogged = totalLoggedFaults,
                    TotalRequested = totalPendingRequests,
                    TotalPending = totalPendingFaults,
                    TotalAllocated = totalFridgesAllocated,
                    RentalsCount = rentalsCount,
                    PendingOrders = pendingOrders,
                    PaidOrders = paidOrders,
                    UnPaidOrders = notPaidOrders,
                    RejectedOrders = rejectedOrders,
                    ApprovedOrders = approvedOrders,
                    FaultsOverTime = faultsOverTime
                };

                return View(viewModel);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Error loading dashboard";
                return RedirectToAction("Index");
            }
        }
        #endregion

        #region Customer Status Pages

        public async Task<IActionResult> PendingPage()
        {
            try
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
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Error loading pending page";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PendingPage(IFormFile document)
        {
            if (document == null || document.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select a file to upload.";
                return RedirectToAction("PendingPage");
            }

            try
            {
                var userId = _userManager.GetUserId(User);

                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.UserId == userId &&
                                             (c.Status == CustomerStatus.Pending || c.Status == CustomerStatus.Rejected));

                if (customer == null)
                {
                    TempData["ErrorMessage"] = "No pending or rejected customer found.";
                    return RedirectToAction("PendingPage");
                }

                // Reset status if rejected
                if (customer.Status == CustomerStatus.Rejected)
                {
                    customer.Status = CustomerStatus.Pending;
                    TempData["InfoMessage"] = "Your account status has been reset to Pending for review.";
                }

                // Save uploaded document
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads/business_docs");
                Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = $"{customer.Id}_{Path.GetFileName(document.FileName)}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await document.CopyToAsync(fileStream);
                }

                // Update customer record
                customer.BusinessDocumentPath = $"/uploads/business_docs/{uniqueFileName}";
                _context.Customers.Update(customer);
                await _context.SaveChangesAsync();

                // Notify Customer Liaison
                var liaisonUsers = await _userManager.GetUsersInRoleAsync("Customer Liaison");
                var liaisonUser = liaisonUsers.FirstOrDefault();

                if (liaisonUser != null)
                {
                    var notification = new Notification
                    {
                        RecipientUserId = liaisonUser.Id,
                        Title = "New Customer Document Uploaded",
                        Message = $"Customer '{customer.BusinessName}' has uploaded a new business document for verification.",
                        CreatedAt = DateTime.UtcNow,
                        IsRead = false,
                        TargetController = "CustomerLiaison",
                        TargetAction = "PendingCustomers"
                    };

                    _context.Notifications.Add(notification);
                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = "Your business document has been uploaded successfully! You will be notified when your account is approved.";
                return RedirectToAction("PendingPage");
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Error uploading document";
                return RedirectToAction("PendingPage");
            }
        }

        public async Task<IActionResult> ActiveCustomers()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return RedirectToAction("Login", "Account");

                var customer = await _context.Customers
                    .Include(c => c.Location)
                    .FirstOrDefaultAsync(c => c.UserId == user.Id);

                if (customer == null) return NotFound("Customer profile not found.");

                TempData["SuccessMessage"] = "Your account has been approved!";
                return View(customer);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Error loading customer profile";
                return RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> Rejected()
        {
            try
            {
                var userId = _userManager.GetUserId(User);

                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (customer == null)
                    return NotFound();

                return View(customer);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Error loading rejected page";
                return RedirectToAction("Index");
            }
        }

        #endregion

        #region Profile Management

        public async Task<IActionResult> MyProfile()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return RedirectToAction("Login", "Account");

                var customer = await _context.Customers
                    .Include(c => c.Location)
                    .FirstOrDefaultAsync(c => c.UserId == user.Id);

                if (customer == null) return NotFound("Customer profile not found.");

                // Compute stats
                var rentalsCount = await _context.RentalRequests
                    .CountAsync(r => r.CustomerId == customer.Id);

                var pendingOrders = await _context.RentalRequests
                    .CountAsync(r => r.CustomerId == customer.Id && r.Status == "Pending");

                var rejectedOrders = await _context.RentalRequests
                    .CountAsync(r => r.CustomerId == customer.Id && r.Status == "Rejected");

                var approvedOrders = await _context.RentalRequests
                    .CountAsync(r => r.CustomerId == customer.Id && r.Status == "Approved");

                var paidOrders = await _context.RentalRequests
                    .CountAsync(r => r.CustomerId == customer.Id && r.IsPaid == true);

                var notPaidOrders = await _context.RentalRequests
                    .CountAsync(r => r.CustomerId == customer.Id && r.IsPaid == false);

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
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Error loading profile";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            try
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

                return View(model);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Error loading edit form";
                return RedirectToAction("MyProfile");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(UpdateCustomerViewModel model)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var customer = await _context.Customers
                    .Include(c => c.User)
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

                // Update profile fields
                customer.BusinessName = model.BusinessName;
                customer.ContactName = model.ContactName;
                customer.Phone = model.Phone;
                customer.Email = model.Email;
                customer.BusinessType = model.BusinessType;
                customer.PhysicalAddress = model.PhysicalAddress;
                customer.LocationId = model.LocationId;

                // Update linked ApplicationUser
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
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Error updating profile";
                return RedirectToAction("MyProfile");
            }
        }

        #endregion

        #region Rentals & Fridge Models

        public async Task<IActionResult> Rentals(string searchModel, int? category, int? supplier, int? location)
        {
            try
            {
                var fridgeModelsQuery = _context.FridgeModels
                    .Include(fm => fm.Supplier)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(searchModel))
                    fridgeModelsQuery = fridgeModelsQuery.Where(fm => fm.Name.Contains(searchModel));

                if (category.HasValue)
                {
                    var categoryEnum = (FridgeCategory)category.Value;
                    fridgeModelsQuery = fridgeModelsQuery.Where(fm => fm.Category == categoryEnum);
                }

                if (supplier.HasValue)
                    fridgeModelsQuery = fridgeModelsQuery.Where(fm => fm.SupplierId == supplier.Value);

                var model = await fridgeModelsQuery
                    .Select(fm => new FridgeCatalogViewModel
                    {
                        Id = fm.Id,
                        Name = fm.Name,
                        Category = fm.Category,
                        PricePerMonth = fm.RentalPricePerMonth,
                        Description = fm.Description,
                        ImageUrl = fm.ImageUrl,
                        SupplierName = fm.Supplier.Name,
                        AvailableUnits = _context.FridgeUnits.Count(fu => fu.FridgeModelId == fm.Id && fu.Status == FridgeStatus.InWarehouse)
                    })
                    .Where(fm => fm.AvailableUnits > 0)
                    .ToListAsync();

                ViewBag.Categories = Enum.GetValues(typeof(FridgeCategory))
                                         .Cast<FridgeCategory>()
                                         .Select(c => new SelectListItem { Text = c.ToString(), Value = ((int)c).ToString() })
                                         .ToList();

                ViewBag.Suppliers = await _context.Suppliers
                                                  .Select(s => new SelectListItem { Text = s.Name, Value = s.Id.ToString() })
                                                  .ToListAsync();

                ViewBag.Locations = await _context.Locations
                                                  .Select(l => new SelectListItem { Text = l.Description, Value = l.Id.ToString() })
                                                  .ToListAsync();

                return View(model);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Error loading rentals";
                return RedirectToAction("Dashboard");
            }
        }

        public async Task<IActionResult> MyRentalRequests()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == user.Id);

                if (customer == null)
                {
                    TempData["ErrorMessage"] = "Customer profile not found.";
                    return RedirectToAction("Dashboard");
                }

                var rentals = await _context.RentalRequests
                    .Include(r => r.FridgeModel)
                    .Where(r => r.CustomerId == customer.Id)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();

                return View(rentals);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Error loading rental requests";
                return RedirectToAction("Dashboard");
            }
        }

        public async Task<IActionResult> MyRentedFridges()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == user.Id);

                if (customer == null)
                {
                    TempData["ErrorMessage"] = "Customer profile not found.";
                    return RedirectToAction("Dashboard");
                }

                // Get allocations with fridge units and their models
                var allocations = await _context.FridgeAllocations
                    .Include(a => a.FridgeUnit)
                    .ThenInclude(fu => fu.FridgeModel)
                    .Where(a => a.CustomerId == customer.Id && a.DeallocationDate == null)
                    .Select(a => new CustomerFridgeAllocationViewModel
                    {
                        FridgeModel = a.FridgeUnit.FridgeModel.Name,
                        SerialNumber = a.FridgeUnit.SerialNumber,
                        ImageUrl = a.FridgeUnit.FridgeModel.ImageUrl,
                        Quantity = 1, // Each allocation is for one unit
                        AllocatedOn = a.AllocationDate,
                        Source = "Allocation",
                        Status = a.FridgeUnit.Status.ToString()
                    })
                    .ToListAsync();

                return View(allocations);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Error loading rented fridges";
                return RedirectToAction("Dashboard");
            }
        }

        public async Task<IActionResult> FridgeMoreInfo(int id)
        {
            try
            {
                var fridgeModel = await _context.FridgeModels
                    .Include(fm => fm.Supplier)
                    .FirstOrDefaultAsync(fm => fm.Id == id);

                if (fridgeModel == null)
                    return NotFound();

                var availableUnits = await _context.FridgeUnits
                    .CountAsync(fu => fu.FridgeModelId == id && fu.Status == FridgeStatus.InWarehouse);

                var viewModel = new FridgeCatalogViewModel
                {
                    Id = fridgeModel.Id,
                    Name = fridgeModel.Name,
                    Category = fridgeModel.Category,
                    PricePerMonth = fridgeModel.RentalPricePerMonth,
                    Description = fridgeModel.Description,
                    ImageUrl = fridgeModel.ImageUrl,
                    SupplierName = fridgeModel.Supplier.Name,
                    AvailableUnits = availableUnits
                };

                return View(viewModel);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Error loading fridge details";
                return RedirectToAction("Rentals");
            }
        }

        #endregion

        #region Checkout & Payment

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout([FromBody] List<CartItemViewModel> cartItems)
        {
            try
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

                var rentalsToSave = new List<RentalRequest>();
                foreach (var item in cartItems)
                {
                    // Check available units for each fridge model
                    var availableUnits = await _context.FridgeUnits
                        .CountAsync(fu => fu.FridgeModelId == item.FridgeModelId && fu.Status == FridgeStatus.InWarehouse);

                    if (availableUnits < item.Quantity)
                    {
                        return Json(new { success = false, message = $"Only {availableUnits} units available for {item.FridgeModelName}. Requested: {item.Quantity}" });
                    }

                    var rentalRequest = new RentalRequest
                    {
                        FridgeModelId = item.FridgeModelId,
                        Quantity = item.Quantity,
                        Status = "Pending",
                        RequestedByUserId = user.Id,
                        CustomerId = customer.Id,
                        CreatedAt = DateTime.UtcNow,
                        IsPaid = false
                    };

                    rentalsToSave.Add(rentalRequest);
                }

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
            catch (Exception)
            {
                return Json(new { success = false, message = "Error processing checkout" });
            }
        }

        [HttpGet]
        public IActionResult Payment(string rentalIds)
        {
            if (string.IsNullOrEmpty(rentalIds))
                return RedirectToAction("Rentals");

            ViewBag.RentalIds = rentalIds;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPayment(string rentalIds)
        {
            try
            {
                if (string.IsNullOrEmpty(rentalIds))
                    return RedirectToAction("Rentals");

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return RedirectToAction("Rentals");

                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == user.Id);
                if (customer == null)
                    return RedirectToAction("Rentals");

                var ids = rentalIds.Split(',')
                                   .Where(id => int.TryParse(id, out _))
                                   .Select(int.Parse)
                                   .ToList();

                var rentals = await _context.RentalRequests
                    .Where(r => ids.Contains(r.Id) && r.CustomerId == customer.Id)
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
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Error confirming payment";
                return RedirectToAction("Rentals");
            }
        }

        public IActionResult PaymentSuccess(string rentalIds = "")
        {
            ViewBag.RentalIds = rentalIds.Split(',')
                                          .Where(id => int.TryParse(id, out _))
                                          .Select(int.Parse)
                                          .ToList();
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> PayLater(string rentalIds)
        {
            try
            {
                if (string.IsNullOrEmpty(rentalIds))
                    return RedirectToAction("Rentals");

                var ids = rentalIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                   .Select(id => int.TryParse(id.Trim(), out var val) ? val : (int?)null)
                                   .Where(id => id.HasValue)
                                   .Select(id => id.Value)
                                   .ToList();

                var rentals = await _context.RentalRequests
                    .Include(r => r.FridgeModel)
                    .Where(r => ids.Contains(r.Id) && !r.IsPaid)
                    .ToListAsync();

                if (!rentals.Any())
                    return RedirectToAction("Rentals");

                return View(rentals);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Error loading pay later page";
                return RedirectToAction("Rentals");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPayLater(string rentalIds)
        {
            try
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
                return RedirectToAction("MyRentalRequests");
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Error confirming pay later";
                return RedirectToAction("Rentals");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayRental(int id)
        {
            try
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
            catch (Exception)
            {
                return Json(new { success = false, message = "Error processing payment" });
            }
        }

        #endregion

        #region Fault Management

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

                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.UserId == user.Id);

                if (customer == null)
                {
                    TempData["ErrorMessage"] = "No customer profile is linked to your account.";
                    return RedirectToAction(nameof(LoggedFault));
                }

                var query = _context.FridgeFaults
                    .Where(f => f.Status != FaultStatus.Cancelled && f.ReportedByUserId == user.Id)
                    .Include(f => f.FridgeUnit)
                    .ThenInclude(fu => fu.FridgeModel)
                    .Include(f => f.ReportedBy)
                    .Include(f => f.AssignedTechnician)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(search))
                {
                    search = search.ToLower();
                    query = query.Where(f =>
                        f.FridgeUnit.FridgeModel.Name.ToLower().Contains(search) ||
                        f.FridgeUnit.SerialNumber.ToLower().Contains(search) ||
                        f.FaultDescription.ToLower().Contains(search) ||
                        f.ReportedBy.FullName.ToLower().Contains(search) ||
                        (f.AssignedTechnician != null && f.AssignedTechnician.FullName.ToLower().Contains(search))
                    );
                }

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
                        FridgeUnitId = f.FridgeUnitId,
                        FridgeModel = f.FridgeUnit.FridgeModel.Name,
                        FridgeSerialNumber = f.FridgeUnit.SerialNumber,
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

                // Load all fridge units allocated to this customer
                var fridgeUnits = await _context.FridgeAllocations
                    .Include(fa => fa.FridgeUnit)
                    .ThenInclude(fu => fu.FridgeModel)
                    .Where(fa => fa.CustomerId == customer.Id &&
                                 (fa.DeallocationDate == null || fa.DeallocationDate > DateTime.Now))
                    .Select(fa => new
                    {
                        Id = fa.FridgeUnit.Id,
                        Name = $"{fa.FridgeUnit.FridgeModel.Name} - {fa.FridgeUnit.SerialNumber}"
                    })
                    .ToListAsync();

                ViewBag.FridgeUnits = new SelectList(fridgeUnits, "Id", "Name");

                return View(faults);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Error loading logged faults";
                return RedirectToAction("LoggedFault");
            }
        }

        [HttpGet]
        public async Task<IActionResult> LogFault()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Unable to identify the current user.";
                    return RedirectToAction(nameof(LoggedFault));
                }

                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.UserId == user.Id);

                if (customer == null)
                {
                    TempData["ErrorMessage"] = "No customer profile is linked to your account.";
                    return RedirectToAction(nameof(LoggedFault));
                }

                var fridgeUnits = await _context.FridgeAllocations
                    .Include(fa => fa.FridgeUnit)
                    .ThenInclude(fu => fu.FridgeModel)
                    .Where(fa => fa.CustomerId == customer.Id &&
                                 (fa.DeallocationDate == null || fa.DeallocationDate > DateTime.Now))
                    .Select(fa => new
                    {
                        Id = fa.FridgeUnit.Id,
                        Name = $"{fa.FridgeUnit.FridgeModel.Name} - {fa.FridgeUnit.SerialNumber}"
                    })
                    .ToListAsync();

                ViewBag.FridgeUnits = new SelectList(fridgeUnits, "Id", "Name");

                return View(new CreateFridgeFaultViewModel());
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Error loading log fault form";
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

                var allocation = await _context.FridgeAllocations
                    .Include(a => a.FridgeUnit)
                    .FirstOrDefaultAsync(a => a.FridgeUnitId == model.FridgeUnitId && a.CustomerId == customer.Id);

                if (allocation == null)
                {
                    TempData["ErrorMessage"] = "Invalid fridge selection. You can only log faults for your allocated fridge units.";
                    return RedirectToAction(nameof(LogFault));
                }

                var fault = new FridgeFault
                {
                    FridgeUnitId = model.FridgeUnitId,
                    ReportedByUserId = user.Id,
                    DateReported = DateTime.Now,
                    FaultDescription = model.FaultDescription,
                    Status = FaultStatus.Open
                };

                _context.FridgeFaults.Add(fault);
                await _context.SaveChangesAsync();

                // Audit trail
                var audit = new AuditTrail
                {
                    ChangeType = "Create",
                    EntityName = "FridgeFault",
                    EntityId = fault.Id,
                    ChangedByUserId = user.Id,
                    ChangeDate = DateTime.Now,
                    ChangeDetails = $"Fridge fault logged with ID {fault.Id} by user {user.FullName}."
                };
                _context.AuditTrails.Add(audit);

                // Notify admins
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
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Error logging fridge fault";
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

                fault.FridgeUnitId = model.FridgeUnitId;
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

                // Notification to Admin
                var admins = await _userManager.GetUsersInRoleAsync("Admin");
                foreach (var admin in admins)
                {
                    _context.Notifications.Add(new Notification
                    {
                        RecipientUserId = admin.Id,
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
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Error updating fridge fault";
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

                // Notify admins
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

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Fridge fault has been cancelled successfully!";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Error cancelling fridge fault";
            }

            return RedirectToAction(nameof(LoggedFault));
        }

        #endregion
    }
}