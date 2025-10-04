using Code_Vanta_Systems.Data;
using Code_Vanta_Systems.Models;
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
            var user = await _userManager.GetUserAsync(User);

            var requests = await _context.PurchaseRequests
                .Include(r => r.RequestedItems)
                .Where(r => r.RequestedByUserId == user.Id)
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();

            return View(requests);
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

            // ✅ Validate at least one item is selected
            if (model.Items == null || !model.Items.Any(i => i.ItemCatalogId > 0))
            {
                ModelState.AddModelError("", "You must select at least one item.");
            }

            if (!ModelState.IsValid)
            {
                // Re-populate dropdowns
                var allItems = await _context.ItemCatalogs
                    .Where(i => i.IsActive)
                    .Select(i => new SelectListItem { Value = i.Id.ToString(), Text = i.Name })
                    .ToListAsync();

                foreach (var item in model.Items)
                {
                    item.AvailableItems = allItems;
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
                Notes = model.Notes
            };

            // 🛒 Map selected items and auto-assign description from catalog
            foreach (var item in model.Items.Where(i => i.ItemCatalogId > 0))
            {
                var catalogItem = await _context.ItemCatalogs
                    .FirstOrDefaultAsync(i => i.Id == item.ItemCatalogId);

                if (catalogItem != null)
                {
                    request.RequestedItems.Add(new PurchaseRequestItem
                    {
                        ItemCatalogId = catalogItem.Id,
                        ModelRequested = catalogItem.Name,       // Automatically assign model/item name
                        Description = catalogItem.Description,   // Automatically assign description
                        Quantity = item.Quantity
                    });
                }
            }

            // Save request
            _context.PurchaseRequests.Add(request);
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
