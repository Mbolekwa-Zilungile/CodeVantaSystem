using Code_Vanta_Systems.Data;
using Code_Vanta_Systems.Models;
using Code_Vanta_Systems.Models.Enums;
using Code_Vanta_Systems.ViewModels;
using Code_Vanta_Systems.ViewModels.MaintenanceTechnician;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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

            // Dynamically count allocated fridge units
            var allocatedFridgeUnitsCount = await _context.FridgeUnits
                .CountAsync(fu => fu.TechnicianUserId == currentUserId && fu.Status == FridgeStatus.Allocated);

            // Dynamically count open faults assigned to this technician
            ViewBag.OpenFaultsCount = await _context.FridgeFaults
                .CountAsync(ff => ff.AssignedTechnicianId == currentUserId && ff.Status == FaultStatus.Open);

            // Count scheduled maintenance assigned to this technician
            ViewBag.ScheduledMaintenanceCount = await _context.MaintenanceSchedules
                .CountAsync(ms => ms.TechnicianId == currentUserId && ms.Status != "Completed");

            ViewBag.AllocatedFridgesCount = allocatedFridgeUnitsCount;

            return View();
        }

        //=========================================================
        // ASSET MANAGEMENT & HISTORY (VIEWING)
        //=========================================================

        // GET: MaintenanceTechnician/AllocatedFridges
        [HttpGet]
        public async Task<IActionResult> AllocatedFridges()
        {
            var currentUserId = _userManager.GetUserId(User);

            var allocatedFridgeUnits = await _context.FridgeUnits
                .Include(f => f.FridgeModel)
                .Include(f => f.Location)
                .Include(f => f.CurrentCustomer)
                .Include(f => f.MaintenanceSchedules)
                .Where(f => f.TechnicianUserId == currentUserId &&
                           f.Status == FridgeStatus.Allocated)
                .ToListAsync();

            var viewModel = allocatedFridgeUnits.Select(f => new MaintenanceFridgeListViewModel
            {
                FridgeId = f.Id,
                SerialNumber = f.SerialNumber,
                Model = f.FridgeModel.Name, // Now from FridgeModel
                FridgeStatus = f.Status.ToString(),
                CustomerBusinessName = f.CurrentCustomer?.BusinessName ?? "N/A",
                CustomerAddress = f.CurrentCustomer?.PhysicalAddress ?? f.Location?.Description,
                NextScheduledVisit = f.MaintenanceSchedules
                    .Where(ms => ms.ScheduledDate >= DateTime.Today)
                    .OrderBy(ms => ms.ScheduledDate)
                    .FirstOrDefault()
                    ?.ScheduledDate
            }).OrderBy(vm => vm.CustomerBusinessName).ToList();

            return View(viewModel);
        }

        // GET: MaintenanceTechnician/AllFridges
        [HttpGet]
        public async Task<IActionResult> AllFridges(int? locationId)
        {
            var query = _context.FridgeUnits
                .Include(f => f.Location)
                .Include(f => f.MaintenanceSchedules)
                .Include(f => f.Allocations.Where(a => a.DeallocationDate == null))
                    .ThenInclude(a => a.Customer)
                .Where(f => !f.IsDeleted); // Adjust based on your FridgeUnit properties

            if (locationId.HasValue && locationId.Value > 0)
            {
                query = query.Where(f => f.LocationId == locationId.Value);
                ViewBag.CurrentLocationId = locationId.Value;
            }

            var allFridgeUnits = await query.ToListAsync();

            var viewModel = allFridgeUnits.Select(f =>
            {
                var activeAllocation = f.Allocations.FirstOrDefault();
                var customer = activeAllocation?.Customer;

                return new MaintenanceFridgeListViewModel
                {
                    FridgeId = f.Id,
                    FridgeUnitId = f.Id,
                    SerialNumber = f.SerialNumber,
                    Model = f.FridgeModel.Name, // Get from FridgeModel
                    FridgeStatus = f.Status.ToString(),
                    CustomerBusinessName = customer?.BusinessName ?? "N/A",
                    CustomerAddress = customer?.PhysicalAddress ?? f.Location?.Description,
                    NextScheduledVisit = f.MaintenanceSchedules
                         .Where(ms => ms.ScheduledDate >= DateTime.Today)
                         .OrderBy(ms => ms.ScheduledDate)
                         .FirstOrDefault()
                         ?.ScheduledDate
                };
            }).OrderBy(vm => vm.CustomerBusinessName).ToList();

            // Populate Locations for Filter Dropdown
            var allLocations = await _context.Locations
                .OrderBy(l => l.Description)
                .Select(l => new SelectListItem { Value = l.Id.ToString(), Text = l.Description })
                .ToListAsync();

            allLocations.Insert(0, new SelectListItem { Value = "", Text = "All Locations" });
            ViewBag.Locations = allLocations;

            return View("AllocatedFridges", viewModel);
        }

        // GET: MaintenanceTechnician/FridgeDetails/5
        public async Task<IActionResult> FridgeDetails(int id)
        {
            var fridgeUnit = await _context.FridgeUnits
                .Include(f => f.Location)
                .Include(f => f.FridgeModel) // Include FridgeModel to get model name
                .Include(f => f.Faults.OrderByDescending(ff => ff.DateReported))
                .Include(f => f.ServiceRecords.OrderByDescending(sr => sr.DateOfService))
                .Include(f => f.MaintenanceSchedules)
                .Include(f => f.Allocations.Where(a => a.DeallocationDate == null))
                    .ThenInclude(a => a.Customer)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (fridgeUnit == null)
            {
                return NotFound();
            }

            var activeAllocation = fridgeUnit.Allocations.FirstOrDefault();
            var customer = activeAllocation?.Customer;

            var viewModel = new MaintenanceFridgeDetailsViewModel
            {
                FridgeSummary = new MaintenanceFridgeListViewModel
                {
                    FridgeId = fridgeUnit.Id,
                    FridgeUnitId = fridgeUnit.Id,
                    SerialNumber = fridgeUnit.SerialNumber,
                    Model = fridgeUnit.FridgeModel.Name, // Get from FridgeModel
                    FridgeStatus = fridgeUnit.Status.ToString(),
                    CustomerBusinessName = customer?.BusinessName ?? "N/A",
                    CustomerAddress = customer?.PhysicalAddress ?? fridgeUnit.Location?.Description,
                    NextScheduledVisit = fridgeUnit.MaintenanceSchedules
                        .Where(ms => ms.ScheduledDate >= DateTime.Today)
                        .OrderBy(ms => ms.ScheduledDate)
                        .FirstOrDefault()
                        ?.ScheduledDate
                },
                FaultHistory = fridgeUnit.Faults.ToList(),
                ServiceHistory = fridgeUnit.ServiceRecords.ToList(),
                UpcomingSchedules = fridgeUnit.MaintenanceSchedules
                    .Where(ms => ms.ScheduledDate >= DateTime.Today)
                    .OrderBy(ms => ms.ScheduledDate)
                    .ToList()
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
                .Include(ff => ff.FridgeUnit)
                .ThenInclude(fu => fu.FridgeModel)
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
                .Include(ff => ff.FridgeUnit)
                .ThenInclude(fu => fu.FridgeModel)
                .Include(ff => ff.FridgeUnit)
                .ThenInclude(fu => fu.CurrentCustomer)
                .Include(ff => ff.ReportedBy)
                .FirstOrDefaultAsync(ff => ff.Id == id && ff.AssignedTechnicianId == currentUserId);

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

        // POST: MaintenanceTechnician/CreateFaultReport
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFaultReport(CreateFaultReportViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateCreateFaultReportViewModel(model);
                return View(model);
            }

            var technicianId = _userManager.GetUserId(User);

            var newFault = new FridgeFault
            {
                FridgeUnitId = model.FridgeUnitId.Value, // Changed from FridgeId to FridgeUnitId
                DateReported = DateTime.Now,
                FaultDescription = model.Description,
                Severity = model.Severity,
                Status = FaultStatus.Reported,
                ReportedByUserId = technicianId,
                AssignedTechnicianId = technicianId
            };

            _context.FridgeFaults.Add(newFault);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Fault reported successfully.";
            return RedirectToAction("FridgeDetails", new { id = model.FridgeUnitId.Value });
        }

        //=========================================================
        // SERVICE RECORD CREATION
        //=========================================================

        // Helper to populate active faults and fridge context for the Service Record form
        private async Task PopulateCreateServiceRecordViewModel(CreateServiceRecordViewModel viewModel)
        {
            // Fetch SerialNumber if it is missing
            if (string.IsNullOrEmpty(viewModel.SerialNumber))
            {
                var fridgeUnit = await _context.FridgeUnits
                    .Include(fu => fu.FridgeModel)
                    .Where(fu => fu.Id == viewModel.FridgeUnitId)
                    .Select(fu => new { fu.SerialNumber, fu.FridgeModel.Name })
                    .FirstOrDefaultAsync();

                if (fridgeUnit != null)
                {
                    viewModel.SerialNumber = fridgeUnit.SerialNumber;
                }
            }

            // Fetch all currently active (unresolved) faults for the specific fridge unit
            var faults = await _context.FridgeFaults
                .Where(f => f.FridgeUnitId == viewModel.FridgeUnitId &&
                            (f.Status == FaultStatus.Reported || f.Status == FaultStatus.Assigned || f.Status == FaultStatus.InProgress))
                .OrderByDescending(f => f.DateReported)
                .ToListAsync();

            viewModel.AvailableFaults = faults.Select(f => new SelectListItem
            {
                Value = f.Id.ToString(),
                Text = $"Fault {f.Id} ({f.DateReported.ToString("yyyy-MM-dd")}): {f.FaultDescription.Substring(0, Math.Min(50, f.FaultDescription.Length))}..."
            }).ToList();
        }

        // GET: MaintenanceTechnician/CreateServiceRecord?fridgeUnitId=5
        public async Task<IActionResult> CreateServiceRecord(int fridgeUnitId, int? faultId)
        {
            var fridgeUnit = await _context.FridgeUnits
                .Include(fu => fu.FridgeModel)
                .Where(fu => fu.Id == fridgeUnitId)
                .Select(fu => new { fu.SerialNumber, fu.FridgeModel.Name })
                .FirstOrDefaultAsync();

            if (fridgeUnit == null)
            {
                TempData["Error"] = "Fridge unit not found.";
                return RedirectToAction(nameof(AllocatedFridges));
            }

            var viewModel = new CreateServiceRecordViewModel
            {
                FridgeUnitId = fridgeUnitId, // Changed from FridgeId to FridgeUnitId
                FaultId = faultId,
                DateOfService = DateTime.Now.Date,
                SerialNumber = fridgeUnit.SerialNumber
            };

            await PopulateCreateServiceRecordViewModel(viewModel);

            ViewData["Title"] = "Create Service Record";
            return View(viewModel);
        }

        // POST: MaintenanceTechnician/CreateServiceRecord
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateServiceRecord(CreateServiceRecordViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                // Your existing database save logic here
                await _context.SaveChangesAsync();

                TempData["Success"] = "Service record created successfully.";
                return RedirectToAction(nameof(FridgeDetails), new { id = viewModel.FridgeUnitId });
            }

            await PopulateCreateServiceRecordViewModel(viewModel);
            return View(viewModel);
        }

        //=========================================================
        // MAINTENANCE SCHEDULING
        //=========================================================

        // GET: MaintenanceTechnician/MaintenanceSchedules (List View)
        public async Task<IActionResult> MaintenanceSchedules()
        {
            var currentUserId = _userManager.GetUserId(User);
            var schedules = await _context.MaintenanceSchedules
                .Include(ms => ms.FridgeUnit)
                .ThenInclude(fu => fu.FridgeModel)
                .Include(ms => ms.FridgeUnit)
                .ThenInclude(fu => fu.CurrentCustomer)
                .Where(ms => ms.TechnicianId == currentUserId && ms.Status != "Completed")
                .OrderBy(ms => ms.ScheduledDate)
                .ToListAsync();

            ViewData["Title"] = "Scheduled Maintenance";
            return View(schedules);
        }

        // Helper to populate the FridgeUnit dropdown list and set FridgeDisplay
        private async Task PopulateCreateScheduleViewModel(CreateScheduleViewModel viewModel)
        {
            var currentUserId = _userManager.GetUserId(User);

            var fridgeUnits = await _context.FridgeUnits
                .Include(f => f.FridgeModel) // Include FridgeModel for model name
                .Include(f => f.Allocations.Where(a => a.DeallocationDate == null))
                    .ThenInclude(a => a.Customer)
                .Where(f => f.TechnicianUserId == currentUserId && f.Status == FridgeStatus.Allocated)
                .OrderBy(f => f.SerialNumber)
                .ToListAsync();

            // Populate both dropdown lists
            viewModel.AvailableFridges = fridgeUnits.Select(fridge =>
            {
                var activeAllocation = fridge.Allocations.FirstOrDefault();
                var customer = activeAllocation?.Customer;

                return new SelectListItem
                {
                    Value = fridge.Id.ToString(),
                    Text = $"{fridge.SerialNumber} ({fridge.FridgeModel.Name} - {customer?.BusinessName ?? "N/A Customer"})"
                };
            }).ToList();

            viewModel.AvailableFridgeUnits = fridgeUnits.Select(fridge =>
            {
                var activeAllocation = fridge.Allocations.FirstOrDefault();
                var customer = activeAllocation?.Customer;

                return new SelectListItem
                {
                    Value = fridge.Id.ToString(),
                    Text = $"{fridge.SerialNumber} ({fridge.FridgeModel.Name} - {customer?.BusinessName ?? "N/A Customer"})"
                };
            }).ToList();

            // Set FridgeDisplay context
            if (viewModel.FridgeId.HasValue)
            {
                var selectedFridge = fridgeUnits.FirstOrDefault(f => f.Id == viewModel.FridgeId.Value);
                if (selectedFridge != null)
                {
                    var activeAllocation = selectedFridge.Allocations.FirstOrDefault();
                    var customer = activeAllocation?.Customer;
                    viewModel.FridgeDisplay = $"{selectedFridge.SerialNumber} ({selectedFridge.FridgeModel.Name} - {customer?.BusinessName ?? "N/A Customer"})";
                }
            }
        }

        // GET: MaintenanceTechnician/CreateSchedule?fridgeUnitId=5
        public async Task<IActionResult> CreateSchedule(int? fridgeUnitId)
        {
            var viewModel = new CreateScheduleViewModel
            {
                FridgeUnitId = fridgeUnitId, // Changed from FridgeId to FridgeUnitId
                ScheduledDate = DateTime.Now.Date.AddHours(9).AddDays(7)
            };

            await PopulateCreateScheduleViewModel(viewModel);

            ViewData["Title"] = "Create Maintenance Schedule";
            return View(viewModel);
        }

        // POST: MaintenanceTechnician/CreateSchedule
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSchedule(CreateScheduleViewModel viewModel)
        {
            if (!viewModel.FridgeUnitId.HasValue)
            {
                ModelState.AddModelError(nameof(viewModel.FridgeUnitId), "A fridge unit must be selected.");
            }

            if (ModelState.IsValid)
            {
                var schedule = new MaintenanceSchedule
                {
                    FridgeUnitId = viewModel.FridgeUnitId.Value, // Changed from FridgeId to FridgeUnitId
                    TechnicianId = _userManager.GetUserId(User),
                    ScheduledDate = viewModel.ScheduledDate,
                    Description = viewModel.Description,
                    Status = "Scheduled"
                };

                _context.MaintenanceSchedules.Add(schedule);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Maintenance scheduled successfully for {viewModel.FridgeDisplay} on {viewModel.ScheduledDate:dd MMM yyyy @ HH:mm}.";

                return RedirectToAction(nameof(FridgeDetails), new { id = viewModel.FridgeUnitId.Value });
            }

            await PopulateCreateScheduleViewModel(viewModel);
            return View(viewModel);
        }

        //=========================================================
        // HELPERS
        //=========================================================

        // Helper to populate the FridgeUnit dropdown and Severity for fault reporting
        private async Task PopulateCreateFaultReportViewModel(CreateFaultReportViewModel viewModel)
        {
            var currentUserId = _userManager.GetUserId(User);
            var fridgeUnits = await _context.FridgeUnits
                .Include(f => f.FridgeModel) // Include FridgeModel for model name
                .Include(f => f.Allocations.Where(a => a.DeallocationDate == null))
                    .ThenInclude(a => a.Customer)
                .Where(f => f.TechnicianUserId == currentUserId && f.Status == FridgeStatus.Allocated)
                .OrderBy(f => f.SerialNumber)
                .ToListAsync();

            // Populate both AvailableFridges and AvailableFridgeUnits
            viewModel.AvailableFridges = fridgeUnits.Select(fridge =>
            {
                var activeAllocation = fridge.Allocations.FirstOrDefault();
                var customer = activeAllocation?.Customer;

                return new SelectListItem
                {
                    Value = fridge.Id.ToString(),
                    Text = $"{fridge.SerialNumber} ({fridge.FridgeModel.Name} - {customer?.BusinessName ?? "N/A"})"
                };
            }).ToList();

            viewModel.AvailableFridgeUnits = fridgeUnits.Select(fridge =>
            {
                var activeAllocation = fridge.Allocations.FirstOrDefault();
                var customer = activeAllocation?.Customer;

                return new SelectListItem
                {
                    Value = fridge.Id.ToString(),
                    Text = $"{fridge.SerialNumber} ({fridge.FridgeModel.Name} - {customer?.BusinessName ?? "N/A"})"
                };
            }).ToList();

            ViewBag.SeverityOptions = Enum.GetValues(typeof(FaultSeverity))
                .Cast<FaultSeverity>()
                .Select(v => new SelectListItem { Text = v.ToString(), Value = v.ToString() });
        }
    }
}