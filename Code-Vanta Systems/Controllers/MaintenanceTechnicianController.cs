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
        [HttpGet]
        public async Task<IActionResult> AllocatedFridges()
        {
            var currentUserId = _userManager.GetUserId(User);

            var allocatedFridges = await _context.Fridges
                .Include(f => f.Location)
                .Include(f => f.MaintenanceSchedules)
                // Include active Allocation record to get the current customer
                .Include(f => f.Allocations.Where(a => a.DeallocationDate == null))
                    .ThenInclude(a => a.Customer)
                .Where(f =>
                    f.TechnicianUserId == currentUserId &&  // Assigned to current technician
                    f.Condition == FridgeCondition.RequireMaintenance && // REQUIRES maintenance
                    !f.IsDeleted)
                .ToListAsync();

            var viewModel = allocatedFridges.Select(f =>
            {
                // Get the customer from the active allocation record
                var activeAllocation = f.Allocations.FirstOrDefault();
                var customer = activeAllocation?.Customer;

                return new MaintenanceFridgeListViewModel
                {
                    FridgeId = f.Id,
                    SerialNumber = f.SerialNumber,
                    Model = f.Model,
                    FridgeStatus = f.Status.ToString(),
                    Condition = f.Condition.ToString(), // Added this to see the condition

                    // Use the Customer from the Allocation table (the source of truth)
                    CustomerBusinessName = customer?.BusinessName ?? "N/A",
                    CustomerAddress = customer?.PhysicalAddress ?? f.Location?.Description,

                    NextScheduledVisit = f.MaintenanceSchedules
                         .Where(ms => ms.ScheduledDate >= DateTime.Today)
                         .OrderBy(ms => ms.ScheduledDate)
                         .FirstOrDefault()
                         ?.ScheduledDate
                };
            }).OrderBy(vm => vm.CustomerBusinessName).ToList();

            return View(viewModel);
        }

        // -------------------------------------------------------------

        // New: GET: MaintenanceTechnician/AllFridges (List of ALL fridges in the system)
        [HttpGet]
        public async Task<IActionResult> AllFridges(int? locationId) // Re-added the locationId for filtering
        {
            // 1. Start query for ALL fridges (excluding deleted)
            var query = _context.Fridges
                // REMOVED: .Include(f => f.Customer)
                .Include(f => f.Location)
                .Include(f => f.MaintenanceSchedules)
                // CRITICAL FIX: Include ONLY the active Allocation record to get the current customer
                .Include(f => f.Allocations.Where(a => a.DeallocationDate == null))
                    .ThenInclude(a => a.Customer)
                .Where(f => !f.IsDeleted);

            // 2. Apply Location Filter if a locationId is provided and valid (using existing logic)
            if (locationId.HasValue && locationId.Value > 0)
            {
                query = query.Where(f => f.LocationId == locationId.Value);
                ViewBag.CurrentLocationId = locationId.Value;
            }

            var allFridges = await query.ToListAsync();

            // 3. Map the Fridge models to your MaintenanceFridgeListViewModel
            var viewModel = allFridges.Select(f =>
            {
                // NEW: Get the customer from the active allocation record
                var activeAllocation = f.Allocations.FirstOrDefault();
                var customer = activeAllocation?.Customer;

                return new MaintenanceFridgeListViewModel
                {
                    FridgeId = f.Id,
                    SerialNumber = f.SerialNumber,
                    Model = f.Model,
                    FridgeStatus = f.Status.ToString(), // Provides the fridge status

                    // Use the Customer from the Allocation table (the source of truth)
                    CustomerBusinessName = customer?.BusinessName ?? "N/A",
                    CustomerAddress = customer?.PhysicalAddress ?? f.Location?.Description,

                    NextScheduledVisit = f.MaintenanceSchedules
                         .Where(ms => ms.ScheduledDate >= DateTime.Today)
                         .OrderBy(ms => ms.ScheduledDate)
                         .FirstOrDefault()
                         ?.ScheduledDate
                };
            }).OrderBy(vm => vm.CustomerBusinessName).ToList();

            // 4. Populate Locations for Filter Dropdown (using existing logic)
            var allLocations = await _context.Locations
                .OrderBy(l => l.Description)
                .Select(l => new SelectListItem { Value = l.Id.ToString(), Text = l.Description })
                .ToListAsync();

            allLocations.Insert(0, new SelectListItem { Value = "", Text = "All Locations" });
            ViewBag.Locations = allLocations;

            // 5. Reuse the existing view (AllocatedFridges.cshtml) to display the full list
            return View("AllocatedFridges", viewModel);
        }


        // GET: MaintenanceTechnician/FridgeDetails/5
        public async Task<IActionResult> FridgeDetails(int id)
        {
            var fridge = await _context.Fridges
                // REMOVED: .Include(f => f.Customer)
                .Include(f => f.Location)
                .Include(f => f.Faults.OrderByDescending(ff => ff.DateReported))          // Load Fault History
                .Include(f => f.ServiceRecords.OrderByDescending(sr => sr.DateOfService)) // Load Service History
                .Include(f => f.MaintenanceSchedules) // Load Maintenance Schedules
                                                      // CRITICAL FIX: Include ONLY the active Allocation record to get the current customer
                .Include(f => f.Allocations.Where(a => a.DeallocationDate == null))
                    .ThenInclude(a => a.Customer)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (fridge == null)
            {
                return NotFound();
            }

            // NEW: Get the customer from the active allocation record
            var activeAllocation = fridge.Allocations.FirstOrDefault();
            var customer = activeAllocation?.Customer;

            // Map the fetched data to the ViewModel
            var viewModel = new MaintenanceFridgeDetailsViewModel
            {
                // 1. Map the Summary Data (Using the structure from MaintenanceFridgeListViewModel)
                FridgeSummary = new MaintenanceFridgeListViewModel
                {
                    FridgeId = fridge.Id,
                    SerialNumber = fridge.SerialNumber,
                    Model = fridge.Model,
                    FridgeStatus = fridge.Status.ToString(),

                    // Use the Customer from the Allocation table
                    CustomerBusinessName = customer?.BusinessName ?? "N/A",
                    CustomerAddress = customer?.PhysicalAddress ?? fridge.Location?.Description,
                    // Find the closest upcoming schedule
                    NextScheduledVisit = fridge.MaintenanceSchedules
                        .Where(ms => ms.ScheduledDate >= DateTime.Today)
                        .OrderBy(ms => ms.ScheduledDate)
                        .FirstOrDefault()
                        ?.ScheduledDate
                },

                // 2. Map the History Data (This fixes the empty sections)
                FaultHistory = fridge.Faults.ToList(), // Already ordered by Include

                ServiceHistory = fridge.ServiceRecords.ToList(), // Already ordered by Include

                // Filter upcoming schedules to only show future dates
                UpcomingSchedules = fridge.MaintenanceSchedules
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



        // POST: MaintenanceTechnician/CreateFaultReport
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFaultReport(CreateFaultReportViewModel model)
        {
            // Step 1: Validation Check
            if (!ModelState.IsValid)
            {
                // If the user didn't select a severity, ModelState will be invalid
                // and this code will correctly reload the form with an error message.
                await PopulateCreateFaultReportViewModel(model);
                return View(model);
            }

            // Step 2: Create Model and Map Data (This is only reached if validation succeeds)
            var technicianId = _userManager.GetUserId(User);

            var newFault = new FridgeFault
            {
                FridgeId = model.FridgeId.Value,
                DateReported = DateTime.Now,
                FaultDescription = model.Description,
                Severity = model.Severity,
                Status = FaultStatus.Reported,
                ReportedByUserId = technicianId,
                AssignedTechnicianId = technicianId
            };

            // Step 3: Save to Database
            _context.FridgeFaults.Add(newFault);
            await _context.SaveChangesAsync();

            // Step 4: Success and Redirect
            TempData["Success"] = "Fault reported successfully.";
            return RedirectToAction("FridgeDetails", new { id = model.FridgeId.Value });
        }

        //=========================================================
        // SERVICE RECORD CREATION
        //=========================================================

        // Helper to populate active faults and fridge context for the Service Record form
        private async Task PopulateCreateServiceRecordViewModel(CreateServiceRecordViewModel viewModel)
        {
            // Fetch SerialNumber if it is missing (needed for view title after POST failure)
            if (string.IsNullOrEmpty(viewModel.SerialNumber))
            {
                var fridge = await _context.Fridges
                    .Where(f => f.Id == viewModel.FridgeId)
                    .Select(f => new { f.SerialNumber })
                    .FirstOrDefaultAsync();

                if (fridge != null)
                {
                    viewModel.SerialNumber = fridge.SerialNumber;
                }
            }

            // Fetch all currently active (unresolved) faults for the specific fridge
            var faults = await _context.FridgeFaults
                .Where(f => f.FridgeId == viewModel.FridgeId &&
                            (f.Status == FaultStatus.Reported || f.Status == FaultStatus.Assigned || f.Status == FaultStatus.InProgress))
                .OrderByDescending(f => f.DateReported)
                .ToListAsync();

            viewModel.AvailableFaults = faults.Select(f => new SelectListItem
            {
                Value = f.Id.ToString(),
                // Display both ID, Date, and a snippet of the description
                Text = $"Fault {f.Id} ({f.DateReported.ToString("yyyy-MM-dd")}): {f.FaultDescription.Substring(0, Math.Min(50, f.FaultDescription.Length))}..."
            }).ToList();
        }

        // GET: MaintenanceTechnician/CreateServiceRecord?fridgeId=5
        public async Task<IActionResult> CreateServiceRecord(int fridgeId, int? faultId)
        {
            // 1. Fetch context data (e.g., SerialNumber)
            var fridge = await _context.Fridges
                .Where(f => f.Id == fridgeId)
                .Select(f => new { f.SerialNumber })
                .FirstOrDefaultAsync();

            if (fridge == null)
            {
                TempData["Error"] = "Fridge not found.";
                return RedirectToAction(nameof(AllocatedFridges));
            }

            // 2. Initialize ViewModel
            var viewModel = new CreateServiceRecordViewModel
            {
                FridgeId = fridgeId,
                FaultId = faultId,
                DateOfService = DateTime.Now.Date, // Use .Date to avoid time issues
                SerialNumber = fridge.SerialNumber // For display in the view title
            };

            // 3. Populate dynamic lists
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
                // ... (Your existing database save logic is correct here)
                // ...
                await _context.SaveChangesAsync();

                TempData["Success"] = "Service record created successfully.";
                // CRITICAL: Redirect back to the Fridge Details page for context
                return RedirectToAction(nameof(FridgeDetails), new { id = viewModel.FridgeId });
            }

            // If validation fails (ModelState.IsValid is false):
            // CRITICAL FIX: Re-populate dynamic data (AvailableFaults) and context (SerialNumber)
            await PopulateCreateServiceRecordViewModel(viewModel);

            // Return the view with the model to show validation errors
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

        // Helper to populate the Fridge dropdown list and set FridgeDisplay
        private async Task PopulateCreateScheduleViewModel(CreateScheduleViewModel viewModel)
        {
            var currentUserId = _userManager.GetUserId(User);

            // 1. Fetch ALL allocated fridges for the current technician
            var fridges = await _context.Fridges
                // CRITICAL FIX: Include active allocation records to get the current customer
                .Include(f => f.Allocations.Where(a => a.DeallocationDate == null))
                    .ThenInclude(a => a.Customer)
                .Where(f => f.TechnicianUserId == currentUserId && f.Status == FridgeStatus.Allocated)
                .OrderBy(f => f.SerialNumber)
                .ToListAsync();

            // 2. Populate the Fridge dropdown list
            viewModel.AvailableFridges = fridges.Select(fridge =>
            {
                var activeAllocation = fridge.Allocations.FirstOrDefault();
                var customer = activeAllocation?.Customer;

                return new SelectListItem
                {
                    Value = fridge.Id.ToString(),
                    // Use the Customer from the Allocation
                    Text = $"{fridge.SerialNumber} ({customer?.BusinessName ?? "N/A Customer"})"
                };
            }).ToList();

            // 3. Set the FridgeDisplay context if an ID is present (Prevents NullReferenceException)
            if (viewModel.FridgeId.HasValue)
            {
                var selectedFridge = fridges.FirstOrDefault(f => f.Id == viewModel.FridgeId.Value);
                if (selectedFridge != null)
                {
                    var activeAllocation = selectedFridge.Allocations.FirstOrDefault();
                    var customer = activeAllocation?.Customer;
                    viewModel.FridgeDisplay = $"{selectedFridge.SerialNumber} ({customer?.BusinessName ?? "N/A Customer"})";
                }
            }
        }

        // GET Method for Creating a Schedule
        // GET: MaintenanceTechnician/CreateSchedule?fridgeId=5
        public async Task<IActionResult> CreateSchedule(int? fridgeId)
        {
            var viewModel = new CreateScheduleViewModel
            {
                FridgeId = fridgeId,
                ScheduledDate = DateTime.Now.Date.AddHours(9).AddDays(7)
            };

            await PopulateCreateScheduleViewModel(viewModel);

            ViewData["Title"] = "Create Maintenance Schedule";
            return View(viewModel);
        }

        // POST Method for Creating a Schedule (CRITICAL ACTION)
        // POST: MaintenanceTechnician/CreateSchedule
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSchedule(CreateScheduleViewModel viewModel)
        {
            // 1. Mandatory check for FridgeId
            if (!viewModel.FridgeId.HasValue)
            {
                ModelState.AddModelError(nameof(viewModel.FridgeId), "A fridge must be selected.");
            }

            if (ModelState.IsValid)
            {
                var schedule = new MaintenanceSchedule
                {
                    FridgeId = viewModel.FridgeId.Value,
                    TechnicianId = _userManager.GetUserId(User), // Using TechnicianId property
                    ScheduledDate = viewModel.ScheduledDate,
                    Description = viewModel.Description,
                    Status = "Scheduled"
                };

                _context.MaintenanceSchedules.Add(schedule);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Maintenance scheduled successfully for {viewModel.FridgeDisplay} on {viewModel.ScheduledDate:dd MMM yyyy @ HH:mm}.";

                // Redirect to FridgeDetails
                return RedirectToAction(nameof(FridgeDetails), new { id = viewModel.FridgeId.Value });
            }

            // 2. DIRECT FIX: Re-populate context and dropdowns on failure
            await PopulateCreateScheduleViewModel(viewModel);

            // This ensures Model.FridgeDisplay is available for the view header
            return View(viewModel);
        }

        //=========================================================
        // HELPERS
        //=========================================================

        // Helper to populate the Fridge dropdown and Severity for fault reporting
        private async Task PopulateCreateFaultReportViewModel(CreateFaultReportViewModel viewModel)
        {
            var currentUserId = _userManager.GetUserId(User);
            var fridges = await _context.Fridges
                // CRITICAL FIX: Include active allocation records to get the current customer
                .Include(f => f.Allocations.Where(a => a.DeallocationDate == null))
                    .ThenInclude(a => a.Customer)
                .Where(f => f.TechnicianUserId == currentUserId && f.Status == FridgeStatus.Allocated)
                .OrderBy(f => f.SerialNumber)
                .ToListAsync();

            // FIX: Resolved CS1061/CS0103 by ensuring AvailableFridges exists on ViewModel and using 'fridge'
            viewModel.AvailableFridges = fridges.Select(fridge =>
            {
                var activeAllocation = fridge.Allocations.FirstOrDefault();
                var customer = activeAllocation?.Customer;

                return new SelectListItem
                {
                    Value = fridge.Id.ToString(),
                    // Use the Customer from the Allocation
                    Text = $"{fridge.SerialNumber} ({customer?.BusinessName ?? "N/A"})"
                };
            }).ToList();

            ViewBag.SeverityOptions = Enum.GetValues(typeof(FaultSeverity))
                .Cast<FaultSeverity>()
                .Select(v => new SelectListItem { Text = v.ToString(), Value = v.ToString() });
        }
    }
}