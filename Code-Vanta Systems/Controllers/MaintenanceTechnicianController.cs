using Code_Vanta_Systems.Data;
using Code_Vanta_Systems.Models;
using Code_Vanta_Systems.Models.Enums;
using Code_Vanta_Systems.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Code_Vanta_Systems.Controllers
{
    [Authorize(Roles = "Maintenance Technician")]
    public class MaintenanceTechnicianController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MaintenanceTechnicianController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: MaintenanceTechnician/Index (Dashboard)
        public async Task<IActionResult> Index()
        {
            var currentUserId = _userManager.GetUserId(User);

            // Dynamically count allocated fridges
            var allocatedFridgesCount = await _context.Fridges
                .CountAsync(f => f.TechnicianUserId == currentUserId && f.Status == FridgeStatus.Allocated);

            // Dynamically count open faults assigned to this technician
            ViewBag.OpenFaultsCount = await _context.FridgeFaults
                .CountAsync(ff => ff.AssignedTechnicianId == currentUserId && ff.Status != FaultStatus.Resolved);

            // Pass the allocated count to the view (used in the dashboard text)
            ViewBag.AllocatedFridgesCount = allocatedFridgesCount;

            return View();
        }

        // GET: MaintenanceTechnician/AllocatedFridges (View list of allocated fridges)
        public async Task<IActionResult> AllocatedFridges()
        {
            var currentUserId = _userManager.GetUserId(User);

            var fridges = await _context.Fridges
                .Where(f => f.TechnicianUserId == currentUserId && f.Status == FridgeStatus.Allocated)
                .Include(f => f.Customer)
                .Include(f => f.MaintenanceSchedules) // <-- FIXED: Changed Schedules to MaintenanceSchedules
                .Select(f => new MaintenanceFridgeListViewModel
                {
                    FridgeId = f.Id,
                    SerialNumber = f.SerialNumber,
                    Model = f.Model,
                    FridgeStatus = f.Status.ToString(),
                    CustomerBusinessName = f.Customer.BusinessName,
                    CustomerAddress = f.Customer.PhysicalAddress,
                    NextScheduledVisit = f.MaintenanceSchedules // <-- FIXED: Changed Schedules to MaintenanceSchedules
                                             .Where(s => s.ScheduledDate > DateTime.Now)
                                             .OrderBy(s => s.ScheduledDate)
                                             .Select(s => (DateTime?)s.ScheduledDate)
                                             .FirstOrDefault()
                })
                .ToListAsync();

            return View(fridges);
        }

        // GET: MaintenanceTechnician/FaultReports (View list of assigned faults)
        public async Task<IActionResult> FaultReports()
        {
            var currentUserId = _userManager.GetUserId(User);

            var faults = await _context.FridgeFaults
                .Where(ff => ff.AssignedTechnicianId == currentUserId && ff.Status != FaultStatus.Resolved)
                .Include(ff => ff.Fridge) // Include Fridge for Serial Number
                .Include(ff => ff.ReportedBy) // Include User for Reporter's Name
                .OrderByDescending(ff => ff.DateReported)
                .ToListAsync();

            return View(faults);
        }

        // ------------------------------------------------------------------------
        // Service Record Actions
        // ------------------------------------------------------------------------

        // GET: MaintenanceTechnician/CreateServiceRecord (Displays the form)
        [HttpGet]
        public IActionResult CreateServiceRecord(int? fridgeId)
        {
            // Initialize a new ViewModel
            var viewModel = new CreateServiceRecordViewModel
            {
                FridgeId = fridgeId.GetValueOrDefault(),
                DateOfService = DateTime.Now
            };

            // If the view uses dropdowns (e.g., for FridgeId or FaultId), 
            // the controller would populate those SelectLists here.

            return View(viewModel);
        }

        // POST: MaintenanceTechnician/CreateServiceRecord (Handles form submission)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateServiceRecord(CreateServiceRecordViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                // Create the ServiceRecord model from the ViewModel
                var serviceRecord = new ServiceRecord
                {
                    FridgeId = viewModel.FridgeId,
                    DateOfService = viewModel.DateOfService,
                    ServiceDetails = viewModel.ServiceDetails,
                    TechnicianUserId = _userManager.GetUserId(User),
                    FaultId = viewModel.FaultId,
                };

                _context.ServiceRecords.Add(serviceRecord);

                // Optional: Update the status of the related FridgeFault if FaultId is present.
                if (viewModel.FaultId.HasValue)
                {
                    var fault = await _context.FridgeFaults.FindAsync(viewModel.FaultId.Value);
                    if (fault != null)
                    {
                        fault.Status = FaultStatus.Resolved;
                        _context.FridgeFaults.Update(fault);
                    }
                }

                await _context.SaveChangesAsync();

                TempData["Success"] = "Service record created successfully.";
                return RedirectToAction("Index");
            }

            // If validation fails, return the view with the model to display errors.
            return View(viewModel);
        }
    }
}