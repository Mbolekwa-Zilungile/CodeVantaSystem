using Code_Vanta_Systems.Data;
using Code_Vanta_Systems.Models;
using Code_Vanta_Systems.ViewModels;
using Code_Vanta_Systems.ViewModels.SupplierQoutation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace Code_Vanta_Systems.Controllers
{
    public class SupplierController : Controller
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly ApplicationDbContext context;

        public SupplierController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            this.userManager = userManager;
            this.roleManager = roleManager;
            this.context = context;
        }

        public async Task<IActionResult> Index()
        {
            if (User.IsInRole("Supplier"))
            {
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (currentUserId == null)
                    return RedirectToAction("Login", "Account");

                var supplier = await context.Suppliers
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.User.Id == currentUserId);

                if (supplier == null)
                {
                    return Unauthorized(); // Or redirect with a message
                }

                var rfqs = await context.RequestQuotations
               .Include(r => r.Supplier)
                   .ThenInclude(s => s.User)
               .Include(r => r.RequestedBy)
               .Where(r => r.SupplierId == supplier.Id) // filter by supplier
               .OrderByDescending(r => r.RequestDate)
               .ToListAsync();

                ViewBag.RFQCount = rfqs.Count;
                // Fetch the latest 5 notifications for the logged-in supplier
                var notifications = await context.Notifications
                    .Where(n => n.RecipientUserId == currentUserId)
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(5)
                    .ToListAsync();

                // Send the notifications to the view
                ViewBag.LatestNotifications = notifications;

                return View(notifications); // optional: pass directly to view
            }

            // If not a supplier, redirect to dashboard or show an access denied page
            return RedirectToAction("AccessDenied", "Account");
        }

        #region RFQ

        public async Task<IActionResult> RFQ()
        {
            // Get the current logged-in user's ID
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Retrieve the Supplier entity associated with this user
            var supplier = await context.Suppliers
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.User.Id == userId);

            if (supplier == null)
            {
                return Unauthorized(); // Or redirect with a message
            }

            // Get only RFQs for this supplier
            var rfqs = await context.RequestQuotations
                .Include(r => r.Supplier)
                    .ThenInclude(s => s.User)
                .Include(r => r.RequestedBy)
                .Where(r => r.SupplierId == supplier.Id) // filter by supplier
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

        public async Task<IActionResult> SupplierRFQDetails(int id)
        {
            // Get current supplier
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var supplier = await context.Suppliers
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.User.Id == userId);

            if (supplier == null)
                return Unauthorized();

            // Get the RFQ with supplier info, requested by user, and linked PurchaseRequest
            var rfq = await context.RequestQuotations
                .Include(r => r.Supplier)
                    .ThenInclude(s => s.User)
                .Include(r => r.RequestedBy)
                .Include(r => r.Quotations) // existing quotations for this RFQ
                .FirstOrDefaultAsync(r => r.RequestQuotationId == id && r.SupplierId == supplier.Id);

            if (rfq == null)
                return NotFound("RFQ not found.");

            // Get related PurchaseRequest with items
            var purchaseRequest = await context.PurchaseRequests
                .Include(pr => pr.RequestedItems)
                    .ThenInclude(ri => ri.Fridge)
                .Include(pr => pr.RequestedBy)
                .FirstOrDefaultAsync(pr => pr.Id == rfq.PurchaseRequestId);

            if (purchaseRequest == null)
                return NotFound("Purchase Request not found.");

            // Get company info (assuming single company)
            var company = await context.Company.FirstOrDefaultAsync();

            // Map items to view model
            var items = purchaseRequest.RequestedItems.Select(i => new RFQItemViewModel
            {
                FridgeId = i.FridgeId,
                FridgeModel = i.Fridge?.Model ?? "Unknown",
                Quantity = i.Quantity
            }).ToList();

            // Map quotations to a view model if needed
            var quotations = rfq.Quotations.Select(q => new QuotationSummaryViewModel
            {
                QuotationId = q.Id,
                SupplierName = q.Supplier.Name,
                DateReceived = q.DateReceived,
                // Add other fields you want to show
            }).ToList();

            // Prepare the RFQ detail view model
            var vm = new RFQDetailViewModel
            {
                RFQId = rfq.RFQId,
                RequestQuotationId = rfq.RequestQuotationId,
                RequestDate = rfq.RequestDate,
                ExpectedDate = rfq.ExpectedDate,
                Notes = rfq.Notes,
                IssuedByName = rfq.RequestedBy?.UserName ?? "Unknown",
                CompanyName = company?.Name,
                CompanyEmail = company?.Email,
                CompanyPhone = company?.Phone,
                CompanyAddress = company?.Address,
                Items = items,
                SubmittedQuotations = quotations, // optional, can be empty
                PurchaseRequestNumber = purchaseRequest.RequestNumber
            };

            return View(vm);
        }

        #endregion

        #region Supplier Qoutations

        [HttpGet]
        public async Task<IActionResult> CreateQoutation(int requestQuotationId)
        {
            var rfq = await context.RequestQuotations
                .Include(r => r.PurchaseRequest)
                    .ThenInclude(pr => pr.RequestedItems)
                        .ThenInclude(i => i.Fridge)
                .Include(r => r.Supplier)
                .FirstOrDefaultAsync(r => r.RequestQuotationId == requestQuotationId);

            if (rfq == null)
                return NotFound();

            var requestedByUser = await context.Users
                .FirstOrDefaultAsync(u => u.Id == rfq.RequestedByUserId);

            var company = await context.Company.FirstOrDefaultAsync();

            var model = new SupplierQuotationCreateViewModel
            {
                RFQId = rfq.RequestQuotationId,
                RFQCode = rfq.RFQId,
                PurchaseRequestId = rfq.PurchaseRequestId,
                RequestDate = rfq.RequestDate,
                ExpectedDate = rfq.PurchaseRequest?.ExpectedDate, // ✅ pull from PurchaseRequest
                Notes = rfq.Notes ?? "",

                Items = rfq.PurchaseRequest.RequestedItems.Select(i => new SupplierQuotationItemViewModel
                {
                    PurchaseRequestItemId = i.Id,
                    FridgeId = i.FridgeId,
                    FridgeModel = i.Fridge?.Model ?? "Unknown",
                    RequestedQuantity = i.Quantity,
                }).ToList(),

                CompanyName = company?.Name,
                CompanyEmail = company?.Email,
                CompanyPhone = company?.Phone,
                CompanyAddress = company?.Address,

                IssuedByName = requestedByUser?.FullName ?? requestedByUser?.UserName
            };

            return View(model);
        }


        // POST: SupplierQuotation/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateQoutation(SupplierQuotationCreateViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var supplier = await context.Suppliers
                .FirstOrDefaultAsync(s => s.ApplicationUserId == userId);

            if (supplier == null)
                return Unauthorized();

            // Create quotation header
            var quotation = new Quotation
            {
                SupplierId = supplier.Id,
                PurchaseRequestId = model.PurchaseRequestId,
                RequestQuotationId = model.RFQId,
                DateReceived = DateTime.Now
            };

            context.Quotations.Add(quotation);
            await context.SaveChangesAsync();

            // Add line items with warranty and expected delivery
            foreach (var item in model.Items)
            {
                var quotationItem = new QuotationItem
                {
                    QuotationId = quotation.Id,
                    FridgeId = item.FridgeId,
                    FridgeModel = item.FridgeModel,
                    PurchaseRequestItemId = item.PurchaseRequestItemId,
                    Quantity = item.RequestedQuantity,
                    QuotedQuantity = item.QuotedQuantity,
                    QuotedUnitPrice = item.QuotedUnitPrice,
                    WarrantyMonths = item.WarrantyMonths,
                    ExpectedDeliveryDate = item.ExpectedDeliveryDate
                };

                context.QuotationItems.Add(quotationItem);
            }

            await context.SaveChangesAsync();

            // Notify Purchasing Manager
            var purchasingManagers = await userManager.GetUsersInRoleAsync("Purchasing Manager");
            var liaisonUser = purchasingManagers.FirstOrDefault();

            if (liaisonUser != null)
            {
                var notification = new Notification
                {
                    RecipientUserId = liaisonUser.Id,
                    Title = "New Quotation Submitted",
                    Message = $"Supplier {supplier.Name} has submitted a new quotation for RFQ #{quotation.RequestQuotationId}.",
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false,
                    TargetController = "PurchasingManager",
                    TargetAction = "ViewQuotationDetails"
                };

                context.Notifications.Add(notification);
                await context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = "Quotation submitted successfully!";
            return RedirectToAction("MyQuotations", "Supplier");
        }


        // GET: SupplierQuotation/MyQuotations
        public async Task<IActionResult> MyQuotations()
        {
        
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var supplier = await context.Suppliers
                .FirstOrDefaultAsync(s => s.ApplicationUserId == userId);

            if (supplier == null)
                return Unauthorized();

            var allQuotations = await context.Quotations
                .Include(q => q.RequestQuotation) 
                .Include(q=> q.Items)
                .Where(q => q.SupplierId == supplier.Id)
                .ToListAsync();

            ViewBag.PendingCount = allQuotations.Count(q => q.Status == "Pending");
            ViewBag.ApprovedCount = allQuotations.Count(q => q.Status == "Approved");
            ViewBag.RejectedCount = allQuotations.Count(q => q.Status == "Rejected");

            var pendingQuotations = allQuotations
                .Where(q => q.Status == "Pending")
                .ToList();

            return View(pendingQuotations);
        }

        public async Task<IActionResult> ApprovedQuotations()
        {

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var supplier = await context.Suppliers
                .FirstOrDefaultAsync(s => s.ApplicationUserId == userId);

            if (supplier == null)
                return Unauthorized();

            var allQuotations = await context.Quotations
                .Include(q => q.RequestQuotation)
                .Include(q => q.Items)
                .Where(q => q.SupplierId == supplier.Id)
                .ToListAsync();

            ViewBag.PendingCount = allQuotations.Count(q => q.Status == "Pending");
            ViewBag.ApprovedCount = allQuotations.Count(q => q.Status == "Approved");
            ViewBag.RejectedCount = allQuotations.Count(q => q.Status == "Rejected");

            var ApprovedQuotations = allQuotations
                .Where(q => q.Status == "Approved")
                .ToList();

            return View(ApprovedQuotations);
        }

        public async Task<IActionResult> RejectedQuotations()
        {

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var supplier = await context.Suppliers
                .FirstOrDefaultAsync(s => s.ApplicationUserId == userId);

            if (supplier == null)
                return Unauthorized();

            var allQuotations = await context.Quotations
                .Include(q => q.RequestQuotation)
                .Include(q => q.Items)
                .Where(q => q.SupplierId == supplier.Id)
                .ToListAsync();

            ViewBag.PendingCount = allQuotations.Count(q => q.Status == "Pending");
            ViewBag.ApprovedCount = allQuotations.Count(q => q.Status == "Approved");
            ViewBag.RejectedCount = allQuotations.Count(q => q.Status == "Rejected");

            var RejectedQuotations = allQuotations
                .Where(q => q.Status == "Rejected")
                .ToList();

            return View(RejectedQuotations);
        }


        public async Task<IActionResult> ViewQuotation(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Get supplier linked to this user
            var supplier = await context.Suppliers
                .FirstOrDefaultAsync(s => s.ApplicationUserId == userId);

            if (supplier == null)
                return Unauthorized();

            // Load the quotation including items
            var quotation = await context.Quotations
                .Include(q => q.Items)
                .Include(q => q.PurchaseRequest)
                    .ThenInclude(pr => pr.RequestedItems)
                .Include(q => q.RequestQuotation)
                .FirstOrDefaultAsync(q => q.Id == id && q.SupplierId == supplier.Id);

            if (quotation == null)
                return NotFound();

            // Get a user in the "Purchasing Manager" role
            var purchasingManagers = await userManager.GetUsersInRoleAsync("Purchasing Manager");
            var purchasingManager = purchasingManagers.FirstOrDefault();

            var model = new SupplierQoutationDetailsViewModel
            {
                QuotationId = quotation.Id,
                RFQCode = quotation.RequestQuotation?.RFQId,
                RequestDate = quotation.PurchaseRequest.RequestDate,
                IssuedByName = purchasingManager?.FullName ?? purchasingManager?.UserName, // Purchase Manager
                Notes = quotation.PurchaseRequest.Notes,

                // Company info
                CompanyName = context.Company.FirstOrDefault()?.Name,
                CompanyEmail = context.Company.FirstOrDefault()?.Email,
                CompanyPhone = context.Company.FirstOrDefault()?.Phone,
                CompanyAddress = context.Company.FirstOrDefault()?.Address,

                // Company requested expected delivery date
                ExpectedDate = quotation.PurchaseRequest.ExpectedDate,

                Items = quotation.Items.Select(i => new SupplierQuotationItemViewModel
                {
                    FridgeModel = i.FridgeModel,
                    RequestedQuantity = i.PurchaseRequestItem?.Quantity ?? i.QuotedQuantity,
                    QuotedQuantity = i.QuotedQuantity,
                    QuotedUnitPrice = i.QuotedUnitPrice,
                    ExpectedDeliveryDate = i.ExpectedDeliveryDate, // Supplier’s expected date
                    WarrantyMonths = i.WarrantyMonths
                }).ToList()
            };

            return View(model);
        }
        #endregion
    }
}
