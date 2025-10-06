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
    [Authorize(Roles = "Inventory Liaison")]
    public class InventoryLiaisonController : Controller
    {

        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public InventoryLiaisonController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        public IActionResult Index()
        {
            return View();
        }
        // 🗂️ Display all purchase requests created by this liaison
      
        public async Task<IActionResult> PurchaseRequests()
        {
            var purchaseRequests = await _context.PurchaseRequests
                .Include(pr => pr.RequestedBy)
                .Include(pr => pr.RequestedItems)
                .Where(pr => pr.Status == RequestStatus.Submited)
                .OrderByDescending(pr => pr.RequestDate)
                .ToListAsync();
            ViewBag.SubmitedCount = purchaseRequests.Count;

            ViewBag.RejectedRequests = await _context.PurchaseRequests
                .CountAsync(s => s.Status == RequestStatus.Rejected);

            ViewBag.PendingRequestsCount = await _context.PurchaseRequests
                .CountAsync(s => s.Status == RequestStatus.Pending);

            ViewBag.CompletedRequestsCount = await _context.PurchaseRequests
                .CountAsync(s => s.Status == RequestStatus.Approved);
            return View(purchaseRequests);
        }

        public async Task<IActionResult> RejectedPurchaseRequests()
        {
            var purchaseRequests = await _context.PurchaseRequests
                .Include(pr => pr.RequestedBy)
                .Include(pr => pr.RequestedItems)
                .Where(pr => pr.Status == RequestStatus.Rejected)
                .OrderByDescending(pr => pr.RequestDate)
                .ToListAsync();

            return View(purchaseRequests);
        }

        // ✍️ GET: Create new purchase request
        [HttpGet]
        public async Task<IActionResult> Create()
        {
               var items = await _context.ItemCatalogs
                .Select(i => new SelectListItem { Value = i.Id.ToString(), Text = i.Name })
                .ToListAsync();

            var model = new CreatePurchaseRequestViewModel
            {
                Items = new List<PurchaseRequestItemViewModel>
        {
            new PurchaseRequestItemViewModel { AvailableItems = items }
        }
            };

            return View(model);
        }

        // ✍️ POST: Create new purchase request
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreatePurchaseRequestViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);

            // ✅ Validate priority
            if (model.Priority == null)
            {
                ModelState.AddModelError(nameof(model.Priority), "Priority is required.");
            }

            if (model.ExpectedDate.HasValue && model.ExpectedDate.Value.Date < DateTime.Now.Date)
            {
                ModelState.AddModelError("ExpectedDate", "Expected date cannot be in the past.");
            }

            // ✅ Validate at least one item is selected
            if (model.Items == null || !model.Items.Any(i => i.ItemCatalogId > 0))
            {
                ModelState.AddModelError("", "You must select at least one item.");
            }

            if (!ModelState.IsValid)
            {
                var allItems = await _context.ItemCatalogs
                    .Where(i => i.IsActive)
                    .Select(i => new SelectListItem { Value = i.Id.ToString(), Text = i.Name })
                    .ToListAsync();

                // Repopulate AvailableItems for all rows
                if (model.Items != null)
                {
                    foreach (var item in model.Items)
                    {
                        item.AvailableItems = allItems;
                    }
                }

                return View(model);
            }

            // 🛒 Create purchase request
            var request = new PurchaseRequest
            {
                RequestedByUserId = user.Id,
                RequestDate = DateTime.Now,
                ExpectedDate = model.ExpectedDate,
                Priority = model.Priority,
                Notes = model.Notes,
                Status=Models.Enums.RequestStatus.Submited,
               // RequestedItems = new List<PurchaseRequestItem>() // 👈 IMPORTANT
            };

            // Add request first to generate ID
            _context.PurchaseRequests.Add(request);
            await _context.SaveChangesAsync(); // now request.Id is available

            // 🛒 Map selected items and auto-assign description from catalog
            foreach (var item in model.Items.Where(i => i.ItemCatalogId > 0))
            {
                var catalogItem = await _context.ItemCatalogs.FindAsync(item.ItemCatalogId);
                if (catalogItem != null)
                {
                    var requestItem = new PurchaseRequestItem
                    {
                        PurchaseRequestId = request.Id, // explicitly set FK
                        ItemCatalogId = catalogItem.Id,
                        ModelRequested = catalogItem.Name,
                        Description = catalogItem.Description,
                        Quantity = item.Quantity
                    };
                    _context.PurchaseRequestItems.Add(requestItem);
                }
            }

            await _context.SaveChangesAsync();

            // 🔔 Notify Purchasing Manager
            var purchasingManager = (await _userManager.GetUsersInRoleAsync("PurchasingManager")).FirstOrDefault();
            if (purchasingManager != null)
            {
                _context.Notifications.Add(new Notification
                {
                    RecipientUserId = purchasingManager.Id,
                    Message = $"New purchase request #{request.Id} submitted by {user.UserName}.",
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Purchase request submitted successfully!";
            return RedirectToAction(nameof(Index));
        }

    }
}
