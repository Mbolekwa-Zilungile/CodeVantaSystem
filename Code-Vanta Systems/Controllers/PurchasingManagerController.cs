using Code_Vanta_Systems.Data;
using Code_Vanta_Systems.Models;
using Code_Vanta_Systems.Models.Enums;
using Code_Vanta_Systems.Services;
using Code_Vanta_Systems.ViewModels;
using Code_Vanta_Systems.ViewModels.DeliveryNote;
using Code_Vanta_Systems.ViewModels.PurchaseOrder;
using Code_Vanta_Systems.ViewModels.PurchaseRequest;
using Code_Vanta_Systems.ViewModels.SupplierQoutation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace Code_Vanta_Systems.Controllers
{
    [Authorize(Roles = "Purchasing Manager")]
    public class PurchasingManagerController : Controller
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly ApplicationDbContext context;
        private readonly EmailService emailService;

        public PurchasingManagerController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context, EmailService emailService)
        {
            this.userManager = userManager;
            this.roleManager = roleManager;
            this.context = context;
            this.emailService = emailService;
        }

        #region Suppliers
        // SUPPLIERS
        public async Task<IActionResult> Index()
        {
            var activeSuppliers = await context.Suppliers
            .ToListAsync();
            ViewBag.ActiveSupplierCount = activeSuppliers.Count;
            ViewBag.RequestCount = await context.PurchaseRequests.CountAsync();

            if (User.IsInRole("Purchasing Manager"))
            {
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var notifications = await context.Notifications
                    .Where(n => n.RecipientUserId == currentUserId) // filter by logged-in user
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(5)
                    .ToListAsync();

                ViewBag.LatestNotifications = notifications;
            }
            return View();
        }

        public async Task<IActionResult> Suppliers()
        {
            var activeSuppliers = await context.Suppliers.Include(s => s.User)
            .Where(s => s.IsActive)
            .ToListAsync();
            ViewBag.ActiveSupplierCount = activeSuppliers.Count;

            ViewBag.InactiveSuppliersCount = await context.Suppliers.CountAsync(s => !s.IsActive);
            return View(activeSuppliers);
        }

        public async Task<IActionResult> InactiveSuppliers()
        {
            var activeSuppliers = await context.Suppliers.Include(s => s.User)
            .Where(s => s.IsActive)
            .ToListAsync();
            ViewBag.ActiveSupplierCount = activeSuppliers.Count;

            var InactiveSuppliers = await context.Suppliers.Include(s => s.User)
           .Where(s => !s.IsActive)
           .ToListAsync();

            ViewBag.InactiveSuppliersCount = InactiveSuppliers.Count;
            return View(InactiveSuppliers);
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewData["Title"] = "Create New Supplier";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RegisterSupplierViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.ContactPerson,
                EmailConfirmed = true,
                PhoneNumber = model.Phone
            };

            var result = await userManager.CreateAsync(user, "Supplier@123!");

            if (result.Succeeded)
            {
                user.EmailConfirmed = true;
                await userManager.UpdateAsync(user);

                var roleExists = await context.Roles.AnyAsync(r => r.Name == "Supplier");
                if (!roleExists)
                {
                    ModelState.AddModelError(string.Empty, "Supplier role does not exist. Please create the role first.");
                    return View(model);
                }

                var roleResult = await userManager.AddToRoleAsync(user, "Supplier");

                if (roleResult.Succeeded)
                {
                    var supplier = new Supplier
                    {
                        ApplicationUserId = user.Id,
                        Name = model.CompanyName,
                        Phone = model.Phone,
                        Address = model.Address,
                        IsActive = true
                    };

                    await context.Suppliers.AddAsync(supplier);
                    await context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Supplier '{model.CompanyName}' created successfully.";
                    return RedirectToAction("Suppliers");
                }

                await userManager.DeleteAsync(user);
                ModelState.AddModelError(string.Empty, "Failed to assign 'Supplier' role. User has been deleted.");
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var supplier = await context.Suppliers
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (supplier == null || supplier.User == null)
            {
                TempData["ErrorMessage"] = "Error: Supplier not found. Deactivation failed.";
                return RedirectToAction("Suppliers");
            }

            var user = supplier.User;

            supplier.IsActive = false;
            context.Suppliers.Update(supplier);

            await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
            context.Users.Update(user);

            await context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Supplier '{supplier.Name}' successfully deactivated.";
            return RedirectToAction("Suppliers");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var supplier = await context.Suppliers
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (supplier == null || supplier.User == null)
            {
                TempData["ErrorMessage"] = "Supplier not found.";
                return RedirectToAction("Suppliers");
            }

            var viewModel = new EditSupplierViewModel
            {
                SupplierId = supplier.Id,
                ApplicationUserId = supplier.ApplicationUserId,
                ContactPerson = supplier.User.FullName,
                Email = supplier.User.Email,
                PhoneNumber = supplier.User.PhoneNumber,
                CompanyName = supplier.Name,
                Address = supplier.Address
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditSupplierViewModel model)
        {
            if (id != model.SupplierId)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var supplier = await context.Suppliers
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (supplier == null || supplier.User == null)
            {
                TempData["ErrorMessage"] = "Supplier not found.";
                return RedirectToAction("Suppliers");
            }

            var user = supplier.User;

            try
            {
                user.FullName = model.ContactPerson;
                user.Email = model.Email;
                user.UserName = model.Email;
                user.PhoneNumber = model.PhoneNumber;
                var userUpdateResult = await userManager.UpdateAsync(user);

                if (!userUpdateResult.Succeeded)
                {
                    foreach (var error in userUpdateResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return View(model);
                }

                supplier.Name = model.CompanyName;
                supplier.Phone = model.PhoneNumber;
                supplier.Address = model.Address;

                context.Suppliers.Update(supplier);
                await context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Supplier '{supplier.Name}' updated successfully.";
                return RedirectToAction("Suppliers");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await context.Suppliers.AnyAsync(s => s.Id == id))
                {
                    return NotFound();
                }
                throw;
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reactivate(int id)
        {
            var supplier = await context.Suppliers
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (supplier == null || supplier.User == null)
            {
                TempData["ErrorMessage"] = "Supplier not found.";
                return RedirectToAction(nameof(InactiveSuppliers));
            }

            var user = supplier.User;

            supplier.IsActive = true;
            await userManager.SetLockoutEndDateAsync(user, null);

            const string roleName = "Supplier";
            if (await roleManager.RoleExistsAsync(roleName))
            {
                var currentRoles = await userManager.GetRolesAsync(user);
                if (!currentRoles.Contains(roleName))
                {
                    await userManager.AddToRoleAsync(user, roleName);
                }
            }
            else
            {
                TempData["WarningMessage"] = $"Supplier '{user.Email}' reactivated, but role '{roleName}' does not exist. Please assign manually.";
            }

            context.Update(supplier);
            await context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Supplier '{user.Email}' has been successfully reactivated.";
            return RedirectToAction(nameof(Suppliers));
        }
        #endregion

        #region Purchase Request
        // PURCHASE REQUEST
        public async Task<IActionResult> PurchaseRequests()
        {
            var SubmitedRequests = await context.PurchaseRequests
               .Include(pr => pr.RequestedBy)
               .Include(pr => pr.RequestedItems)
               .Where(pr => pr.Status == Code_Vanta_Systems.Models.Enums.RequestStatus.Submitted)
               .OrderByDescending(pr => pr.RequestDate)
               .ToListAsync();

            ViewBag.PendingCount = await context.PurchaseRequests.CountAsync(s => s.Status == RequestStatus.Pending);
            ViewBag.SubmitedCount = SubmitedRequests.Count;
            ViewBag.RejectedRequests = await context.PurchaseRequests.CountAsync(s => s.Status == RequestStatus.Rejected);
            ViewBag.ApprovedRequestsCount = await context.PurchaseRequests.CountAsync(s => s.Status == RequestStatus.Approved);
            ViewBag.CompletedRequestsCount = await context.PurchaseRequests.CountAsync(s => s.Status == RequestStatus.Completed);
            return View(SubmitedRequests);
        }

        public async Task<IActionResult> RejectedPurchaseRequests()
        {
            var purchaseRequests = await context.PurchaseRequests
                .Include(pr => pr.RequestedBy)
                .Include(pr => pr.RequestedItems)
                .Where(pr => pr.Status == RequestStatus.Rejected)
                .OrderByDescending(pr => pr.RequestDate)
                .ToListAsync();

            ViewBag.PendingCount = await context.PurchaseRequests.CountAsync(s => s.Status == RequestStatus.Pending);
            ViewBag.SubmitedCount = await context.PurchaseRequests.CountAsync(s => s.Status == RequestStatus.Submitted);
            ViewBag.RejectedRequests = await context.PurchaseRequests.CountAsync(s => s.Status == RequestStatus.Rejected);
            ViewBag.ApprovedRequestsCount = await context.PurchaseRequests.CountAsync(s => s.Status == RequestStatus.Approved);
            ViewBag.CompletedRequestsCount = await context.PurchaseRequests.CountAsync(s => s.Status == RequestStatus.Completed);
            return View(purchaseRequests);
        }

        public async Task<IActionResult> ApprovedPurchaseRequests()
        {
            var purchaseRequests = await context.PurchaseRequests
                .Include(pr => pr.RequestedBy)
                .Include(pr => pr.RequestedItems)
                .Where(pr => pr.Status == RequestStatus.Approved)
                .OrderByDescending(pr => pr.RequestDate)
                .ToListAsync();

            ViewBag.PendingCount = await context.PurchaseRequests.CountAsync(s => s.Status == RequestStatus.Pending);
            ViewBag.SubmitedCount = await context.PurchaseRequests.CountAsync(s => s.Status == RequestStatus.Submitted);
            ViewBag.RejectedRequests = await context.PurchaseRequests.CountAsync(s => s.Status == RequestStatus.Rejected);
            ViewBag.ApprovedRequestsCount = await context.PurchaseRequests.CountAsync(s => s.Status == RequestStatus.Approved);
            ViewBag.CompletedRequestsCount = await context.PurchaseRequests.CountAsync(s => s.Status == RequestStatus.Completed);
            return View(purchaseRequests);
        }

        public IActionResult CreatePurchaseRequest()
        {
            return View();
        }

        public async Task<IActionResult> Details(int id)
        {
            var request = await context.PurchaseRequests
                         .Include(r => r.RequestedBy)
                         .Include(r => r.RequestedItems)
                         .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null) return NotFound();

            return View(request);
        }

        [HttpGet]
        public async Task<IActionResult> EditRequest(int id)
        {
            var request = await context.PurchaseRequests
                .Include(r => r.RequestedItems)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
                return NotFound();

            // Use FridgeModels instead of FridgeUnits
            var fridgeModels = await context.FridgeModels
                 .Include(f => f.Supplier)
                 .Select(f => new SelectListItem
                 {
                     Value = f.Id.ToString(),
                     Text = $"{f.Name} - {f.Supplier.Name}" // Use FridgeModel.Name and Supplier
                 })
                 .ToListAsync();

            var model = new EditPurchaseRequestViewModel
            {
                Id = request.Id,
                ExpectedDate = request.ExpectedDate,
                Priority = request.Priority,
                Status = Code_Vanta_Systems.Models.Enums.RequestStatus.Submitted,
                Notes = request.Notes,
                Items = request.RequestedItems.Select(i => new PurchaseRequestItemViewModel
                {
                    PurchaseRequestItemId = i.Id,
                    FridgeModelId = i.FridgeModelId, // Changed from FridgeId to FridgeModelId
                    Quantity = i.Quantity,
                    AvailableItems = fridgeModels
                }).ToList()
            };

            model.Items.Add(new PurchaseRequestItemViewModel { AvailableItems = fridgeModels });

            ViewBag.Fridges = fridgeModels; // Actually fridge models
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRequest(EditPurchaseRequestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var fridgeModels = await context.FridgeModels
                     .Include(f => f.Supplier)
                     .Select(f => new SelectListItem
                     {
                         Value = f.Id.ToString(),
                         Text = $"{f.Name} - {f.Supplier.Name}"
                     })
                     .ToListAsync();

                foreach (var item in model.Items)
                    item.AvailableItems = fridgeModels;

                ViewBag.Fridges = fridgeModels;
                return View(model);
            }

            var request = await context.PurchaseRequests
                .Include(r => r.RequestedItems)
                .FirstOrDefaultAsync(r => r.Id == model.Id);

            if (request == null) return NotFound();

            request.ExpectedDate = model.ExpectedDate;
            request.Priority = model.Priority;
            request.Status = Code_Vanta_Systems.Models.Enums.RequestStatus.Submitted;
            request.Notes = model.Notes;

            var itemIdsFromForm = model.Items
                .Where(i => i.PurchaseRequestItemId.HasValue)
                .Select(i => i.PurchaseRequestItemId.Value)
                .ToList();
            var itemsToRemove = request.RequestedItems
                .Where(i => !itemIdsFromForm.Contains(i.Id))
                .ToList();
            context.PurchaseRequestItems.RemoveRange(itemsToRemove);

            foreach (var itemVm in model.Items.Where(i => i.FridgeModelId > 0 && i.Quantity >= 1)) // Changed to FridgeModelId
            {
                var fridgeModel = await context.FridgeModels
                    .Include(f => f.Supplier)
                    .FirstOrDefaultAsync(f => f.Id == itemVm.FridgeModelId); // Changed to FridgeModelId

                if (fridgeModel == null) continue;

                if (itemVm.PurchaseRequestItemId.HasValue)
                {
                    var existingItem = request.RequestedItems.First(i => i.Id == itemVm.PurchaseRequestItemId.Value);

                    existingItem.FridgeModelId = fridgeModel.Id; // Changed to FridgeModelId
                    existingItem.ModelRequested = fridgeModel.Name;
                    existingItem.Description = fridgeModel.Description;
                    existingItem.Quantity = itemVm.Quantity;
                }
                else
                {
                    var newItem = new PurchaseRequestItem
                    {
                        PurchaseRequestId = request.Id,
                        FridgeModelId = fridgeModel.Id, // Changed to FridgeModelId
                        ModelRequested = fridgeModel.Name,
                        Description = fridgeModel.Description,
                        Quantity = itemVm.Quantity
                    };
                    context.PurchaseRequestItems.Add(newItem);
                }
            }

            await context.SaveChangesAsync();
            TempData["Success"] = $"Purchase request #{request.Id} updated successfully!";
            return RedirectToAction("Details", new { id = request.Id });
        }

        [HttpGet]
        public async Task<IActionResult> RejectRequest(int id)
        {
            var request = await context.PurchaseRequests
                .Include(r => r.RequestedBy)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
                return NotFound();

            return View(request);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectRequest(int id, string reason)
        {
            var request = await context.PurchaseRequests
                .Include(r => r.RequestedBy)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
                return NotFound();

            request.Status = Models.Enums.RequestStatus.Rejected;
            request.Notes += string.IsNullOrEmpty(reason) ? "" : $"\nRejection Reason: {reason}";

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError("", "Unable to update request: " + ex.Message);
                return RedirectToAction("Details", new { id });
            }

            var inventoryliaison = await userManager.GetUsersInRoleAsync("Inventory Liaison");
            var liaisonUser = inventoryliaison.FirstOrDefault();

            if (liaisonUser != null)
            {
                var notification = new Notification
                {
                    RecipientUserId = liaisonUser.Id,
                    Title = "Purchase Request Rejected",
                    Message = $"Your purchase request #{request.Id} has been rejected by {User.Identity.Name}.",
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false,
                    TargetController = "InventoryLiaison",
                    TargetAction = "RejectedPurchaseRequests"
                };

                context.Notifications.Add(notification);
                await context.SaveChangesAsync();
            }

            TempData["Success"] = $"Purchase request #{request.Id} rejected successfully!";
            return RedirectToAction("PurchaseRequests");
        }
        #endregion

        #region RFQ
        public async Task<IActionResult> RFQ()
        {
            var rfqs = await context.RequestQuotations
                .Include(r => r.Supplier)
                    .ThenInclude(s => s.User)
                .Include(r => r.RequestedBy)
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();

            var vm = rfqs.Select(r => new RFQListItemViewModel
            {
                RFQId = r.RFQId,
                RequestQuotationId = r.RequestQuotationId,
                RequestDate = r.RequestDate,
                SupplierName = r.Supplier?.Name,
                SupplierEmail = r.Supplier?.User?.Email,
                RequestedBy = r.RequestedBy?.UserName ?? "Unknown",
                Notes = r.Notes
            }).ToList();

            return View(vm);
        }

        public async Task<IActionResult> CreateRFQ(int id)
        {
            var purchaseRequest = await context.PurchaseRequests
                .Include(r => r.RequestedItems)
                    .ThenInclude(ri => ri.FridgeModel)  // Changed from FridgeUnit to FridgeModel
                    .ThenInclude(fm => fm.Supplier)     // Include supplier
                .Include(r => r.RequestedBy)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (purchaseRequest == null)
                return NotFound();

            var suppliers = await context.Suppliers
                .Include(s => s.User)
                .Where(s => s.IsActive)
                .ToListAsync();
            var company = await context.Company.FirstOrDefaultAsync();

            var viewModel = new RQFViewModel
            {
                PurchaseRequestId = purchaseRequest.Id,
                PurchaseRequestNotes = purchaseRequest.Notes,
                RequestedDate = DateTime.Now,
                ExpectedDate = purchaseRequest.ExpectedDate,
                Status = purchaseRequest.Status,
                Priority = purchaseRequest.Priority,
                Suppliers = suppliers,
                SelectedSupplierIds = new List<int>(),
                Company = company,

                PurchaseRequestItems = purchaseRequest.RequestedItems.Select(item => new PurchaseRequestItemViewModel
                {
                    FridgeModelId = item.FridgeModelId, // Changed from FridgeId to FridgeModelId
                    Quantity = item.Quantity,
                    FridgeModel = item.FridgeModel?.Name ?? "Unknown"  // Get from FridgeModel
                }).ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRFQ(RQFViewModel model)
        {
            if (model.SelectedSupplierIds == null || !model.SelectedSupplierIds.Any())
            {
                ModelState.AddModelError(nameof(model.SelectedSupplierIds), "Please select at least one supplier.");
            }

            if (!ModelState.IsValid)
            {
                model.Suppliers = await context.Suppliers
                    .Include(s => s.User)
                    .Where(s => s.IsActive)
                    .ToListAsync();

                // UPDATED: Use FridgeUnits instead of Fridges
                var allFridgeUnits = await context.FridgeUnits
                    .Include(f => f.FridgeModel)
                    .Where(f => f.Status != FridgeStatus.Scrapped)
                    .Select(f => new SelectListItem
                    {
                        Value = f.Id.ToString(),
                        Text = $"{f.FridgeModel.Name} ({f.SerialNumber})"
                    })
                    .ToListAsync();

                foreach (var item in model.PurchaseRequestItems)
                {
                    item.AvailableItems = allFridgeUnits;
                }

                return View(model);
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var purchaseRequest = await context.PurchaseRequests
                .FirstOrDefaultAsync(r => r.Id == model.PurchaseRequestId);

            if (purchaseRequest != null)
            {
                purchaseRequest.Status = RequestStatus.Approved;
                context.PurchaseRequests.Update(purchaseRequest);
            }

            string lastRFQId = await context.RequestQuotations
                .OrderByDescending(r => r.RequestQuotationId)
                .Select(r => r.RFQId)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (!string.IsNullOrEmpty(lastRFQId))
            {
                var parts = lastRFQId.Split('-');
                if (parts.Length == 3 && int.TryParse(parts[2], out int lastNum))
                {
                    nextNumber = lastNum + 1;
                }
            }

            foreach (var supplierId in model.SelectedSupplierIds)
            {
                string newRFQId = $"RFQ-{DateTime.Now.Year}-{nextNumber:000}";
                nextNumber++;

                var requestQuotation = new RequestQuotation
                {
                    RFQId = newRFQId,
                    RequestDate = DateTime.Now,
                    RequestedByUserId = currentUserId,
                    SupplierId = supplierId,
                    PurchaseRequestId = model.PurchaseRequestId,
                    Notes = model.Notes ?? "",
                    ExpectedDeliveryDate = purchaseRequest?.ExpectedDate
                };

                context.RequestQuotations.Add(requestQuotation);

                var supplier = await context.Suppliers
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.Id == supplierId);

                if (supplier != null && supplier.User != null)
                {
                    var notification = new Notification
                    {
                        RecipientUserId = supplier.User.Id,
                        Title = "New RFQ Created",
                        Message = $"A new RFQ ({newRFQId}) has been created for you. Please respond.",
                        TargetController = "Supplier",
                        TargetAction = "RFQ"
                    };
                    context.Notifications.Add(notification);

                    try
                    {
                        string subject = $"New RFQ Created: {newRFQId}";
                        string body = $@"
                    <p>A new RFQ (<strong>{newRFQId}</strong>) has been created for you. Expected delivery date: <strong>{purchaseRequest.ExpectedDate:dd MMM yyyy}</strong>.</p>
<p>Please log in to your dashboard to view and respond.</p>
<p>Best regards,<br/>CodeVanta Team</p>";
                        await emailService.SendEmailAsync(supplier.User.Email, subject, body);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to send email to {supplier.User.Email}: {ex.Message}");
                    }
                }
            }

            await context.SaveChangesAsync();
            return RedirectToAction("RFQ", "PurchasingManager", new { id = model.PurchaseRequestId });
        }

        public async Task<IActionResult> RFQDetails(int id)
        {
            var rfq = await context.RequestQuotations
                .Include(r => r.Supplier)
                    .ThenInclude(s => s.User)
                .Include(r => r.PurchaseRequest)
                    .ThenInclude(pr => pr.RequestedItems)
                        .ThenInclude(ri => ri.FridgeModel)  // Changed from FridgeUnit to FridgeModel
                .Include(r => r.RequestedBy)
                .FirstOrDefaultAsync(r => r.RequestQuotationId == id);

            if (rfq == null)
            {
                return NotFound();
            }

            var viewModel = new RFQDetailViewModel
            {
                RFQId = rfq.RFQId,
                RequestDate = rfq.RequestDate,
                ExpectedDate = rfq.PurchaseRequest?.ExpectedDate,
                Notes = rfq.Notes,
                SupplierName = rfq.Supplier?.Name ?? rfq.Supplier?.ContactName,
                SupplierEmail = rfq.Supplier?.User?.Email,
                SupplierPhone = rfq.Supplier?.Phone,
                SupplierAddress = rfq.Supplier?.Address,
                IssuedByName = rfq.RequestedBy?.UserName ?? "Unknown",
                Items = rfq.PurchaseRequest?.RequestedItems.Select(ri => new RFQItemViewModel
                {
                    FridgeModel = ri.FridgeModel?.Name ?? "Unknown",  // Get from FridgeModel
                    Quantity = ri.Quantity
                }).ToList() ?? new List<RFQItemViewModel>()
            };
            return View(viewModel);
        }
        #endregion

        #region Supplier Quotation
        public async Task<IActionResult> SupplerQuotations()
        {
            var allQuotations = await context.Quotations.ToListAsync();
            ViewBag.PendingCount = allQuotations.Count(q => q.Status == "Pending");
            ViewBag.ApprovedCount = allQuotations.Count(q => q.Status == "Approved");
            ViewBag.RejectedCount = allQuotations.Count(q => q.Status == "Rejected");
            ViewBag.sentCount = allQuotations.Count(q => q.Status == "Sent");

            var quotations = await context.Quotations
                .Where(q => q.Status == "Pending")
                .Include(q => q.RequestQuotation)
                .Include(q => q.Supplier)
                    .ThenInclude(s => s.User)
                .Include(q => q.Items)
                    .ThenInclude(i => i.PurchaseRequestItem)
                .OrderByDescending(q => q.DateReceived)
                .ToListAsync();

            return View(quotations);
        }

        public async Task<IActionResult> ApprovedQuotations()
        {
            var allQuotations = await context.Quotations.ToListAsync();
            ViewBag.PendingCount = allQuotations.Count(q => q.Status == "Pending");
            ViewBag.ApprovedCount = allQuotations.Count(q => q.Status == "Approved");
            ViewBag.RejectedCount = allQuotations.Count(q => q.Status == "Rejected");
            ViewBag.sentCount = allQuotations.Count(q => q.Status == "Sent");

            var quotations = await context.Quotations
                .Where(q => q.Status == "Approved")
               .Include(q => q.RequestQuotation)
                .Include(q => q.Supplier)
                    .ThenInclude(s => s.User)
                .Include(q => q.Items)
                    .ThenInclude(i => i.PurchaseRequestItem)
                .OrderByDescending(q => q.DateReceived)
                .ToListAsync();

            return View(quotations);
        }

        public async Task<IActionResult> RejectedQuotations()
        {
            var allQuotations = await context.Quotations.ToListAsync();
            ViewBag.PendingCount = allQuotations.Count(q => q.Status == "Pending");
            ViewBag.ApprovedCount = allQuotations.Count(q => q.Status == "Approved");
            ViewBag.RejectedCount = allQuotations.Count(q => q.Status == "Rejected");
            ViewBag.sentCount = allQuotations.Count(q => q.Status == "Sent");

            var quotations = await context.Quotations
                .Where(q => q.Status == "Rejected")
              .Include(q => q.RequestQuotation)
                .Include(q => q.Supplier)
                    .ThenInclude(s => s.User)
                .Include(q => q.Items)
                    .ThenInclude(i => i.PurchaseRequestItem)
                .OrderByDescending(q => q.DateReceived)
                .ToListAsync();

            return View(quotations);
        }

        public async Task<IActionResult> SentQuotations()
        {
            var allQuotations = await context.Quotations.ToListAsync();
            ViewBag.PendingCount = allQuotations.Count(q => q.Status == "Pending");
            ViewBag.ApprovedCount = allQuotations.Count(q => q.Status == "Approved");
            ViewBag.RejectedCount = allQuotations.Count(q => q.Status == "Rejected");
            ViewBag.sentCount = allQuotations.Count(q => q.Status == "Sent");

            var quotations = await context.Quotations
                .Where(q => q.Status == "Sent")
              .Include(q => q.RequestQuotation)
                .Include(q => q.Supplier)
                    .ThenInclude(s => s.User)
                .Include(q => q.Items)
                    .ThenInclude(i => i.PurchaseRequestItem)
                .OrderByDescending(q => q.DateReceived)
                .ToListAsync();

            return View(quotations);
        }
        #endregion

        #region Comparison
        public async Task<IActionResult> SelectPurchaseRequest()
        {
            var purchaseRequests = await context.PurchaseRequests
                .Select(pr => new { pr.Id, pr.RequestNumber })
                .ToListAsync();

            ViewBag.PurchaseRequests = new SelectList(purchaseRequests, "Id", "RequestNumber");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SelectPurchaseRequest(int purchaseRequestId)
        {
            if (purchaseRequestId == 0)
            {
                ModelState.AddModelError("", "Please select a purchase request.");
                return RedirectToAction(nameof(SelectPurchaseRequest));
            }

            var purchaseRequest = await context.PurchaseRequests
                .FirstOrDefaultAsync(pr => pr.Id == purchaseRequestId);

            if (purchaseRequest == null)
                return NotFound("Purchase request not found.");

            var quotations = await context.Quotations
                .Where(q => q.PurchaseRequestId == purchaseRequestId && q.Status == "Pending")
                .Include(q => q.RequestQuotation)
                .Include(q => q.Supplier)
                .Include(q => q.Items)
                .ToListAsync();

            ViewBag.RequestNumber = purchaseRequest.RequestNumber;
            return View("QuotationsByRequest", quotations);
        }

        public async Task<IActionResult> QuotationsByRequest(int purchaseRequestId)
        {
            if (purchaseRequestId == 0)
                return RedirectToAction("SelectPurchaseRequest");

            var purchaseRequest = await context.PurchaseRequests
                .FirstOrDefaultAsync(pr => pr.Id == purchaseRequestId);

            if (purchaseRequest == null)
                return NotFound("Purchase Request not found.");

            var quotations = await context.Quotations
                .Where(q => q.PurchaseRequestId == purchaseRequestId)
                .Include(q => q.RequestQuotation)
                .Include(q => q.Supplier)
                .Include(q => q.Items)
                .ToListAsync();

            if (!quotations.Any())
            {
                ViewBag.Message = "No quotations found for this purchase request.";
            }

            ViewBag.RequestNumber = purchaseRequest.RequestNumber;
            return View(quotations);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveQuotation(int quotationId)
        {
            var quotation = await context.Quotations
                .Include(q => q.PurchaseRequest)
                .Include(q => q.Supplier)
                    .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(q => q.Id == quotationId);

            if (quotation == null)
                return NotFound("Quotation not found.");

            var allQuotations = await context.Quotations
                .Where(q => q.PurchaseRequestId == quotation.PurchaseRequestId)
                .Include(q => q.Supplier)
                    .ThenInclude(s => s.User)
                .ToListAsync();

            foreach (var q in allQuotations)
            {
                if (q.Id == quotation.Id)
                {
                    q.Status = "Approved";

                    var notification = new Notification
                    {
                        RecipientUserId = q.Supplier.ApplicationUserId,
                        Title = "Quotation Approved",
                        Message = $"Your quotation #{q.Id} for Purchase Request {q.PurchaseRequest.RequestNumber} has been approved.",
                        CreatedAt = DateTime.UtcNow,
                        IsRead = false,
                        TargetController = "Supplier",
                        TargetAction = "Quotations"
                    };
                    context.Notifications.Add(notification);

                    try
                    {
                        if (!string.IsNullOrEmpty(q.Supplier.User?.Email))
                        {
                            await emailService.SendEmailAsync(
                                q.Supplier.User.Email,
                                "Quotation Approved",
                                $"<p>Dear {q.Supplier.Name},</p>" +
                                $"<p>Your quotation <strong>#{q.Id}</strong> for Purchase Request <strong>{q.PurchaseRequest.RequestNumber}</strong> has been <b>approved</b>.</p>" +
                                $"<p>Thank you for working with CodeVanta Systems.</p>");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Email sending failed: {ex.Message}");
                    }
                }
                else
                {
                    q.Status = "Rejected";

                    var notification = new Notification
                    {
                        RecipientUserId = q.Supplier.ApplicationUserId,
                        Title = "Quotation Rejected",
                        Message = $"Your quotation #{q.Id} for Purchase Request {q.PurchaseRequest.RequestNumber} has been rejected.",
                        CreatedAt = DateTime.UtcNow,
                        IsRead = false,
                        TargetController = "Supplier",
                        TargetAction = "Quotations"
                    };
                    context.Notifications.Add(notification);

                    try
                    {
                        if (!string.IsNullOrEmpty(q.Supplier.User?.Email))
                        {
                            await emailService.SendEmailAsync(
                                q.Supplier.User.Email,
                                "Quotation Rejected",
                                $"<p>Dear {q.Supplier.Name},</p>" +
                                $"<p>We regret to inform you that your quotation <strong>#{q.Id}</strong> for Purchase Request <strong>{q.PurchaseRequest.RequestNumber}</strong> has been <b>rejected</b>.</p>" +
                                $"<p>We appreciate your participation and encourage you to quote again in the future.</p>");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Email sending failed: {ex.Message}");
                    }
                }
            }

            await context.SaveChangesAsync();
            TempData["Success"] = $"Quotation #{quotation.Id} approved and others rejected.";
            return RedirectToAction("ApprovedQuotations");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectQuotation(int quotationId)
        {
            var quotation = await context.Quotations
                .Include(q => q.PurchaseRequest)
                .Include(q => q.Supplier)
                    .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(q => q.Id == quotationId);

            if (quotation == null)
                return NotFound("Quotation not found.");

            quotation.Status = "Rejected";

            var notification = new Notification
            {
                RecipientUserId = quotation.Supplier.ApplicationUserId,
                Title = "Quotation Rejected",
                Message = $"Your quotation #{quotation.Id} for Purchase Request {quotation.PurchaseRequest.RequestNumber} has been rejected.",
                CreatedAt = DateTime.UtcNow,
                IsRead = false,
                TargetController = "Supplier",
                TargetAction = "Quotations"
            };
            context.Notifications.Add(notification);

            if (!string.IsNullOrEmpty(quotation.Supplier.User?.Email))
            {
                await emailService.SendEmailAsync(quotation.Supplier.User.Email,
                    "Quotation Rejected",
                    $"We regret to inform you that your quotation #{quotation.Id} for Purchase Request {quotation.PurchaseRequest.RequestNumber} has been rejected.");
            }

            await context.SaveChangesAsync();
            TempData["Success"] = $"Quotation #{quotation.Id} rejected.";
            return RedirectToAction("QuotationsByRequest", new { purchaseRequestId = quotation.PurchaseRequestId });
        }
        #endregion

        #region Purchase Order
        [HttpGet]
        public async Task<IActionResult> CreatePurchaseOrder(int quotationId)
        {
            var quotation = await context.Quotations
                .Include(q => q.Supplier)
                    .ThenInclude(s => s.User)
                .Include(q => q.RequestQuotation)
                    .ThenInclude(rq => rq.PurchaseRequest)
                .Include(q => q.Items)
                    .ThenInclude(i => i.PurchaseRequestItem)
                    .ThenInclude(pri => pri.FridgeModel)
                .FirstOrDefaultAsync(q => q.Id == quotationId);

            if (quotation == null)
                return NotFound();

            var company = await context.Company.FirstOrDefaultAsync();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await context.Users.FindAsync(userId);
            var roles = await userManager.GetRolesAsync(user);

            var vm = new PurchaseOrderCreateViewModel
            {
                QuotationId = quotation.Id,
                QuotationNumber = quotation.QuotationNumber,
                SupplierId = quotation.SupplierId,
                SupplierName = quotation.Supplier.Name,
                SupplierEmail = quotation.Supplier.User?.Email,
                SupplierPhone = quotation.Supplier.Phone,
                SupplierAddress = quotation.Supplier.Address,

                CompanyName = company?.Name,
                CompanyEmail = company?.Email,
                CompanyPhone = company?.Phone,
                CompanyAddress = company?.Address,

                ExpectedDeliveryDate = quotation.RequestQuotation?.ExpectedDeliveryDate,
                DeliveryAddress = "CoolTech Warehouse, 124 Logistic Ave, Johannesburg",
                DeliveryTerms = "Free on Board (FOB) Johannesburg",
                PaymentTerms = "30 Days after Delivery",

                Items = quotation.Items.Select(i => new PurchaseOrderItemViewModel
                {
                    FridgeModelId = i.PurchaseRequestItem.FridgeModelId, // Use FridgeModelId
                    ItemName = i.PurchaseRequestItem.FridgeModel?.Name ?? i.FridgeModel,
                    Quantity = i.QuotedQuantity,
                    UnitPrice = i.QuotedUnitPrice
                }).ToList(),

                ManagerName = user?.FullName,
                ManagerEmail = user?.Email,
                ManagerRole = string.Join(", ", roles)
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePurchaseOrder(PurchaseOrderCreateViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            string lastPO = await context.PurchaseOrders
                .OrderByDescending(po => po.PurchaseOrderId)
                .Select(po => po.PONumber)
                .FirstOrDefaultAsync();

            int nextNum = 1;
            if (!string.IsNullOrEmpty(lastPO) && lastPO.StartsWith("PO-"))
            {
                var parts = lastPO.Split('-');
                if (parts.Length == 3 && int.TryParse(parts[2], out int lastNum))
                    nextNum = lastNum + 1;
            }

            string newPONumber = $"PO-{DateTime.Now.Year}-{nextNum:000}";

            var purchaseOrder = new PurchaseOrder
            {
                PONumber = newPONumber,
                QuotationId = model.QuotationId,
                SupplierId = model.SupplierId,
                ExpectedDeliveryDate = model.ExpectedDeliveryDate,
                DeliveryAddress = model.DeliveryAddress,
                DeliveryTerms = model.DeliveryTerms,
                PaymentTerms = model.PaymentTerms,
                CreatedDate = DateTime.Now,
                OrderDate = DateTime.Now,
                OrderedByUserId = userId,
                Status = OrderStatus.Pending,
                Items = new List<PurchaseOrderItem>()
            };

            foreach (var item in model.Items)
            {
                purchaseOrder.Items.Add(new PurchaseOrderItem
                {
                    FridgeModelId = item.FridgeModelId, // Use FridgeModelId
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                });
            }

            context.PurchaseOrders.Add(purchaseOrder);

            var quotation = await context.Quotations.FindAsync(model.QuotationId);
            if (quotation != null)
            {
                quotation.Status = "Sent";
                context.Quotations.Update(quotation);
            }

            await context.SaveChangesAsync();

            var supplierUserId = await context.Suppliers
                .Where(s => s.Id == model.SupplierId)
                .Select(s => s.User.Id)
                .FirstOrDefaultAsync();

            if (!string.IsNullOrEmpty(supplierUserId))
            {
                var notification = new Notification
                {
                    RecipientUserId = supplierUserId,
                    Title = "New Purchase Order Created",
                    Message = $"A new Purchase Order ({newPONumber}) has been created based on your quotation ({quotation?.QuotationNumber}).",
                    TargetController = "Supplier",
                    TargetAction = "PurchaseOrders",
                    CreatedAt = DateTime.Now
                };
                context.Notifications.Add(notification);
                await context.SaveChangesAsync();
            }

            return RedirectToAction("AllPurchaseOrders");
        }

        public async Task<IActionResult> PurchaseOrderDetails(int id)
        {
            var purchaseOrder = await context.PurchaseOrders
                .Include(po => po.Supplier)
                    .ThenInclude(s => s.User)
                .Include(po => po.Items)
                    .ThenInclude(i => i.FridgeModel)  // CHANGED: FridgeUnit → FridgeModel
                .Include(po => po.Quotation)
                    .ThenInclude(q => q.RequestQuotation)
                .Include(po => po.DeliveryNotes)
                .FirstOrDefaultAsync(po => po.PurchaseOrderId == id);

            if (purchaseOrder == null)
                return NotFound();

            ViewBag.TotalAmount = purchaseOrder.Items?.Sum(i => i.Quantity * i.UnitPrice) ?? 0;
            ViewBag.ExpectedDeliveryDate = purchaseOrder.Quotation?.RequestQuotation?.ExpectedDeliveryDate;

            return View(purchaseOrder);
        }

        public async Task<IActionResult> AllPurchaseOrders()
        {
            var allOrders = await context.PurchaseOrders
                .Include(po => po.Supplier)
                    .ThenInclude(s => s.User)
                .Include(po => po.Items)
                    .ThenInclude(i => i.FridgeModel)  // CHANGED: FridgeUnit → FridgeModel
                .Include(po => po.Quotation)
                    .ThenInclude(q => q.RequestQuotation)
                .OrderByDescending(po => po.CreatedDate)
                .ToListAsync();

            ViewBag.PendingCount = allOrders.Count(po => po.Status == OrderStatus.Pending);
            ViewBag.ApprovedCount = allOrders.Count(po => po.Status == OrderStatus.Approved);
            ViewBag.DeliveredCount = allOrders.Count(po => po.Status == OrderStatus.Delivered);
            ViewBag.ShippedCount = allOrders.Count(po => po.Status == OrderStatus.Shipped);

            var pendingOrders = allOrders
                .Where(po => po.Status == OrderStatus.Pending)
                .OrderByDescending(po => po.CreatedDate)
                .ToList();

            return View(pendingOrders);
        }

        public async Task<IActionResult> ApprovedPurchaseOrders()
        {
            var allOrders = await context.PurchaseOrders
                .Include(po => po.Supplier)
                    .ThenInclude(s => s.User)
                .Include(po => po.Items)
                    .ThenInclude(i => i.FridgeModel)  // CHANGED: FridgeUnit → FridgeModel
                .Include(po => po.Quotation)
                    .ThenInclude(q => q.RequestQuotation)
                .OrderByDescending(po => po.CreatedDate)
                .ToListAsync();

            ViewBag.PendingCount = allOrders.Count(po => po.Status == OrderStatus.Pending);
            ViewBag.ApprovedCount = allOrders.Count(po => po.Status == OrderStatus.Approved);
            ViewBag.DeliveredCount = allOrders.Count(po => po.Status == OrderStatus.Delivered);
            ViewBag.ShippedCount = allOrders.Count(po => po.Status == OrderStatus.Shipped);

            var approvedOrders = allOrders
                .Where(po => po.Status == OrderStatus.Approved)
                .OrderByDescending(po => po.CreatedDate)
                .ToList();

            return View(approvedOrders);
        }

        public async Task<IActionResult> ShippedPurchaseOrders()
        {
            var allOrders = await context.PurchaseOrders
                .Include(po => po.Supplier)
                    .ThenInclude(s => s.User)
                .Include(po => po.Items)
                    .ThenInclude(i => i.FridgeModel)  // CHANGED: FridgeUnit → FridgeModel
                .Include(po => po.Quotation)
                    .ThenInclude(q => q.RequestQuotation)
                .OrderByDescending(po => po.CreatedDate)
                .ToListAsync();

            ViewBag.PendingCount = allOrders.Count(po => po.Status == OrderStatus.Pending);
            ViewBag.ApprovedCount = allOrders.Count(po => po.Status == OrderStatus.Approved);
            ViewBag.DeliveredCount = allOrders.Count(po => po.Status == OrderStatus.Delivered);
            ViewBag.ShippedCount = allOrders.Count(po => po.Status == OrderStatus.Shipped);

            var shippedOrders = allOrders
                .Where(po => po.Status == OrderStatus.Shipped)
                .OrderByDescending(po => po.CreatedDate)
                .ToList();

            return View(shippedOrders);
        }

        public async Task<IActionResult> DeliveredPurchaseOrders()
        {
            var allOrders = await context.PurchaseOrders
                .Include(po => po.Supplier)
                    .ThenInclude(s => s.User)
                .Include(po => po.Items)
                    .ThenInclude(i => i.FridgeModel)  // CHANGED: FridgeUnit → FridgeModel
                .Include(po => po.Quotation)
                    .ThenInclude(q => q.RequestQuotation)
                .OrderByDescending(po => po.CreatedDate)
                .ToListAsync();

            ViewBag.PendingCount = allOrders.Count(po => po.Status == OrderStatus.Pending);
            ViewBag.ApprovedCount = allOrders.Count(po => po.Status == OrderStatus.Approved);
            ViewBag.DeliveredCount = allOrders.Count(po => po.Status == OrderStatus.Delivered);
            ViewBag.ShippedCount = allOrders.Count(po => po.Status == OrderStatus.Shipped);

            var deliveredOrders = allOrders
                .Where(po => po.Status == OrderStatus.Delivered)
                .OrderByDescending(po => po.CreatedDate)
                .ToList();

            return View(deliveredOrders);
        }

        public async Task<IActionResult> DeliveryNoteDetails(int id)
        {
            if (id == 0)
                return BadRequest();

            var deliveryNote = await context.DeliveryNotes
                .Include(dn => dn.PurchaseOrder)
                    .ThenInclude(po => po.Items)
                        .ThenInclude(i => i.FridgeModel)  // CHANGED: FridgeUnit → FridgeModel
                .Include(dn => dn.Supplier)
                .Include(dn => dn.ReceivedBy)
                .FirstOrDefaultAsync(dn => dn.Id == id);

            if (deliveryNote == null)
                return NotFound();

            string supplierEmail = null;
            if (!string.IsNullOrEmpty(deliveryNote.Supplier?.ApplicationUserId))
            {
                var supplierUser = await userManager.FindByIdAsync(deliveryNote.Supplier.ApplicationUserId);
                supplierEmail = supplierUser?.Email;
            }

            var viewModel = new DeliveryNoteViewModel
            {
                Id = deliveryNote.Id,
                PurchaseOrderId = deliveryNote.PurchaseOrderId,
                PONumber = deliveryNote.PurchaseOrder.PONumber,
                SupplierName = deliveryNote.Supplier?.Name,
                SupplierEmail = supplierEmail,
                SupplierPhone = deliveryNote.Supplier?.Phone,
                SupplierAddress = deliveryNote.Supplier?.Address,
                DeliveryAddress = deliveryNote.PurchaseOrder.DeliveryAddress,
                DeliveryTerms = deliveryNote.PurchaseOrder.DeliveryTerms,
                PaymentTerms = deliveryNote.PurchaseOrder.PaymentTerms,
                GeneratedDate = deliveryNote.GeneratedDate ?? deliveryNote.DeliveryDate,
                ExpectedDeliveryDate = deliveryNote.PurchaseOrder.ExpectedDeliveryDate,
                Status = deliveryNote.PurchaseOrder.Status.ToString(),
                ReceivedById = deliveryNote.ReceivedById,
                Items = deliveryNote.PurchaseOrder.Items.Select(i => new DeliveryItemViewModel
                {
                    FridgeModel = i.FridgeModel?.Name,  // CHANGED: FridgeUnit.FridgeModel.Name → FridgeModel.Name
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveDeliveryNote(int id)
        {
            var deliveryNote = await context.DeliveryNotes
                .Include(dn => dn.PurchaseOrder)
                    .ThenInclude(po => po.Items)
                        .ThenInclude(i => i.FridgeModel)  // CHANGED: FridgeUnit → FridgeModel
                .Include(dn => dn.Supplier)
                .FirstOrDefaultAsync(dn => dn.Id == id);

            if (deliveryNote == null)
                return NotFound();

            var purchaseOrder = deliveryNote.PurchaseOrder;

            purchaseOrder.Status = OrderStatus.Delivered;
            context.PurchaseOrders.Update(purchaseOrder);

            deliveryNote.IsApproved = true;
            deliveryNote.GeneratedDate = DateTime.Now;
            context.DeliveryNotes.Update(deliveryNote);

            // NOTE: When a delivery is approved, you might want to create FridgeUnits
            // based on the FridgeModels in the purchase order
            foreach (var item in purchaseOrder.Items)
            {
                if (item.FridgeModel != null)
                {
                    // Here you would create FridgeUnit instances based on the delivered FridgeModels
                    // For example:
                    for (int i = 0; i < item.Quantity; i++)
                    {
                        var newFridgeUnit = new FridgeUnit
                        {
                            SerialNumber = $"TEMP-{Guid.NewGuid().ToString().Substring(0, 8)}", // Generate temp serial
                            FridgeModelId = item.FridgeModelId,
                            Status = FridgeStatus.InWarehouse,
                            Condition = FridgeCondition.New,
                            PurchaseDate = DateTime.Now,
                            LocationId = 1, // Default location
                            TechnicianUserId = null
                        };
                        context.FridgeUnits.Add(newFridgeUnit);
                    }
                }
            }

            var liaisons = await userManager.GetUsersInRoleAsync("Inventory Liaison");
            foreach (var liaison in liaisons)
            {
                var notification = new Notification
                {
                    RecipientUserId = liaison.Id,
                    Title = "New Fridges Received",
                    Message = $"Fridges from Purchase Order {purchaseOrder.PONumber} have been received. Please complete their details in the inventory module.",
                    TargetController = "Inventory",
                    TargetAction = "PendingFridges",
                    CreatedAt = DateTime.Now,
                    IsRead = false
                };
                context.Notifications.Add(notification);
            }

            if (deliveryNote.Supplier != null && deliveryNote.Supplier.User != null)
            {
                var supplierUser = await userManager.FindByIdAsync(deliveryNote.Supplier.ApplicationUserId);
                if (supplierUser != null)
                {
                    var supplierNotification = new Notification
                    {
                        RecipientUserId = supplierUser.Id,
                        Title = "Delivery Approved",
                        Message = $"Your delivery for Purchase Order {purchaseOrder.PONumber} has been approved and stock is pending inventory processing.",
                        TargetController = "Supplier",
                        TargetAction = "ApprovedPurchaseOrders",
                        CreatedAt = DateTime.Now,
                        IsRead = false
                    };
                    context.Notifications.Add(supplierNotification);
                }
            }

            await context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Delivery Note {deliveryNote.Id} approved successfully. Fridge units created and awaiting inventory processing.";

            return RedirectToAction("AllPurchaseOrders", "PurchasingManager");
        }
        #endregion
    }
}