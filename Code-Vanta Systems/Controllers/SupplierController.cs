using Code_Vanta_Systems.Data;
using Code_Vanta_Systems.Models;
using Code_Vanta_Systems.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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

        public IActionResult Index()
        {
            return View();
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

        public async Task<IActionResult> RFQDetails(int? id)
        {
            if (id == null)
                return BadRequest();

            var rfq = await context.RequestQuotations
                .Include(r => r.Supplier)
                    .ThenInclude(s => s.User)
                .Include(r => r.RequestedBy)
                .Include(r => r.Quotations)
                    .ThenInclude(q => q.Items)
                        .ThenInclude(qi => qi.PurchaseRequestItem)
                .FirstOrDefaultAsync(r => r.RequestQuotationId == id);

            if (rfq == null)
                return NotFound();

            // Ensure supplier can only view their own RFQs
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (rfq.Supplier?.User?.Id != userId)
                return Forbid();

            // Map to view model
            var vm = new RFQDetailViewModel
            {
                RFQId = rfq.RFQId,
                RequestQuotationId = rfq.RequestQuotationId,
                RequestDate = rfq.RequestDate,
                SupplierName = rfq.Supplier?.Name,
                SupplierEmail = rfq.Supplier?.User?.Email,
                RequestedBy = rfq.RequestedBy?.UserName ?? "Unknown",
                Notes = rfq.Notes,
                Items = rfq.Quotations
                            .SelectMany(q => q.Items) // <-- use Items now
                            .ToList()
            };

            return View(vm);
        }
        #endregion
    }
}
