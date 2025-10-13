using Code_Vanta_Systems.Data;
using Code_Vanta_Systems.Models;
using Code_Vanta_Systems.Models.Enums;
using Code_Vanta_Systems.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering; // <-- FINAL ADJUSTMENT: Added this using directive
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
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

        //=========================================================
        // DASHBOARD & INDEX
        //=========================================================

        // GET: MaintenanceTechnician/Index (Dashboard)
        public async Task<IActionResult> Index()
        {
            var currentUserId = _userManager.GetUserId(User);

            // Dynamically count allocated fridges
            var allocatedFridgesCount = await _context.Fridges
                .CountAsync(f => f.TechnicianUserId == currentUserId && f.Status == FridgeStatus.Allocated);

            // Dynamically count open faults assigned to this technician
            ViewBag.OpenFaultsCount = await _context.FridgeFaults
                .CountAsync(ff => ff.AssignedTechnicianId == currentUserId && ff.Status == FaultStatus.Open);

            // FIX: Uses TechnicianId and Status property for completion check
            ViewBag.ScheduledMaintenanceCount = await _context.MaintenanceSchedules
                .CountAsync(ms => ms.TechnicianId == currentUserId && ms.Status != "Completed");

            ViewBag.AllocatedFridgesCount = allocatedFridgesCount;

            return View();
        }

        //=========================================================
        // ASSET MANAGEMENT & HISTORY (VIEWING)
        //=========================================================


        // GET: MaintenanceTechnician/AllocatedFridges (List of fridges assigned to the current technician)
        public async Task<IActionResult> AllocatedFridges()
        {
            var currentUserId = _userManager.GetUserId(User);

            // 1. Fetch all allocated fridges assigned to this specific technician
            var allocatedFridges = await _context.Fridges
                .Include(f => f.Customer)
                .Include(f => f.Location)
                .Include(f => f.MaintenanceSchedules.OrderByDescending(ms => ms.ScheduledDate).Take(1))
                .Where(f =>
                    f.TechnicianUserId == currentUserId &&  // The critical filter
                    f.Status == FridgeStatus.Allocated &&    // Only show fridges currently deployed
                    !f.IsDeleted)                         // Exclude deleted fridges
                .ToListAsync();

            // 2. Map the Fridge models to your MaintenanceFridgeListViewModel
            var viewModel = allocatedFridges.Select(f => new MaintenanceFridgeListViewModel
            {
                FridgeId = f.Id,
                SerialNumber = f.SerialNumber,
                Model = f.Model,
                // The Status should probably be mapped from the FridgeStatus enum
                FridgeStatus = f.Status.ToString(),
                CustomerBusinessName = f.Customer?.BusinessName ?? "N/A",
                CustomerAddress = f.Customer?.PhysicalAddress ?? f.Location.Description,

                // Find the next upcoming scheduled visit for this fridge
                NextScheduledVisit = f.MaintenanceSchedules
                    .FirstOrDefault(ms => ms.ScheduledDate >= DateTime.Today)?
                    .ScheduledDate

            }).OrderBy(vm => vm.CustomerBusinessName).ToList();

            // 3. Pass the list of view models to the view
            return View(viewModel);
        }


        // GET: MaintenanceTechnician/FridgeDetails/5
        public async Task<IActionResult> FridgeDetails(int id)
        {
            var currentUserId = _userManager.GetUserId(User);

            var fridge = await _context.Fridges
                .Include(f => f.Customer)
                .Include(f => f.Location)
                .Include(f => f.Supplier)

                // Load collections/history needed for the ViewModel:
                .Include(f => f.Faults.OrderByDescending(ff => ff.DateReported))

                // FIX: Include the new ServiceRecords collection instead of Maintenances
                .Include(f => f.ServiceRecords.OrderByDescending(sr => sr.DateOfService))

                .Include(f => f.MaintenanceSchedules.Where(ms => ms.ScheduledDate >= DateTime.Now).OrderBy(ms => ms.ScheduledDate))

                .FirstOrDefaultAsync(f => f.Id == id && f.TechnicianUserId == currentUserId);

            if (fridge == null)
            {
                // ... (error handling)
            }

            // Map the data to the expected ViewModel
            var viewModel = new MaintenanceFridgeDetailsViewModel
            {
                // ... (FridgeSummary mapping)

                FaultHistory = fridge.Faults.OrderByDescending(f => f.DateReported).ToList(),

                // FIX: Assign the ICollection<ServiceRecord> data from the new property
                ServiceHistory = fridge.ServiceRecords.OrderByDescending(sr => sr.DateOfService).ToList(),

                // ... (UpcomingSchedules mapping)
            };

            return View(viewModel);
        }


        //=========================================================
        // FAULT MANAGEMENT
        //=========================================================

        // GET: MaintenanceTechnician/FaultReports (List of faults assigned to the current technician)
        public async Task<IActionResult> FaultReports()
        {
            var currentUserId = _userManager.GetUserId(User);

            var faults = await _context.FridgeFaults
                .Include(ff => ff.Fridge)
                .Include(ff => ff.ReportedBy)
                .Where(ff => ff.AssignedTechnicianId == currentUserId)
                .OrderByDescending(ff => ff.DateReported)
                .ToListAsync();

            ViewData["Title"] = "My Fault Reports";
            return View(faults);
        }

        // GET: MaintenanceTechnician/FaultDetails/5
        public async Task<IActionResult> FaultDetails(int id)
        {
            var currentUserId = _userManager.GetUserId(User);

            var fault = await _context.FridgeFaults
                .Include(ff => ff.Fridge)
                    .ThenInclude(f => f.Customer)
                .Include(ff => ff.ReportedBy)
                .FirstOrDefaultAsync(ff => ff.Id == id && ff.AssignedTechnicianId == currentUserId); // Security check

            if (fault == null)
            {
                TempData["ErrorMessage"] = "Fault not found or not assigned to you.";
                return RedirectToAction(nameof(FaultReports));
            }

            // Retrieve service record history linked to this specific fault
            ViewBag.ServiceRecords = await _context.ServiceRecords
                .Where(sr => sr.FaultId == id)
                .OrderByDescending(sr => sr.DateOfService)
                .ToListAsync();

            ViewData["Title"] = $"Fault Report: {fault.Id}";
            return View(fault);
        }

        // GET Method for Proactive Fault Reporting
        // GET: MaintenanceTechnician/CreateFaultReport
        public async Task<IActionResult> CreateFaultReport(int? fridgeId)
        {
            // FIX: ViewModel property FridgeId must be int? for this to work implicitly.
            var viewModel = new CreateFaultReportViewModel { FridgeId = fridgeId };
            await PopulateCreateFaultReportViewModel(viewModel);
            ViewData["Title"] = "Report New Fault";
            return View(viewModel);
        }

        // POST Method for Proactive Fault Reporting
        // POST: MaintenanceTechnician/CreateFaultReport
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFaultReport(CreateFaultReportViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var currentUserId = _userManager.GetUserId(User);

                var fault = new FridgeFault
                {
                    // FIX: Use .Value to safely convert int? (ViewModel) to int (Model)
                    FridgeId = viewModel.FridgeId.Value,
                    FaultDescription = viewModel.Description,
                    DateReported = DateTime.Now,
                    ReportedByUserId = currentUserId, // Technician reports the fault
                    AssignedTechnicianId = currentUserId, // Technician self-assigns the new fault
                    Status = FaultStatus.Open,
                    Severity = viewModel.Severity
                };

                _context.FridgeFaults.Add(fault);
                await _context.SaveChangesAsync();
                TempData["Success"] = "New fault report created and assigned to you.";
                return RedirectToAction(nameof(FaultReports));
            }

            await PopulateCreateFaultReportViewModel(viewModel);
            return View(viewModel);
        }

        //=========================================================
        // SERVICE RECORD CREATION
        //=========================================================

        // GET: MaintenanceTechnician/CreateServiceRecord
        public async Task<IActionResult> CreateServiceRecord(int fridgeId, int? faultId)
        {
            var viewModel = new CreateServiceRecordViewModel
            {
                FridgeId = fridgeId,
                FaultId = faultId,
                DateOfService = DateTime.Now
            };

            ViewData["Title"] = "Create Service Record";
            return View(viewModel);
        }


        // POST: MaintenanceTechnician/CreateServiceRecord (Existing logic retained)
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

                // Update the status of the related FridgeFault if FaultId is present.
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

        //=========================================================
        // MAINTENANCE SCHEDULING
        //=========================================================

        // GET Method for Maintenance Schedule List
        // GET: MaintenanceTechnician/MaintenanceSchedules (List View)
        public async Task<IActionResult> MaintenanceSchedules()
        {
            var currentUserId = _userManager.GetUserId(User);
            var schedules = await _context.MaintenanceSchedules
                .Include(ms => ms.Fridge)
                    .ThenInclude(f => f.Customer)
                // FIX: Corrected property names: TechnicianId and Status
                .Where(ms => ms.TechnicianId == currentUserId && ms.Status != "Completed")
                .OrderBy(ms => ms.ScheduledDate)
                .ToListAsync();

            ViewData["Title"] = "Scheduled Maintenance";
            return View(schedules);
        }

        // GET Method for Creating a Schedule
        // GET: MaintenanceTechnician/CreateSchedule
        public async Task<IActionResult> CreateSchedule(int? fridgeId)
        {
            var viewModel = new CreateScheduleViewModel { FridgeId = fridgeId, ScheduledDate = DateTime.Now.AddDays(7) };
            await PopulateCreateScheduleViewModel(viewModel);
            ViewData["Title"] = "Create Maintenance Schedule";
            return View(viewModel);
        }

        // POST Method for Creating a Schedule
        // POST: MaintenanceTechnician/CreateSchedule
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSchedule(CreateScheduleViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var schedule = new MaintenanceSchedule
                {
                    // FIX: Use .Value to convert int? (ViewModel) to int (Model)
                    FridgeId = viewModel.FridgeId.Value,
                    // FIX: Corrected property name: TechnicianId
                    TechnicianId = _userManager.GetUserId(User),
                    ScheduledDate = viewModel.ScheduledDate,
                    Description = viewModel.Description,
                    Status = "Scheduled"
                };

                _context.MaintenanceSchedules.Add(schedule);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Maintenance schedule created successfully.";
                return RedirectToAction(nameof(MaintenanceSchedules));
            }

            await PopulateCreateScheduleViewModel(viewModel);
            return View(viewModel);
        }

        //=========================================================
        // HELPERS
        //=========================================================

        // Helper to populate the Fridge dropdown for scheduling
        private async Task PopulateCreateScheduleViewModel(CreateScheduleViewModel viewModel)
        {
            var currentUserId = _userManager.GetUserId(User);
            // Only show fridges assigned to this technician
            var fridges = await _context.Fridges
                .Include(f => f.Customer)
                .Where(f => f.TechnicianUserId == currentUserId && f.Status == FridgeStatus.Allocated)
                .OrderBy(f => f.SerialNumber)
                .ToListAsync();

            // FIX: Resolved CS0103 by changing lambda variable to 'fridge'
            viewModel.AvailableFridges = fridges.Select(fridge => new SelectListItem
            {
                Value = fridge.Id.ToString(),
                Text = $"{fridge.SerialNumber} ({fridge.Customer?.BusinessName ?? "N/A"})"
            }).ToList();
        }

        // Helper to populate the Fridge dropdown and Severity for fault reporting
        private async Task PopulateCreateFaultReportViewModel(CreateFaultReportViewModel viewModel)
        {
            var currentUserId = _userManager.GetUserId(User);
            var fridges = await _context.Fridges
                .Include(f => f.Customer)
                .Where(f => f.TechnicianUserId == currentUserId && f.Status == FridgeStatus.Allocated)
                .OrderBy(f => f.SerialNumber)
                .ToListAsync();

            // FIX: Resolved CS1061/CS0103 by ensuring AvailableFridges exists on ViewModel and using 'fridge'
            viewModel.AvailableFridges = fridges.Select(fridge => new SelectListItem
            {
                Value = fridge.Id.ToString(),
                Text = $"{fridge.SerialNumber} ({fridge.Customer?.BusinessName ?? "N/A"})"
            }).ToList();

            ViewBag.SeverityOptions = Enum.GetValues(typeof(FaultSeverity))
                .Cast<FaultSeverity>()
                .Select(v => new SelectListItem { Text = v.ToString(), Value = v.ToString() });
        }
    }
}