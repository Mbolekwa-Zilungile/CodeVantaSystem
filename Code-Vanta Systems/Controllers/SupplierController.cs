using Code_Vanta_Systems.Data;
using Code_Vanta_Systems.Models;
using Code_Vanta_Systems.Models.Enums;
using Code_Vanta_Systems.Services;
using Code_Vanta_Systems.ViewModels;
using Code_Vanta_Systems.ViewModels.DeliveryNote;
using Code_Vanta_Systems.ViewModels.SupplierQoutation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
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
        private readonly EmailService _emailService;

        public SupplierController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            EmailService emailService)
        {
            this.userManager = userManager;
            this.roleManager = roleManager;
            this.context = context;
            _emailService = emailService;
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
                    return Unauthorized();
                }

                var rfqs = await context.RequestQuotations
                    .Include(r => r.Supplier)
                        .ThenInclude(s => s.User)
                    .Include(r => r.RequestedBy)
                    .Where(r => r.SupplierId == supplier.Id)
                    .OrderByDescending(r => r.RequestDate)
                    .ToListAsync();

                ViewBag.RFQCount = rfqs.Count;

                var notifications = await context.Notifications
                    .Where(n => n.RecipientUserId == currentUserId)
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(5)
                    .ToListAsync();

                ViewBag.LatestNotifications = notifications;

                return View(notifications);
            }

            return RedirectToAction("AccessDenied", "Account");
        }

        #region RFQ

        public async Task<IActionResult> RFQ()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var supplier = await context.Suppliers
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.User.Id == userId);

            if (supplier == null)
            {
                return Unauthorized();
            }

            var rfqs = await context.RequestQuotations
                .Include(r => r.Supplier)
                    .ThenInclude(s => s.User)
                .Include(r => r.RequestedBy)
                .Where(r => r.SupplierId == supplier.Id)
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
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var supplier = await context.Suppliers
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.User.Id == userId);

            if (supplier == null)
                return Unauthorized();

            var rfq = await context.RequestQuotations
                .Include(r => r.Supplier)
                    .ThenInclude(s => s.User)
                .Include(r => r.RequestedBy)
                .Include(r => r.Quotations)
                .FirstOrDefaultAsync(r => r.RequestQuotationId == id && r.SupplierId == supplier.Id);

            if (rfq == null)
                return NotFound("RFQ not found.");

            // CHANGED: Use FridgeModel instead of Fridge
            var purchaseRequest = await context.PurchaseRequests
                .Include(pr => pr.RequestedItems)
                    .ThenInclude(ri => ri.FridgeModel)  // CHANGED: Fridge → FridgeModel
                .Include(pr => pr.RequestedBy)
                .FirstOrDefaultAsync(pr => pr.Id == rfq.PurchaseRequestId);

            if (purchaseRequest == null)
                return NotFound("Purchase Request not found.");

            var company = await context.Company.FirstOrDefaultAsync();

            // CHANGED: Use FridgeModel properties
            var items = purchaseRequest.RequestedItems.Select(i => new RFQItemViewModel
            {
                FridgeModelId = i.FridgeModelId, // CHANGED: Added FridgeModelId
                FridgeModel = i.FridgeModel?.Name ?? "Unknown", // CHANGED: Fridge?.Model → FridgeModel?.Name
                Quantity = i.Quantity
            }).ToList();

            var quotations = rfq.Quotations.Select(q => new QuotationSummaryViewModel
            {
                QuotationId = q.Id,
                SupplierName = q.Supplier.Name,
                DateReceived = q.DateReceived,
            }).ToList();

            var vm = new RFQDetailViewModel
            {
                RFQId = rfq.RFQId,
                RequestQuotationId = rfq.RequestQuotationId,
                RequestDate = rfq.RequestDate,
                ExpectedDate = rfq.PurchaseRequest?.ExpectedDate,
                Notes = rfq.Notes,
                IssuedByName = rfq.RequestedBy?.UserName ?? "Unknown",
                CompanyName = company?.Name,
                CompanyEmail = company?.Email,
                CompanyPhone = company?.Phone,
                CompanyAddress = company?.Address,
                Items = items,
                SubmittedQuotations = quotations,
                PurchaseRequestNumber = purchaseRequest.RequestNumber
            };

            return View(vm);
        }

        #endregion

        #region Supplier Quotations

        [HttpGet]
        public async Task<IActionResult> CreateQoutation(int requestQuotationId)
        {
            var rfq = await context.RequestQuotations
                .Include(r => r.PurchaseRequest)
                    .ThenInclude(pr => pr.RequestedItems)
                        .ThenInclude(i => i.FridgeModel)  // CHANGED: Fridge → FridgeModel
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
                ExpectedDate = rfq.PurchaseRequest?.ExpectedDate,
                Notes = rfq.Notes ?? "",

                Items = rfq.PurchaseRequest.RequestedItems.Select(i => new SupplierQuotationItemViewModel
                {
                    PurchaseRequestItemId = i.Id,
                    FridgeModelId = i.FridgeModelId, // CHANGED: FridgeId → FridgeModelId
                    FridgeModel = i.FridgeModel?.Name ?? "Unknown", // CHANGED: Fridge?.Model → FridgeModel?.Name
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateQoutation(SupplierQuotationCreateViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            string lastQuotation = await context.Quotations
                .OrderByDescending(q => q.Id)
                .Select(q => q.QuotationNumber)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (!string.IsNullOrEmpty(lastQuotation))
            {
                var parts = lastQuotation.Split('-');
                if (parts.Length == 3 && int.TryParse(parts[2], out int lastNum))
                    nextNumber = lastNum + 1;
            }
            string newQuotationNumber = $"QTN-{DateTime.Now.Year}-{nextNumber:000}";

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var supplier = await context.Suppliers
                .FirstOrDefaultAsync(s => s.ApplicationUserId == userId);

            if (supplier == null)
                return Unauthorized();

            var quotation = new Quotation
            {
                SupplierId = supplier.Id,
                PurchaseRequestId = model.PurchaseRequestId,
                RequestQuotationId = model.RFQId,
                QuotationNumber = newQuotationNumber,
                DateReceived = DateTime.Now
            };

            context.Quotations.Add(quotation);
            await context.SaveChangesAsync();

            foreach (var item in model.Items)
            {
                var quotationItem = new QuotationItem
                {
                    QuotationId = quotation.Id,
                    FridgeModelId = item.FridgeModelId, // CHANGED: FridgeId → FridgeModelId
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

        public async Task<IActionResult> MyQuotations()
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

            var approvedQuotations = allQuotations
                .Where(q => q.Status == "Approved")
                .ToList();

            return View(approvedQuotations);
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

            var rejectedQuotations = allQuotations
                .Where(q => q.Status == "Rejected")
                .ToList();

            return View(rejectedQuotations);
        }

        public async Task<IActionResult> ViewQuotation(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var supplier = await context.Suppliers
                .FirstOrDefaultAsync(s => s.ApplicationUserId == userId);

            if (supplier == null)
                return Unauthorized();

            var quotation = await context.Quotations
                .Include(q => q.Items)
                .Include(q => q.PurchaseRequest)
                    .ThenInclude(pr => pr.RequestedItems)
                .Include(q => q.RequestQuotation)
                .FirstOrDefaultAsync(q => q.Id == id && q.SupplierId == supplier.Id);

            if (quotation == null)
                return NotFound();

            var purchasingManagers = await userManager.GetUsersInRoleAsync("Purchasing Manager");
            var purchasingManager = purchasingManagers.FirstOrDefault();

            var model = new SupplierQoutationDetailsViewModel
            {
                QuotationId = quotation.Id,
                RFQCode = quotation.RequestQuotation?.RFQId,
                RequestDate = quotation.PurchaseRequest.RequestDate,
                IssuedByName = purchasingManager?.FullName ?? purchasingManager?.UserName,
                Notes = quotation.PurchaseRequest.Notes,

                CompanyName = context.Company.FirstOrDefault()?.Name,
                CompanyEmail = context.Company.FirstOrDefault()?.Email,
                CompanyPhone = context.Company.FirstOrDefault()?.Phone,
                CompanyAddress = context.Company.FirstOrDefault()?.Address,

                Items = quotation.Items.Select(i => new SupplierQuotationItemViewModel
                {
                    FridgeModel = i.FridgeModel,
                    RequestedQuantity = i.PurchaseRequestItem?.Quantity ?? i.QuotedQuantity,
                    QuotedQuantity = i.QuotedQuantity,
                    QuotedUnitPrice = i.QuotedUnitPrice,
                    ExpectedDeliveryDate = i.ExpectedDeliveryDate,
                    WarrantyMonths = i.WarrantyMonths
                }).ToList()
            };

            return View(model);
        }
        #endregion

        #region Purchase Order

        public async Task<IActionResult> PurchaseOrderDetails(int id)
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var purchaseOrder = await context.PurchaseOrders
                .Include(po => po.Supplier)
                    .ThenInclude(s => s.User)
                .Include(po => po.Items)
                    .ThenInclude(i => i.FridgeModel)  // CHANGED: Fridge → FridgeModel
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
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var supplier = await context.Suppliers
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.User.Id == userId);

            if (supplier == null)
            {
                return Unauthorized();
            }

            var allOrders = await context.PurchaseOrders
                .Where(po => po.SupplierId == supplier.Id)
                .Include(po => po.Supplier)
                    .ThenInclude(s => s.User)
                .Include(po => po.Items)
                    .ThenInclude(i => i.FridgeModel)  // CHANGED: Fridge → FridgeModel
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
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var supplier = await context.Suppliers
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.User.Id == userId);

            if (supplier == null)
            {
                return Unauthorized();
            }

            var allOrders = await context.PurchaseOrders
                .Where(po => po.SupplierId == supplier.Id)
                .Include(po => po.Supplier)
                    .ThenInclude(s => s.User)
                .Include(po => po.Items)
                    .ThenInclude(i => i.FridgeModel)  // CHANGED: Fridge → FridgeModel
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
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var supplier = await context.Suppliers
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.User.Id == userId);

            if (supplier == null)
            {
                return Unauthorized();
            }

            var allOrders = await context.PurchaseOrders
                .Where(po => po.SupplierId == supplier.Id)
                .Include(po => po.Supplier)
                    .ThenInclude(s => s.User)
                .Include(po => po.Items)
                    .ThenInclude(i => i.FridgeModel)  // CHANGED: Fridge → FridgeModel
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
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var supplier = await context.Suppliers
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.User.Id == userId);

            if (supplier == null)
            {
                return Unauthorized();
            }

            var allOrders = await context.PurchaseOrders
                .Where(po => po.SupplierId == supplier.Id)
                .Include(po => po.Supplier)
                    .ThenInclude(s => s.User)
                .Include(po => po.Items)
                    .ThenInclude(i => i.FridgeModel)  // CHANGED: Fridge → FridgeModel
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptPurchaseOrder(int id)
        {
            var purchaseOrder = await context.PurchaseOrders
                .FirstOrDefaultAsync(po => po.PurchaseOrderId == id);

            if (purchaseOrder == null)
                return NotFound();

            purchaseOrder.Status = OrderStatus.Approved;
            context.PurchaseOrders.Update(purchaseOrder);

            var managers = await userManager.GetUsersInRoleAsync("Purchasing Manager"); // CHANGED: Fixed role name
            foreach (var manager in managers)
            {
                var notification = new Notification
                {
                    RecipientUserId = manager.Id,
                    Title = "Purchase Order Approved",
                    Message = $"Purchase Order {purchaseOrder.PONumber} has been approved by the supplier.",
                    TargetController = "PurchasingManager",
                    TargetAction = "AllPurchaseOrders",
                    CreatedAt = DateTime.Now,
                    IsRead = false
                };

                context.Notifications.Add(notification);
            }

            await context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Purchase Order {purchaseOrder.PONumber} has been successfully approved.";
            return RedirectToAction("AllPurchaseOrders");
        }

        #endregion

        #region Delivery Note

        public async Task<IActionResult> DeliveryNote(int id)
        {
            var purchaseOrder = await context.PurchaseOrders
                .Include(po => po.Supplier)
                    .ThenInclude(s => s.User)
                .Include(po => po.Items)
                    .ThenInclude(i => i.FridgeModel)  // CHANGED: Fridge → FridgeModel
                .Include(po => po.Quotation)
                    .ThenInclude(q => q.RequestQuotation)
                .FirstOrDefaultAsync(po => po.PurchaseOrderId == id && po.Status == OrderStatus.Approved);

            if (purchaseOrder == null)
                return NotFound();

            var model = new DeliveryNoteViewModel
            {
                PurchaseOrderId = purchaseOrder.PurchaseOrderId,
                PONumber = purchaseOrder.PONumber,
                SupplierName = purchaseOrder.Supplier?.Name,
                SupplierEmail = purchaseOrder.Supplier?.User?.Email,
                SupplierPhone = purchaseOrder.Supplier?.Phone,
                SupplierAddress = purchaseOrder.Supplier?.Address,
                DeliveryAddress = purchaseOrder.DeliveryAddress,
                DeliveryTerms = purchaseOrder.DeliveryTerms,
                PaymentTerms = purchaseOrder.PaymentTerms,
                GeneratedDate = DateTime.Now,
                ExpectedDeliveryDate = purchaseOrder.Quotation?.RequestQuotation?.ExpectedDeliveryDate,
                Items = purchaseOrder.Items.Select(i => new DeliveryItemViewModel
                {
                    FridgeModel = i.FridgeModel?.Name,  // CHANGED: Fridge?.Model → FridgeModel?.Name
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendDeliveryNote(DeliveryNoteViewModel model)
        {
            if (!ModelState.IsValid)
                return View("DeliveryNote", model);

            var purchaseOrder = await context.PurchaseOrders
                .Include(po => po.Supplier)
                .Include(po => po.Items)
                    .ThenInclude(i => i.FridgeModel)  // CHANGED: Fridge → FridgeModel
                .FirstOrDefaultAsync(po => po.PurchaseOrderId == model.PurchaseOrderId);

            if (purchaseOrder == null)
                return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var deliveryNote = new DeliveryNote
            {
                PurchaseOrderId = purchaseOrder.PurchaseOrderId,
                SupplierId = purchaseOrder.SupplierId,
                GeneratedDate = DateTime.Now,
                ReceivedById = userId,
                Notes = "Delivery generated from approved purchase order."
            };

            context.DeliveryNotes.Add(deliveryNote);

            purchaseOrder.Status = OrderStatus.Shipped;
            context.PurchaseOrders.Update(purchaseOrder);

            await context.SaveChangesAsync();

            var managers = await userManager.GetUsersInRoleAsync("Purchasing Manager"); // CHANGED: Fixed role name
            foreach (var manager in managers)
            {
                var notification = new Notification
                {
                    RecipientUserId = manager.Id,
                    Title = "Delivery Note Created",
                    Message = $"Delivery Note for Purchase Order {purchaseOrder.PONumber} has been generated.",
                    TargetController = "PurchasingManager",
                    TargetAction = "AllPurchaseOrders",
                    CreatedAt = DateTime.Now,
                    IsRead = false
                };

                context.Notifications.Add(notification);
            }

            await context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Delivery Note for {purchaseOrder.PONumber} sent successfully.";

            return RedirectToAction("ShippedPurchaseOrders", "Supplier");
        }

        [HttpGet]
        public async Task<IActionResult> DetailDeliveryNote(int id)
        {
            var deliveryNote = await context.DeliveryNotes
                .Include(dn => dn.PurchaseOrder)
                    .ThenInclude(po => po.Supplier)
                .Include(dn => dn.PurchaseOrder)
                    .ThenInclude(po => po.Items)
                        .ThenInclude(i => i.FridgeModel)  // CHANGED: Fridge → FridgeModel
                .FirstOrDefaultAsync(dn => dn.Id == id);

            if (deliveryNote == null)
            {
                return NotFound();
            }

            var po = deliveryNote.PurchaseOrder;

            var viewModel = new DeliveryNoteViewModel
            {
                Id = deliveryNote.Id,
                PurchaseOrderId = po.PurchaseOrderId,
                PONumber = po.PONumber,
                SupplierName = po.Supplier?.Name,
                SupplierEmail = po.Supplier?.User?.Email,
                SupplierPhone = po.Supplier?.Phone,
                SupplierAddress = po.Supplier?.Address,
                DeliveryAddress = po.DeliveryAddress,
                DeliveryTerms = po.DeliveryTerms,
                PaymentTerms = po.PaymentTerms,
                GeneratedDate = deliveryNote.GeneratedDate ?? deliveryNote.DeliveryDate,
                ExpectedDeliveryDate = po.ExpectedDeliveryDate,
                Status = po.Status.ToString(),
                Items = po.Items.Select(i => new DeliveryItemViewModel
                {
                    FridgeModel = i.FridgeModel?.Name,  // CHANGED: Fridge?.Model → FridgeModel?.Name
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList()
            };

            return View(viewModel);
        }
        #endregion
    }
}