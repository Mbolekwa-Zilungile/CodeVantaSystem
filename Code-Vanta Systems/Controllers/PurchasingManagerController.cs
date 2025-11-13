using Code_Vanta_Systems.Data;
using Code_Vanta_Systems.Migrations;
using Code_Vanta_Systems.Models;
using Code_Vanta_Systems.Models.Enums;
using Code_Vanta_Systems.Services;
using Code_Vanta_Systems.ViewModels;
using Code_Vanta_Systems.ViewModels.DeliveryNote;
using Code_Vanta_Systems.ViewModels.PurchaseOrder;
using Code_Vanta_Systems.ViewModels.SupplierQoutation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
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
        // GET: Admin/CreateSupplier
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
            // Use a strong default password
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
                    // 3. Create Supplier record
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
                    return RedirectToAction("Suppliers"); // Adjust this action to your supplier list page
                }

                // Cleanup user if role assignment fails
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

            // 1. Perform soft delete
            supplier.IsActive = false;
            context.Suppliers.Update(supplier);

            // 2. Lock user account
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
                return RedirectToAction("Suppliers"); // or wherever your suppliers list is
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
                // Update user info
                user.FullName = model.ContactPerson;
                user.Email = model.Email;
                user.UserName = model.Email; // Usually username = email
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

                // Update supplier info
                supplier.Name = model.CompanyName;
                supplier.Phone = model.PhoneNumber;
                supplier.Address = model.Address;


                // Lock/unlock account based on active status


                context.Suppliers.Update(supplier);
                await context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Supplier '{supplier.Name}' updated successfully.";
                return RedirectToAction("Suppliers"); // or your suppliers list
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
                return RedirectToAction(nameof(InactiveSuppliers)); // Adjust as needed
            }

            var user = supplier.User;

            // 1. Reverse the soft delete
            supplier.IsActive = true;

            // 2. Remove the lockout (Re-enables login)
            await userManager.SetLockoutEndDateAsync(user, null);

            // 3. Re-assign the supplier's role (e.g., "Supplier")
            const string roleName = "Supplier"; // Or supplier.Role if stored in DB
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
            return RedirectToAction(nameof(Suppliers)); // Adjust to your active suppliers list
        }
        #endregion

        #region Purchase Request
        //PURCHASE REQUEST
        // Optional: list all purchase requests for current user or all (for manager)
        public async Task<IActionResult> PurchaseRequests()
        {
            var SubmitedRequests = await context.PurchaseRequests
               .Include(pr => pr.RequestedBy)
               .Include(pr => pr.RequestedItems)
               .Where(pr => pr.Status == Code_Vanta_Systems.Models.Enums.RequestStatus.Submitted)
               .OrderByDescending(pr => pr.RequestDate)
               .ToListAsync();

            ViewBag.PendingCount = ViewBag.RejectedRequests = await context.PurchaseRequests
               .CountAsync(s => s.Status == RequestStatus.Pending);
            ViewBag.SubmitedCount = SubmitedRequests.Count;
            ViewBag.RejectedRequests = await context.PurchaseRequests
                .CountAsync(s => s.Status == RequestStatus.Rejected);

            ViewBag.ApprovedRequestsCount = await context.PurchaseRequests
                .CountAsync(s => s.Status == RequestStatus.Approved);
            ViewBag.CompletedRequestsCount = await context.PurchaseRequests
               .CountAsync(s => s.Status == RequestStatus.Completed);
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

            ViewBag.PendingCount = ViewBag.RejectedRequests = await context.PurchaseRequests
               .CountAsync(s => s.Status == RequestStatus.Pending);
            ViewBag.SubmitedCount = ViewBag.RejectedRequests = await context.PurchaseRequests
                .CountAsync(s => s.Status == RequestStatus.Submitted);

            ViewBag.RejectedRequests = await context.PurchaseRequests
                .CountAsync(s => s.Status == RequestStatus.Rejected);

            ViewBag.ApprovedRequestsCount = await context.PurchaseRequests
                .CountAsync(s => s.Status == RequestStatus.Approved);
            ViewBag.CompletedRequestsCount = await context.PurchaseRequests
               .CountAsync(s => s.Status == RequestStatus.Completed);
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

            ViewBag.PendingCount = ViewBag.RejectedRequests = await context.PurchaseRequests
               .CountAsync(s => s.Status == RequestStatus.Pending);
            ViewBag.SubmitedCount = ViewBag.RejectedRequests = await context.PurchaseRequests
                .CountAsync(s => s.Status == RequestStatus.Submitted);

            ViewBag.RejectedRequests = await context.PurchaseRequests
                .CountAsync(s => s.Status == RequestStatus.Rejected);

            ViewBag.ApprovedRequestsCount = await context.PurchaseRequests
                .CountAsync(s => s.Status == RequestStatus.Approved);
            ViewBag.CompletedRequestsCount = await context.PurchaseRequests
               .CountAsync(s => s.Status == RequestStatus.Completed);
            return View(purchaseRequests);
        }


        // GET: PurchaseRequest/CreatePurchaseRequest
        public IActionResult CreatePurchaseRequest()
        {
            return View();
        }

        public async Task<IActionResult> Details(int id)
        {
            var request = await context.PurchaseRequests
                         .Include(r => r.RequestedBy)
                         .Include(r => r.RequestedItems)
                         /*.ThenInclude(i => i.ItemCatalog) */// <--- This is correct for eager loading
                         .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null) return NotFound();

            return View(request);
        }

        //  GET: Edit Purchase Request
        [HttpGet]
        public async Task<IActionResult> EditRequest(int id)
        {
            // 1. Fetch the existing Purchase Request and its line items
            var request = await context.PurchaseRequests
                .Include(r => r.RequestedItems)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
                return NotFound();

            // 2. Get all active fridges for dropdown lists
            var fridges = await context.Fridges
                 .Where(f => !f.IsDeleted)
                 .Select(f => new SelectListItem
                 {
                     Value = f.Id.ToString(),
                     Text = $"{f.Model} ({f.StockQuantity} in stock)"
                 })
                 .ToListAsync();

            // 3. Map the entity to the view model
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
                    FridgeId = i.FridgeId,
                    Quantity = i.Quantity,
                    AvailableItems = fridges
                }).ToList()
            };

            model.Items.Add(new PurchaseRequestItemViewModel { AvailableItems = fridges });

            ViewBag.Fridges = fridges;
            return View(model);
        }

        // POST: Handle the form submission for editing
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRequest(EditPurchaseRequestViewModel model)
        {
            // 1. Validate form data
            if (!ModelState.IsValid)
            {
                // On failure, reload fridges to repopulate dropdowns
                var fridges = await context.Fridges
                     .Where(f => !f.IsDeleted)
                     .Select(f => new SelectListItem
                     {
                         Value = f.Id.ToString(),
                         Text = $"{f.Model} ({f.StockQuantity} in stock)"
                     })
                     .ToListAsync();

                foreach (var item in model.Items)
                    item.AvailableItems = fridges;

                ViewBag.Fridges = fridges;
                return View(model);
            }

            // 2. Fetch the existing request with items
            var request = await context.PurchaseRequests
                .Include(r => r.RequestedItems)
                .FirstOrDefaultAsync(r => r.Id == model.Id);

            if (request == null) return NotFound();

            // 3. Update main request fields
            request.ExpectedDate = model.ExpectedDate;
            request.Priority = model.Priority;
            request.Status = Code_Vanta_Systems.Models.Enums.RequestStatus.Submitted;
            request.Notes = model.Notes;

            // 4. Manage line items (Delete, Update, Add)

            // Identify which existing items were removed from the form
            var itemIdsFromForm = model.Items
                .Where(i => i.PurchaseRequestItemId.HasValue)
                .Select(i => i.PurchaseRequestItemId.Value)
                .ToList();
            var itemsToRemove = request.RequestedItems
                .Where(i => !itemIdsFromForm.Contains(i.Id))
                .ToList();
            context.PurchaseRequestItems.RemoveRange(itemsToRemove);

            // Process all items submitted in the form
            foreach (var itemVm in model.Items.Where(i => i.FridgeId > 0 && i.Quantity >= 1))
            {
                // Fetch the details of the selected Fridge (needed for ModelRequested/Description)
                var fridge = await context.Fridges.FindAsync(itemVm.FridgeId);

                if (fridge == null) continue;

                if (itemVm.PurchaseRequestItemId.HasValue)
                {
                    // A. Update Existing Item
                    var existingItem = request.RequestedItems.First(i => i.Id == itemVm.PurchaseRequestItemId.Value);

                    existingItem.FridgeId = fridge.Id;
                    existingItem.ModelRequested = fridge.Model;
                    existingItem.Description = fridge.Description;
                    existingItem.Quantity = itemVm.Quantity;
                }
                else
                {
                    // B. Add New Item
                    var newItem = new PurchaseRequestItem
                    {
                        PurchaseRequestId = request.Id,
                        FridgeId = fridge.Id,
                        ModelRequested = fridge.Model,
                        Description = fridge.Description,
                        Quantity = itemVm.Quantity
                    };
                    context.PurchaseRequestItems.Add(newItem);
                }
            }

            // 5. Save all changes (Request updates, item removals, item updates, item additions)
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
            // 1️⃣ Load the purchase request
            var request = await context.PurchaseRequests
                .Include(r => r.RequestedBy)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
                return NotFound();

            // 2️⃣ Update status to Rejected
            request.Status = Models.Enums.RequestStatus.Rejected;
            request.Notes += string.IsNullOrEmpty(reason) ? "" : $"\nRejection Reason: {reason}";

            // 3️⃣ Save changes
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
                // ✅ 2. Create notification
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


        // GET: Create RFQ page
        public async Task<IActionResult> CreateRFQ(int id)
        {
            var purchaseRequest = await context.PurchaseRequests
                .Include(r => r.RequestedItems)
                    .ThenInclude(ri => ri.Fridge)  // Load the Fridge entity for each requested item
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
                    FridgeId = item.FridgeId,
                    Quantity = item.Quantity,
                    FridgeModel = item.Fridge?.Model ?? "Unknown"  // <-- Assign fridge model here safely
                }).ToList()
            };

            return View(viewModel);
        }


        // POST: Handle RFQ submission
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRFQ(RQFViewModel model)
        {
            // Validate supplier selection
            if (model.SelectedSupplierIds == null || !model.SelectedSupplierIds.Any())
            {
                ModelState.AddModelError(nameof(model.SelectedSupplierIds), "Please select at least one supplier.");
            }

            if (!ModelState.IsValid)
            {
                // Reload suppliers list
                model.Suppliers = await context.Suppliers
                    .Include(s => s.User)
                    .Where(s => s.IsActive)
                    .ToListAsync();

                // Reload fridge dropdown items
                var allFridges = await context.Fridges
                    .Where(f => !f.IsDeleted)
                    .Select(f => new SelectListItem
                    {
                        Value = f.Id.ToString(),
                        Text = $"{f.Model} ({f.StockQuantity} in stock)"
                    })
                    .ToListAsync();

                foreach (var item in model.PurchaseRequestItems)
                {
                    item.AvailableItems = allFridges;
                }

                return View(model);
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var purchaseRequest = await context.PurchaseRequests
       .FirstOrDefaultAsync(r => r.Id == model.PurchaseRequestId);

            if (purchaseRequest != null)
            {
                purchaseRequest.Status = RequestStatus.Approved; // enum or string
                context.PurchaseRequests.Update(purchaseRequest);
            }


            // Get the latest RFQ number once
            string lastRFQId = await context.RequestQuotations
                .OrderByDescending(r => r.RequestQuotationId)
                .Select(r => r.RFQId)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (!string.IsNullOrEmpty(lastRFQId))
            {
                var parts = lastRFQId.Split('-'); // e.g., "RFQ-2025-004"
                if (parts.Length == 3 && int.TryParse(parts[2], out int lastNum))
                {
                    nextNumber = lastNum + 1;
                }
            }

            // Loop through suppliers and create RFQs + notifications
            foreach (var supplierId in model.SelectedSupplierIds)
            {
                string newRFQId = $"RFQ-{DateTime.Now.Year}-{nextNumber:000}";
                nextNumber++;

                // Create RFQ
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

                // Create Notification for Supplier
                var supplier = await context.Suppliers
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.Id == supplierId);

                if (supplier != null && supplier.User != null)
                {
                    var notification = new Notification
                    {
                        RecipientUserId = supplier.User.Id,  // Use correct property
                        Title = "New RFQ Created",
                        Message = $"A new RFQ ({newRFQId}) has been created for you. Please respond.",
                        TargetController = "Supplier",
                        TargetAction = "RFQ" // Replace with actual action for viewing RFQ
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
                        // Log the email failure
                        Console.WriteLine($"Failed to send email to {supplier.User.Email}: {ex.Message}");
                    }
                }
            }

            await context.SaveChangesAsync();

            return RedirectToAction("RFQ", "PurchasingManager", new { id = model.PurchaseRequestId });
        }

        public async Task<IActionResult> RFQDetails(int id)
        {
            // Fetch the RFQ with related data
            var rfq = await context.RequestQuotations
                .Include(r => r.Supplier)
                    .ThenInclude(s => s.User)
                .Include(r => r.PurchaseRequest)
                    .ThenInclude(pr => pr.RequestedItems)
                        .ThenInclude(ri => ri.Fridge)
                .Include(r => r.RequestedBy)
                .FirstOrDefaultAsync(r => r.RequestQuotationId == id);

            if (rfq == null)
            {
                return NotFound();
            }

            // Map to a view model
            var viewModel = new RFQDetailViewModel
            {
                RFQId = rfq.RFQId,
                RequestDate = rfq.RequestDate,
                ExpectedDate = rfq.PurchaseRequest?.ExpectedDate,
                Notes = rfq.Notes,
                // Supplier Info
                SupplierName = rfq.Supplier?.Name ?? rfq.Supplier?.ContactName,
                SupplierEmail = rfq.Supplier?.User?.Email,
                SupplierPhone = rfq.Supplier?.Phone,
                SupplierAddress = rfq.Supplier?.Address,
                // Requested By (who issued the RFQ)
                IssuedByName = rfq.RequestedBy?.UserName ?? "Unknown",
                // Requested Items
                Items = rfq.PurchaseRequest?.RequestedItems.Select(ri => new RFQItemViewModel
                {
                    FridgeModel = ri.Fridge?.Model ?? "Unknown",
                    Quantity = ri.Quantity
                }).ToList() ?? new List<RFQItemViewModel>()
            };
            return View(viewModel);
        }

        #endregion RFQ

        #region Supplier Qoutation 

        public async Task<IActionResult> SupplerQuotations()
        {
            // Full counts (from all quotations)
            var allQuotations = await context.Quotations.ToListAsync();
            ViewBag.PendingCount = allQuotations.Count(q => q.Status == "Pending");
            ViewBag.ApprovedCount = allQuotations.Count(q => q.Status == "Approved");
            ViewBag.RejectedCount = allQuotations.Count(q => q.Status == "Rejected");
            ViewBag.sentCount = allQuotations.Count(q => q.Status == "Sent");

            // Only pending quotations for this page
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
            // Fetch purchase requests with RequestNumber only
            var purchaseRequests = await context.PurchaseRequests
                .Select(pr => new { pr.Id, pr.RequestNumber }) // only RequestNumber
                .ToListAsync();

            // Use ViewBag as SelectList for Razor binding
            ViewBag.PurchaseRequests = new SelectList(purchaseRequests, "Id", "RequestNumber");

            return View();
        }

        // POST: Fetch quotations for selected RequestNumber
        [HttpPost]
        public async Task<IActionResult> SelectPurchaseRequest(int purchaseRequestId)
        {
            if (purchaseRequestId == 0)
            {
                ModelState.AddModelError("", "Please select a purchase request.");
                return RedirectToAction(nameof(SelectPurchaseRequest));
            }

            // Get the purchase request number for the selected ID
            var purchaseRequest = await context.PurchaseRequests
                .FirstOrDefaultAsync(pr => pr.Id == purchaseRequestId);

            if (purchaseRequest == null)
                return NotFound("Purchase request not found.");

            // Fetch only pending quotations
            var quotations = await context.Quotations
                .Where(q => q.PurchaseRequestId == purchaseRequestId && q.Status == "Pending")
                .Include(q => q.RequestQuotation)
                .Include(q => q.Supplier)
                .Include(q => q.Items)
                .ToListAsync();

            // Pass the request number to the view
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

            // Set the RequestNumber for header
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

            // Get all quotations for the same purchase request
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

                    // ✅ Add notification for approved supplier
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

                    // ✅ Try sending approval email
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
                        // Log error (optional)
                        Console.WriteLine($"Email sending failed: {ex.Message}");
                    }
                }
                else
                {
                    q.Status = "Rejected";

                    // ✅ Add notification for rejected suppliers
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

                    // ✅ Try sending rejection email
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
                        // Log error (optional)
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

        #region Purchase order

        [HttpGet]
        public async Task<IActionResult> CreatePurchaseOrder(int quotationId)
        {
            // 1. Retrieve the Quotation with all necessary related data (Supplier, Items, etc.)
            var quotation = await context.Quotations
                .Include(q => q.Supplier)
                    .ThenInclude(s => s.User)
                .Include(q => q.RequestQuotation)
                    .ThenInclude(rq => rq.PurchaseRequest)
                .Include(q => q.Items)
                    .ThenInclude(i => i.PurchaseRequestItem)
                .FirstOrDefaultAsync(q => q.Id == quotationId);

            if (quotation == null)
                return NotFound();

            var company = await context.Company.FirstOrDefaultAsync();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await context.Users.FindAsync(userId);
            var roles = await userManager.GetRolesAsync(user);

            // 2. Map data to the PurchaseOrderCreateViewModel
            var vm = new PurchaseOrderCreateViewModel
            {
                QuotationId = quotation.Id,
                QuotationNumber = quotation.QuotationNumber, // Note: This might be null if not set yet
                SupplierId = quotation.SupplierId,
                SupplierName = quotation.Supplier.Name,
                SupplierEmail = quotation.Supplier.User?.Email,
                SupplierPhone = quotation.Supplier.Phone,
                SupplierAddress = quotation.Supplier.Address,

                CompanyName = company?.Name,
                CompanyEmail = company?.Email,
                CompanyPhone = company?.Phone,
                CompanyAddress = company?.Address,

                // ExpectedDate is pulled from RequestQuotation/PurchaseRequest
                ExpectedDeliveryDate = quotation.RequestQuotation?.ExpectedDeliveryDate,
                DeliveryAddress = "CoolTech Warehouse, 124 Logistic Ave, Johannesburg",
                DeliveryTerms = "Free on Board (FOB) Johannesburg",
                PaymentTerms = "30 Days after Delivery",

                // **CRITICAL:** Mapping the quotation items to PO items for the view model
                Items = quotation.Items.Select(i => new PurchaseOrderItemViewModel
                {
                    // Note: FridgeId is required for the PO Item entity
                    FridgeId = i.PurchaseRequestItem.FridgeId,
                    ItemName = i.FridgeModel, // Display name
                    Quantity = i.QuotedQuantity, // Use quoted quantity
                    UnitPrice = i.QuotedUnitPrice // Use quoted unit price
                }).ToList(),

                ManagerName = user?.FullName,
                ManagerEmail = user?.Email,
                ManagerRole = string.Join(", ", roles)
            };

            return View(vm); // show confirmation form
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePurchaseOrder(PurchaseOrderCreateViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Generate PO number
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
                    FridgeId = item.FridgeId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                });
            }

            context.PurchaseOrders.Add(purchaseOrder);

            // Update quotation status
            var quotation = await context.Quotations.FindAsync(model.QuotationId);
            if (quotation != null)
            {
                quotation.Status = "Sent";
                context.Quotations.Update(quotation);
            }

            await context.SaveChangesAsync(); // Save PO and quotation status

            // ---- Notification ----
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

            // ---- Email ----
            var supplierEmail = await context.Suppliers
                .Where(s => s.Id == model.SupplierId)
                .Select(s => s.User.Email)
                .FirstOrDefaultAsync();


            return RedirectToAction("AllPurchaseOrders");
        }

        public async Task<IActionResult> PurchaseOrderDetails(int id)
        {
            var purchaseOrder = await context.PurchaseOrders
     .Include(po => po.Supplier)
         .ThenInclude(s => s.User)
     .Include(po => po.Items)
         .ThenInclude(i => i.Fridge)
     .Include(po => po.Quotation)
         .ThenInclude(q => q.RequestQuotation)
     .Include(po => po.DeliveryNotes) // Include delivery notes
     .FirstOrDefaultAsync(po => po.PurchaseOrderId == id);

            if (purchaseOrder == null)
                return NotFound();

            // Total amount calculated via Items
            ViewBag.TotalAmount = purchaseOrder.Items?.Sum(i => i.Quantity * i.UnitPrice) ?? 0;

            // Expected delivery date from RequestQuotation
            ViewBag.ExpectedDeliveryDate = purchaseOrder.Quotation?.RequestQuotation?.ExpectedDeliveryDate;

            return View(purchaseOrder);
        }

        public async Task<IActionResult> AllPurchaseOrders()
        {
            var allOrders = await context.PurchaseOrders
       .Include(po => po.Supplier)
           .ThenInclude(s => s.User) // Supplier info
       .Include(po => po.Items)
           .ThenInclude(i => i.Fridge) // Fridge details
       .Include(po => po.Quotation)
           .ThenInclude(q => q.RequestQuotation) // Include request quotation
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
           .ThenInclude(s => s.User) // Supplier info
       .Include(po => po.Items)
           .ThenInclude(i => i.Fridge) // Fridge details
       .Include(po => po.Quotation)
           .ThenInclude(q => q.RequestQuotation) // Include request quotation
       .OrderByDescending(po => po.CreatedDate)
       .ToListAsync();

            ViewBag.PendingCount = allOrders.Count(po => po.Status == OrderStatus.Pending);
            ViewBag.ApprovedCount = allOrders.Count(po => po.Status == OrderStatus.Approved);
            ViewBag.DeliveredCount = allOrders.Count(po => po.Status == OrderStatus.Delivered);
            ViewBag.ShippedCount = allOrders.Count(po => po.Status == OrderStatus.Shipped);

            var pendingOrders = allOrders
                .Where(po => po.Status == OrderStatus.Approved)
                .OrderByDescending(po => po.CreatedDate)
                .ToList();

            return View(pendingOrders);

        }
        public async Task<IActionResult> ShippedPurchaseOrders()
        {
            var allOrders = await context.PurchaseOrders
       .Include(po => po.Supplier)
           .ThenInclude(s => s.User) // Supplier info
       .Include(po => po.Items)
           .ThenInclude(i => i.Fridge) // Fridge details
       .Include(po => po.Quotation)
           .ThenInclude(q => q.RequestQuotation) // Include request quotation
       .OrderByDescending(po => po.CreatedDate)
       .ToListAsync();

            ViewBag.PendingCount = allOrders.Count(po => po.Status == OrderStatus.Pending);
            ViewBag.ApprovedCount = allOrders.Count(po => po.Status == OrderStatus.Approved);
            ViewBag.DeliveredCount = allOrders.Count(po => po.Status == OrderStatus.Delivered);
            ViewBag.ShippedCount = allOrders.Count(po => po.Status == OrderStatus.Shipped);

            var pendingOrders = allOrders
                .Where(po => po.Status == OrderStatus.Shipped)
                .OrderByDescending(po => po.CreatedDate)
                .ToList();

            return View(pendingOrders);

        }

        public async Task<IActionResult> DeliveredPurchaseOrders()
        {
            var allOrders = await context.PurchaseOrders
       .Include(po => po.Supplier)
           .ThenInclude(s => s.User) // Supplier info
       .Include(po => po.Items)
           .ThenInclude(i => i.Fridge) // Fridge details
       .Include(po => po.Quotation)
           .ThenInclude(q => q.RequestQuotation) // Include request quotation
       .OrderByDescending(po => po.CreatedDate)
       .ToListAsync();

            ViewBag.PendingCount = allOrders.Count(po => po.Status == OrderStatus.Pending);
            ViewBag.ApprovedCount = allOrders.Count(po => po.Status == OrderStatus.Approved);
            ViewBag.DeliveredCount = allOrders.Count(po => po.Status == OrderStatus.Delivered);
            ViewBag.ShippedCount = allOrders.Count(po => po.Status == OrderStatus.Shipped);

            var pendingOrders = allOrders
                .Where(po => po.Status == OrderStatus.Delivered)
                .OrderByDescending(po => po.CreatedDate)
                .ToList();

            return View(pendingOrders);

        }
        public async Task<IActionResult> DeliveryNoteDetails(int id)
        {
            if (id == 0)
                return BadRequest();

            var deliveryNote = await context.DeliveryNotes
                .Include(dn => dn.PurchaseOrder)
                    .ThenInclude(po => po.Items)
                        .ThenInclude(i => i.Fridge)
                .Include(dn => dn.Supplier) // Supplier who created the delivery note
                .Include(dn => dn.ReceivedBy)
                .FirstOrDefaultAsync(dn => dn.Id == id);

            if (deliveryNote == null)
                return NotFound();

            // Get Supplier email from ApplicationUser
            string supplierEmail = null;
            if (!string.IsNullOrEmpty(deliveryNote.Supplier?.ApplicationUserId))
            {
                var supplierUser = await userManager.FindByIdAsync(deliveryNote.Supplier.ApplicationUserId);
                supplierEmail = supplierUser?.Email;
            }

            // Map to ViewModel
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
                    FridgeModel = i.Fridge?.Model,
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
            // Load the delivery note and related entities
            var deliveryNote = await context.DeliveryNotes
                .Include(dn => dn.PurchaseOrder)
                    .ThenInclude(po => po.Items)
                        .ThenInclude(i => i.Fridge)
                .Include(dn => dn.Supplier)
                .FirstOrDefaultAsync(dn => dn.Id == id);

            if (deliveryNote == null)
                return NotFound();

            var purchaseOrder = deliveryNote.PurchaseOrder;

            // 1. Update PO status
            purchaseOrder.Status = OrderStatus.Delivered;
            context.PurchaseOrders.Update(purchaseOrder);

            // 2. Approve the delivery note
            deliveryNote.IsApproved = true;
            deliveryNote.GeneratedDate = DateTime.Now;
            context.DeliveryNotes.Update(deliveryNote);

            // 3. Update fridge models AND auto-generate units
            foreach (var item in purchaseOrder.Items)
            {
                var fridge = item.Fridge;
                if (fridge == null) continue;

                // Keep original stock before update (needed for serial numbering)
                int previousStock = fridge.StockQuantity;

                // Add delivered stock
                fridge.StockQuantity += item.Quantity;

                // Ensure fridge fields are populated
                fridge.PurchaseDate = fridge.PurchaseDate == default ? DateTime.Now : fridge.PurchaseDate;
                fridge.ImageUrl ??= "/images/fridge-placeholder.png";
                fridge.Description ??= "Awaiting Inventory Liaison completion.";
                fridge.Condition = FridgeCondition.New;
                fridge.Status = FridgeStatus.Pending;
                fridge.LocationId = fridge.LocationId == 0 ? 1 : fridge.LocationId;
                fridge.IsDeleted = false;

                context.Fridges.Update(fridge);

                // AUTO-GENERATE FRIDGE UNITS
                for (int i = 1; i <= item.Quantity; i++)
                {
                    var unit = new Code_Vanta_Systems.Models.FridgeUnit
                    {
                        FridgeId = fridge.Id,

                        // SERIAL: e.g. ABC123-006
                        SerialNumber = $"{fridge.SerialNumber}-{(previousStock + i):D3}",

                        Condition = fridge.Condition,
                        Status = fridge.Status,
                    };

                    context.FridgeUnits.Add(unit);
                }
            }

            // 4. Notify Inventory Liaisons
            var liaisons = await userManager.GetUsersInRoleAsync("InventoryLiaison");
            foreach (var liaison in liaisons)
            {
                var notification = new Notification
                {
                    RecipientUserId = liaison.Id,
                    Title = "Fridges Awaiting Completion",
                    Message = $"Fridges from Purchase Order {purchaseOrder.PONumber} have been updated. Please complete their details.",
                    TargetController = "Inventory",
                    TargetAction = "PendingFridges",
                    CreatedAt = DateTime.Now,
                    IsRead = false
                };
                context.Notifications.Add(notification);
            }

            // 5. Notify Supplier
            if (deliveryNote.Supplier?.User != null)
            {
                var supplierNotification = new Notification
                {
                    RecipientUserId = deliveryNote.Supplier.User.Id,
                    Title = "Delivery Approved",
                    Message = $"Your delivery for Purchase Order {purchaseOrder.PONumber} has been approved.",
                    TargetController = "Supplier",
                    TargetAction = "ApprovedPurchaseOrders",
                    CreatedAt = DateTime.Now,
                    IsRead = false
                };

                context.Notifications.Add(supplierNotification);
            }

            // 6. Save all changes
            await context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"Delivery Note {deliveryNote.Id} approved. Stock updated and fridge units created.";

            return RedirectToAction("AllPurchaseOrders", "PurchasingManager");
        }


      
        #endregion
    }

}
