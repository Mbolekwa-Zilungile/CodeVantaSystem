using Code_Vanta_Systems.Data;
using Code_Vanta_Systems.Models;
using Code_Vanta_Systems.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Code_Vanta_Systems.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminFaultsController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public AdminFaultsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager, IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _environment = environment;
        }

        // Get All Logged Faults
        [HttpGet]
        public async Task<IActionResult> Index(int? pageIndex, string? search)
        {
            try
            {
                // Base query: include related entities
                var query = _context.FridgeFaults
                    .Include(f => f.Fridge)                     // parent fridge (if needed)
                    .Include(f => f.FridgeUnit)                 // specific unit
                        .ThenInclude(u => u.Technician)         // technician assigned to unit
                    .Include(f => f.ReportedBy)
                    .Include(f => f.AssignedTechnician)
                    .AsQueryable();

                // Apply search if provided
                if (!string.IsNullOrEmpty(search))
                {
                    search = search.ToLower();
                    query = query.Where(f =>
                        f.FridgeUnit.SerialNumber.ToLower().Contains(search) ||   // use FridgeUnit serial
                        f.FaultDescription.ToLower().Contains(search) ||
                        f.ReportedBy.FullName.ToLower().Contains(search) ||
                        (f.AssignedTechnician != null &&
                         f.AssignedTechnician.FullName.ToLower().Contains(search)) ||
                        (f.FridgeUnit.Technician != null &&
                         f.FridgeUnit.Technician.FullName.ToLower().Contains(search))
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
                    .ToListAsync();

                // Get all Fault Technicians
                var technicians = await _context.Employees
                    .Include(e => e.User)
                    .Where(e => e.Position == "Fault Technician" && e.IsActive)
                    .Select(e => new
                    {
                        TechnicianId = e.Id,
                        e.ApplicationUserId,
                        FullName = e.User.FullName
                    })
                    .ToListAsync();

                ViewData["Technicians"] = technicians;
                ViewData["currentPage"] = pageIndex.Value;
                ViewData["totalPages"] = totalPages;
                ViewData["search"] = search ?? "";

                return View(faults);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Error loading fridge faults";
                return RedirectToAction("Index");
            }
        }


        // Assign Technician
        [HttpPost]
        public async Task<IActionResult> AssignTechnician(int faultId, int technicianId)
        {
            try
            {
                // 1️ Find the fault and include FridgeUnit
                var fault = await _context.FridgeFaults
                    .Include(f => f.Fridge)
                    .Include(f => f.FridgeUnit)
                    .Include(f => f.ReportedBy)
                    .Include(f => f.AssignedTechnician)
                    .FirstOrDefaultAsync(f => f.Id == faultId);

                if (fault == null)
                {
                    TempData["ErrorMessage"] = "The selected fault could not be found.";
                    return RedirectToAction(nameof(Index));
                }

                // 2️ Find the technician (Employee)
                var technician = await _context.Employees
                    .Include(e => e.User)
                    .FirstOrDefaultAsync(e => e.Id == technicianId && e.Position == "Fault Technician");

                if (technician == null)
                {
                    TempData["ErrorMessage"] = "Technician not found or not a valid Fault Technician.";
                    return RedirectToAction(nameof(Index));
                }

                // 3️ Assign the technician to the FAULT
                fault.AssignedTechnicianId = technician.ApplicationUserId;

                // 3b️⃣ Assign the same technician to the FRIDGE UNIT
                if (fault.FridgeUnit != null)
                {
                    fault.FridgeUnit.TechnicianUserId = technician.ApplicationUserId;
                    fault.FridgeUnit.UpdatedAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                // 4️ Get the current logged-in admin (Identity)
                var currentUser = await _userManager.GetUserAsync(User);

                // 5️ Audit trail (use FridgeUnit serial)
                var audit = new AuditTrail
                {
                    EntityName = "FridgeFault",
                    EntityId = fault.Id,
                    ChangedByUserId = currentUser?.Id ?? "System",
                    ChangeType = "Update",
                    ChangeDate = DateTime.Now,
                    ChangeDetails =
                        $"Technician '{technician?.User?.FullName ?? "Unknown"}' assigned to Fridge Unit (Serial: {fault?.FridgeUnit?.SerialNumber ?? "N/A"}) by '{currentUser?.FullName ?? "System"}'."
                };

                _context.AuditTrails.Add(audit);
                await _context.SaveChangesAsync();

                // 6️ Notification for assigned technician
                var notification = new Notification
                {
                    RecipientUserId = technician.ApplicationUserId,
                    Title = "New Fridge Fault Assigned",
                    Message =
                        $"You have been assigned to resolve a fault for Fridge Unit (Serial: {fault?.FridgeUnit?.SerialNumber ?? "N/A"}). Please review the fault details.",
                    CreatedAt = DateTime.Now,
                    IsRead = false,
                    TargetController = "TechnicianFaults",
                    TargetAction = "Details"
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    $"Technician '{technician.User.FullName}' has been successfully assigned to the fault for fridge unit with Serial Number '{fault.FridgeUnit?.SerialNumber ?? "N/A"}'.";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while assigning the technician.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetFridges()
        {
            var fridges = await _context.Fridges
                .Where(f => !f.IsDeleted)
                .ToListAsync();

            return View("GetFridges", fridges);  // ✅ Return the view
        }

        [HttpGet]
        public async Task<IActionResult> EditFridge(int id)
        {
            var fridge = await _context.Fridges.FindAsync(id);
            if (fridge == null) return NotFound();

            var vm = new CreateFridgeViewModel
            {
                Id = fridge.Id,
                Model = fridge.Model,
                SerialNumber = fridge.SerialNumber,
                Category = fridge.Category,
                SupplierId = fridge.SupplierId,
                LocationId = fridge.LocationId,
                RentalPricePerMonth = fridge.RentalPricePerMonth,
                StockQuantity = fridge.StockQuantity,
                Description = fridge.Description,
                ImageUrl = fridge.ImageUrl
            };

            PopulateDropdowns(vm);

            return View(vm);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditFridge(CreateFridgeViewModel vm)
        {

            PopulateDropdowns(vm);

            if (!ModelState.IsValid)
            {
                Console.WriteLine("ModelState invalid.");
                return View(vm);
            }

            var fridge = await _context.Fridges.FindAsync(vm.Id);
            if (fridge == null) return NotFound();

            fridge.Model = vm.Model;
            fridge.SerialNumber = vm.SerialNumber;
            fridge.Category = vm.Category;
            fridge.Status = vm.Status;
            fridge.SupplierId = vm.SupplierId;
            fridge.LocationId = vm.LocationId;
            fridge.RentalPricePerMonth = vm.RentalPricePerMonth;
            fridge.StockQuantity = vm.StockQuantity;
            fridge.Description = vm.Description;


            if (vm.ImageFile != null && vm.ImageFile.Length > 0)
            {
                string uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "fridges");
                Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = Guid.NewGuid() + Path.GetExtension(vm.ImageFile.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                await using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await vm.ImageFile.CopyToAsync(fileStream);
                }


                if (!string.IsNullOrEmpty(fridge.ImageUrl) && fridge.ImageUrl != "/images/fridges/placeholder.png")
                {
                    var oldImagePath = Path.Combine(_environment.WebRootPath, fridge.ImageUrl.TrimStart('/').Replace("/", "\\"));
                    if (System.IO.File.Exists(oldImagePath))
                        System.IO.File.Delete(oldImagePath);
                }

                fridge.ImageUrl = "/images/fridges/" + uniqueFileName;
            }

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Fridge updated successfully!";

                return RedirectToAction("GetFridges");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error updating fridge: " + ex.Message);
                ModelState.AddModelError("", "Unable to update fridge. Check server logs.");
                return View(vm);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFridge(int id)
        {
            var fridge = await _context.Fridges.FirstOrDefaultAsync(f => f.Id == id);

            if (fridge == null)
            {
                TempData["ErrorMessage"] = "Fridge not found.";
                return RedirectToAction("GetFridges");
            }

            // Soft delete
            fridge.IsDeleted = true;
            fridge.DeletedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Fridge deleted successfully!";
            return RedirectToAction("GetFridges");
        }
        private void PopulateDropdowns(CreateFridgeViewModel vm)
        {
            vm.Suppliers = _context.Suppliers
                .Select(s => new CreateFridgeViewModel.SupplierDropdownViewModel { Id = s.Id, Name = s.Name })
                .ToList();

            vm.Locations = _context.Locations
                .Select(l => new CreateFridgeViewModel.LocationDropdownViewModel { Id = l.Id, Name = l.Description })
                .ToList();
        }
    }
}

