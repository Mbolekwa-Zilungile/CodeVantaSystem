using Code_Vanta_Systems.Data;
using Code_Vanta_Systems.Models;
using Code_Vanta_Systems.Models.Enums;
using Code_Vanta_Systems.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Code_Vanta_Systems.Controllers
{
    [Authorize(Roles = "Purchasing Manager")]
    public class PurchasingManagerController : Controller
    {

        private readonly UserManager<ApplicationUser> userManager;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly ApplicationDbContext context;

        public PurchasingManagerController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            this.userManager = userManager;
            this.roleManager = roleManager;
            this.context = context;
        }
        // SUPPLIERS
        public async Task<IActionResult> Index()
        {
            var activeSuppliers = await context.Suppliers
            .ToListAsync();
            ViewBag.ActiveSupplierCount = activeSuppliers.Count;
            ViewBag.RequestCount = await context.PurchaseRequests.CountAsync();
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
            {
                return View(model);
            }

            // 1. Create ApplicationUser without password
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.ContactPerson,
                EmailConfirmed = true,
                PhoneNumber = model.Phone
            };

            var userCreationResult = await userManager.CreateAsync(user);

            if (userCreationResult.Succeeded)
            {
                // 2. Check role exists and assign "Supplier" role
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
                foreach (var error in userCreationResult.Errors)
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
    }

}