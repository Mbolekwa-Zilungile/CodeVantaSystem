using Code_Vanta_Systems.Data;
using Code_Vanta_Systems.Models;
using Code_Vanta_Systems.Models.Enums;
using Code_Vanta_Systems.ViewModels;
using Code_Vanta_Systems.ViewModels.Customer;
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

            // Dynamically count allocated *units* (not models)
            var allocatedFridgesCount = await _context.FridgeUnits
                .CountAsync(u => u.TechnicianUserId == currentUserId && u.IsRented);

            // Dynamically count open faults assigned to this technician
            ViewBag.OpenFaultsCount = await _context.FridgeFaults
                .CountAsync(ff => ff.AssignedTechnicianId == currentUserId && ff.Status == FaultStatus.Open);

            // FIX: Use TechnicianId and check for non-Completed status for schedules
            ViewBag.ScheduledMaintenanceCount = await _context.MaintenanceSchedules
                .CountAsync(ms => ms.TechnicianId == currentUserId && ms.Status != "Completed");

            ViewBag.AllocatedFridgesCount = allocatedFridgesCount;

            return View();
        }

        //=========================================================
        // ASSET MANAGEMENT & HISTORY (VIEWING)
        // Focus is now entirely on FridgeUnit
        //=========================================================


        // GET: MaintenanceTechnician/AllocatedFridges (List of fridges assigned to the current technician)
        [HttpGet]
        public async Task<IActionResult> AllocatedFridges()
        {
            var currentUserId = _userManager.GetUserId(User);

            // Query only the FridgeUnits assigned to the current technician
            var allocatedUnits = await _context.FridgeUnits
                .Include(u => u.Fridge)
                .Include(u => u.Customer)
                    .ThenInclude(c => c.Location)
                .Include(u => u.MaintenanceSchedules.Where(ms => ms.Status != "Completed")) // Upcoming schedules
                .Where(u =>
                    // Filter by the assigned technician on the UNIT
                    u.TechnicianUserId == currentUserId &&
                    u.IsRented && // Filter for units currently allocated to a customer
                    !u.IsDeleted)
                .ToListAsync();

            var viewModel = allocatedUnits.Select(u =>
            {
                var customer = u.Customer;

                return new MaintenanceFridgeListViewModel
                {
                    // CRITICAL: Use the FridgeUnit ID
                    FridgeId = u.Id,
                    SerialNumber = u.SerialNumber,
                    Model = u.Fridge.Model,
                    FridgeStatus = u.Status.ToString(),

                    CustomerBusinessName = customer?.BusinessName ?? "N/A",
                    CustomerAddress = customer?.PhysicalAddress ?? "N/A",

                    // Find the closest upcoming schedule linked to the UNIT
                    NextScheduledVisit = u.MaintenanceSchedules?
                         .Where(ms => ms.ScheduledDate >= DateTime.Today)
                         .OrderBy(ms => ms.ScheduledDate)
                         .FirstOrDefault()
                         ?.ScheduledDate
                };
            }).OrderBy(vm => vm.CustomerBusinessName).ToList();

            ViewData["Title"] = "My Allocated Fridge Units";
            return View(viewModel);
        }

        // -------------------------------------------------------------

        // New: GET: MaintenanceTechnician/AllFridges (List of ALL fridges in the system - UNITS)
        [HttpGet]
        public async Task<IActionResult> AllFridges(int? locationId, bool? isAllocated)
        {
            // 1. Start query for ALL *UNITS* (excluding deleted)
            var query = _context.FridgeUnits
                .Include(u => u.Fridge)
                    .ThenInclude(f => f.Location) // Get the warehouse location if not allocated
                .Include(u => u.Customer) // Get the customer if allocated
                .Include(u => u.MaintenanceSchedules.Where(ms => ms.Status != "Completed")) // Upcoming schedules
                .Where(u => !u.IsDeleted);

            // 2. Apply Location Filter (based on the UNIT's current location)
            if (locationId.HasValue && locationId.Value > 0)
            {
                // Filter by customer location (if rented) OR warehouse location (if in stock)
                query = query.Where(u =>
                    (u.IsRented && u.Customer.LocationId == locationId.Value) ||
                    (!u.IsRented && u.Fridge.LocationId == locationId.Value));

                ViewBag.CurrentLocationId = locationId.Value;
            }

            // 3. APPLY NEW FILTER: Filter by assignment status to a customer
            if (isAllocated.HasValue)
            {
                query = query.Where(u => u.IsRented == isAllocated.Value);
                ViewBag.IsAllocated = isAllocated.Value;
            }


            var allUnits = await query.ToListAsync();

            // 4. Map the Fridge UNITS to your MaintenanceFridgeListViewModel
            var viewModel = allUnits.Select(u =>
            {
                var customer = u.Customer;

                return new MaintenanceFridgeListViewModel
                {
                    FridgeId = u.Id, // CRITICAL: This is the Unit ID
                    SerialNumber = u.SerialNumber,
                    Model = u.Fridge.Model,
                    FridgeStatus = u.Status.ToString(),

                    // Determine Name and Address based on allocation
                    CustomerBusinessName = u.IsRented ? customer?.BusinessName ?? "N/A" : "In Stock",
                    CustomerAddress = u.IsRented ? customer?.PhysicalAddress ?? "N/A" : u.Fridge.Location?.Description,

                    // Find the closest upcoming schedule linked to the UNIT
                    NextScheduledVisit = u.MaintenanceSchedules
                         .Where(ms => ms.ScheduledDate >= DateTime.Today)
                         .OrderBy(ms => ms.ScheduledDate)
                         .FirstOrDefault()
                         ?.ScheduledDate
                };
            }).OrderBy(vm => vm.CustomerBusinessName).ToList();

            // 5. Populate Locations for Filter Dropdown
            var allLocations = await _context.Locations
                .OrderBy(l => l.Description)
                .Select(l => new SelectListItem { Value = l.Id.ToString(), Text = l.Description })
                .ToListAsync();

            allLocations.Insert(0, new SelectListItem { Value = "", Text = "All Locations" });
            ViewBag.Locations = allLocations;

            // 6. Reuse the existing view (AllocatedFridges.cshtml) to display the full list
            ViewData["Title"] = "All Fridge Units";
            return View("AllocatedFridges", viewModel);
        }


        // GET: MaintenanceTechnician/FridgeDetails/5 (ID is now FridgeUnitId)
        public async Task<IActionResult> FridgeDetails(int id)
        {
            // 1. Fetch the specific Fridge UNIT, including all necessary related data
            var unit = await _context.FridgeUnits
                .Include(u => u.Fridge)         // Unit's general model data
                .Include(u => u.Customer)       // Unit's assigned customer
                .Include(u => u.Technician)     // Unit's assigned technician (if applicable)

                // Include Unit-level history
                .Include(u => u.Faults.OrderByDescending(f => f.DateReported))
                .Include(u => u.ServiceRecords.OrderByDescending(s => s.DateOfService))
                .Include(u => u.MaintenanceSchedules)

                .FirstOrDefaultAsync(u => u.Id == id); // Filter by UNIT ID

            if (unit == null)
            {
                return NotFound();
            }

            // 2. Map the SINGLE UNIT's data to the ViewModel
            var viewModel = new MaintenanceFridgeDetailsViewModel
            {
                // Use the unit's data to populate the summary card
                FridgeSummary = new MaintenanceFridgeListViewModel
                {
                    FridgeId = unit.Id,
                    SerialNumber = unit.SerialNumber,
                    Model = unit.Fridge.Model,
                    FridgeStatus = unit.Status.ToString(),
                    CustomerBusinessName = unit.Customer?.BusinessName ?? "In Stock",
                    CustomerAddress = unit.Customer?.PhysicalAddress ?? unit.Fridge.Location?.Description,

                    // Find the closest upcoming schedule linked to the UNIT
                    NextScheduledVisit = unit.MaintenanceSchedules
                         .Where(ms => ms.ScheduledDate >= DateTime.Today)
                         .OrderBy(ms => ms.ScheduledDate)
                         .FirstOrDefault()
                         ?.ScheduledDate
                },

                // UnitDetailViewModel mapping (already done in original)
                UnitDetails = new List<UnitDetailViewModel>
                {
                    new UnitDetailViewModel
                    {
                        UnitId = unit.Id,
                        SerialNumber = unit.SerialNumber,
                        Condition = unit.Condition.ToString(),
                        UnitStatus = unit.Status.ToString(),
                        CurrentCustomer = unit.Customer?.BusinessName ?? "In Stock",
                        AssignedTechnician = unit.Technician?.FullName ?? "N/A"
                    }
                },

                // 3. Map the Unit's History
                FaultHistory = unit.Faults.ToList(),
                ServiceHistory = unit.ServiceRecords.ToList(),
                UpcomingSchedules = unit.MaintenanceSchedules
                    .Where(ms => ms.Status != "Completed") // Show non-completed only
                    .OrderBy(ms => ms.ScheduledDate)
                    .ToList()
            };

            // Pass the Unit's ID to the view for links to CreateFault/Service/Schedule
            ViewBag.UnitId = unit.Id;
            ViewBag.FridgeModelId = unit.FridgeId; // Needed if linking back to the Fridge model inventory view

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
                .Include(ff => ff.FridgeUnit) // Include the Unit it belongs to
                    .ThenInclude(u => u.Customer)
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
                .Include(ff => ff.FridgeUnit) // Include the Unit it belongs to
                    .ThenInclude(u => u.Customer)
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

            // Pass UnitId to the view for quick links
            ViewBag.UnitId = fault.FridgeUnitId;

            ViewData["Title"] = $"Fault Report: {fault.Id}";
            return View(fault);
        }

        // GET: MaintenanceTechnician/CreateFaultReport?unitId=5 (New GET method signature)
        public async Task<IActionResult> CreateFaultReport(int? unitId)
        {
            var viewModel = new CreateFaultReportViewModel { FridgeUnitId = unitId };
            await PopulateCreateFaultReportViewModel(viewModel);

            // Fetch the Unit's SerialNumber for the view title/context
            if (unitId.HasValue)
            {
                var unit = await _context.FridgeUnits.FindAsync(unitId.Value);
                if (unit != null)
                {
                    ViewBag.UnitSerialNumber = unit.SerialNumber;
                }
            }

            ViewData["Title"] = "Create Fault Report";
            return View(viewModel);
        }


        // POST: MaintenanceTechnician/CreateFaultReport
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFaultReport(CreateFaultReportViewModel model)
        {
            // Step 1: Validation Check
            if (!ModelState.IsValid)
            {
                // Re-populate data for the view
                await PopulateCreateFaultReportViewModel(model);
                return View(model);
            }

            // Step 2: Create Model and Map Data
            var technicianId = _userManager.GetUserId(User);

            var newFault = new FridgeFault
            {
                // CRITICAL FIX: Use FridgeUnitId
                FridgeUnitId = model.FridgeUnitId.Value,
                // CRITICAL: Must also supply FridgeId for foreign key on the Fault model
                FridgeId = _context.FridgeUnits.Find(model.FridgeUnitId.Value)?.FridgeId ?? 0,
                DateReported = DateTime.Now,
                FaultDescription = model.Description,
                Severity = model.Severity,
                Status = FaultStatus.Reported,
                ReportedByUserId = technicianId,
                AssignedTechnicianId = technicianId // Assign to self immediately
            };

            // Step 3: Save to Database
            _context.FridgeFaults.Add(newFault);
            await _context.SaveChangesAsync();

            // Step 4: Success and Redirect
            TempData["Success"] = "Fault reported successfully.";
            // CRITICAL: Redirect back to the FridgeUnit Details page
            return RedirectToAction(nameof(FridgeDetails), new { id = model.FridgeUnitId.Value });
        }

        //=========================================================
        // SERVICE RECORD CREATION
        //=========================================================

        // Helper to populate active faults and fridge context for the Service Record form
        private async Task PopulateCreateServiceRecordViewModel(CreateServiceRecordViewModel viewModel)
        {
            // Fetch SerialNumber if it is missing (needed for view title after POST failure)
            if (!viewModel.FridgeUnitId.HasValue || string.IsNullOrEmpty(viewModel.SerialNumber))
            {
                var unit = await _context.FridgeUnits
                    .Where(u => u.Id == viewModel.FridgeUnitId.Value)
                    .Select(u => new { u.SerialNumber })
                    .FirstOrDefaultAsync();

                if (unit != null)
                {
                    viewModel.SerialNumber = unit.SerialNumber;
                }
            }

            // 2. Populate Maintenance Types (Placeholder Solution - Correctly populates AvailableMaintenanceTypes)
            var maintenanceTypes = new List<SelectListItem>
    {
        new SelectListItem { Value = "1", Text = "Routine Coil Cleaning" },
        new SelectListItem { Value = "2", Text = "Refrigerant Pressure Check" },
        new SelectListItem { Value = "3", Text = "Electrical System Inspection" },
        new SelectListItem { Value = "4", Text = "Door Seal and Hinge Replacement" },
        new SelectListItem { Value = "5", Text = "Diagnostic Repair (General)" },
        new SelectListItem { Value = "6", Text = "Emergency Callout" }
    };

            viewModel.AvailableMaintenanceTypes = maintenanceTypes;


            // 3. RESTORED LOGIC: Fault fetching for the 'Resolved Fault' dropdown
            if (viewModel.FridgeUnitId.HasValue)
            {
                // Fetch all currently active (unresolved) faults for the specific fridge UNIT
                var faults = await _context.FridgeFaults
                    .Where(f => f.FridgeUnitId == viewModel.FridgeUnitId.Value &&
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
        }

        // GET: MaintenanceTechnician/CreateServiceRecord?unitId=5 (New GET method signature)
        public async Task<IActionResult> CreateServiceRecord(int unitId, int? faultId)
        {
            // 1. Fetch context data (e.g., SerialNumber)
            var unit = await _context.FridgeUnits
                .Where(u => u.Id == unitId)
                .Select(u => new { u.SerialNumber, u.FridgeId })
                .FirstOrDefaultAsync();

            if (unit == null)
            {
                TempData["Error"] = "Fridge Unit not found.";
                return RedirectToAction(nameof(AllocatedFridges));
            }

            // 2. Initialize ViewModel
            var viewModel = new CreateServiceRecordViewModel
            {
                // CRITICAL FIX: Set FridgeUnitId
                FridgeUnitId = unitId,
                // Also set FridgeId (needed by ServiceRecord model)
                FridgeId = unit.FridgeId,
                FaultId = faultId,
                DateOfService = DateTime.Now.Date, // Use .Date to avoid time issues
                SerialNumber = unit.SerialNumber // For display in the view title
            };

            // 3. Populate dynamic lists
            await PopulateCreateServiceRecordViewModel(viewModel);

            ViewData["Title"] = $"Create Service Record for {unit.SerialNumber}";
            return View(viewModel);
        }


        // POST: MaintenanceTechnician/CreateServiceRecord
        // Inside MaintenanceTechnicianController.cs

        // POST: MaintenanceTechnician/CreateServiceRecord
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateServiceRecord(CreateServiceRecordViewModel viewModel)
        {
            // --- 1. PRE-VALIDATION CHECK ---
            if (!ModelState.IsValid)
            {
                // CRITICAL FIX: Re-populate dynamic data and context on failure
                await PopulateCreateServiceRecordViewModel(viewModel);
                return View(viewModel);
            }

            var technicianId = _userManager.GetUserId(User);

            // --- 2. FETCH AND PREPARE DATA ---
            var unitToUpdate = await _context.FridgeUnits.FindAsync(viewModel.FridgeUnitId.Value);

            if (unitToUpdate == null)
            {
                TempData["ErrorMessage"] = "Error: Fridge Unit not found.";
                return RedirectToAction(nameof(AllocatedFridges));
            }

            // --- 3. CREATE SERVICE RECORD ---
            var serviceRecord = new ServiceRecord
            {
                FridgeUnitId = viewModel.FridgeUnitId.Value,
                FridgeId = viewModel.FridgeId.Value,
                DateOfService = viewModel.DateOfService,
                TechnicianUserId = technicianId,
                ServiceDetails = viewModel.Description, // Maps ViewModel.Description to Model.ServiceDetails
                PartsUsed = viewModel.PartsUsed,
                FaultId = viewModel.FaultId,
                MaintenanceTypeId = viewModel.MaintenanceTypeId,
                Cost = viewModel.Cost,
            };

            _context.ServiceRecords.Add(serviceRecord);

            // --- 4. HANDLE SCHEDULE COMPLETION (Maintenance System Action) ---
            // Automatically find and close the oldest non-completed schedule for this unit on this date.
            var matchingSchedule = await _context.MaintenanceSchedules
                .FirstOrDefaultAsync(ms =>
                    ms.FridgeUnitId == viewModel.FridgeUnitId.Value &&
                    ms.Status != "Completed" &&
                    ms.ScheduledDate.Date == viewModel.DateOfService.Date);

            // --- 5. HANDLE FAULT RESOLUTION ---
            // If a fault ID was selected, update its status to Repaired
            if (viewModel.FaultId.HasValue)
            {
                var fault = await _context.FridgeFaults.FindAsync(viewModel.FaultId.Value);
                if (fault != null)
                {
                    fault.Status = FaultStatus.Repaired;
                    fault.ResolutionDate = DateTime.Now;
                    _context.FridgeFaults.Update(fault);
                }
            }

            // --- 6. UPDATE UNIT STATUS & CONDITION (Proactive Maintenance) ---
            // The technician's final action on the physical unit.
            if (unitToUpdate != null)
            {
                unitToUpdate.Condition = viewModel.NewCondition;
                unitToUpdate.Status = FridgeStatus.Operational; // Assume service makes it operational
                unitToUpdate.UpdatedAt = DateTime.Now;
                // The TechnicianUserId is NOT cleared here, as the Maintenance Technician is the oversight.
            }

            // --- 7. SAVE ALL CHANGES ---
            try
            {
                if (matchingSchedule != null)
                {
                    matchingSchedule.Status = "Completed";
                    _context.MaintenanceSchedules.Update(matchingSchedule);
                    TempData["Info"] = $"Scheduled visit {matchingSchedule.Id} marked as Completed.";
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Service record created successfully.";
            }
            catch (Exception ex)
            {
                // Log exception and return detailed error
                ModelState.AddModelError("", "Unable to save service record. Check server logs.");
                await PopulateCreateServiceRecordViewModel(viewModel);
                return View(viewModel);
            }

            return RedirectToAction(nameof(FridgeDetails), new { id = viewModel.FridgeUnitId.Value });
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
                // CRITICAL: Change to FridgeUnit instead of Fridge
                .Include(ms => ms.FridgeUnit)
                    .ThenInclude(u => u.Customer)
               .Where(ms => ms.TechnicianId == currentUserId && ms.Status != "Completed")
               .OrderBy(ms => ms.ScheduledDate)
               .ToListAsync();

            ViewData["Title"] = "Scheduled Maintenance";
            return View(schedules);
        }

        // Helper to populate the Fridge *Unit* dropdown list and set FridgeDisplay
        private async Task PopulateCreateScheduleViewModel(CreateScheduleViewModel viewModel)
        {
            var currentUserId = _userManager.GetUserId(User);

            // 1. Fetch ALL allocated *units* for the current technician
            var units = await _context.FridgeUnits
                .Include(u => u.Customer)
                .Include(u => u.Fridge)
                // Filter by technician assignment and rental status
                .Where(u => u.TechnicianUserId == currentUserId && u.IsRented)
                .OrderBy(u => u.SerialNumber)
                .ToListAsync();

            // 2. Populate the Fridge dropdown list
            viewModel.AvailableFridges = units.Select(unit =>
            {
                return new SelectListItem
                {
                    Value = unit.Id.ToString(),
                    // Display the Unit Serial Number and the Customer
                    Text = $"{unit.SerialNumber} ({unit.Customer?.BusinessName ?? "N/A Customer"})"
                };
            }).ToList();

            // 3. Set the FridgeDisplay context if an ID is present
            if (viewModel.FridgeUnitId.HasValue)
            {
                var selectedUnit = units.FirstOrDefault(u => u.Id == viewModel.FridgeUnitId.Value);
                if (selectedUnit != null)
                {
                    viewModel.FridgeDisplay = $"{selectedUnit.SerialNumber} ({selectedUnit.Customer?.BusinessName ?? "N/A Customer"})";
                }
            }
        }

        // GET Method for Creating a Schedule
        // GET: MaintenanceTechnician/CreateSchedule?unitId=5 (New GET method signature)
        public async Task<IActionResult> CreateSchedule(int? unitId)
        {
            var viewModel = new CreateScheduleViewModel
            {
                FridgeUnitId = unitId,
                ScheduledDate = DateTime.Now.Date.AddHours(9).AddDays(7)
            };

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
            // 1. Mandatory check for FridgeUnitId
            if (!viewModel.FridgeUnitId.HasValue)
            {
                ModelState.AddModelError(nameof(viewModel.FridgeUnitId), "A fridge unit must be selected.");
            }

            if (ModelState.IsValid)
            {
                var unit = await _context.FridgeUnits.FindAsync(viewModel.FridgeUnitId.Value);
                if (unit == null)
                {
                    ModelState.AddModelError("", "Selected Fridge Unit not found.");
                    await PopulateCreateScheduleViewModel(viewModel);
                    return View(viewModel);
                }

                var schedule = new MaintenanceSchedule
                {
                    // CRITICAL FIX: Map to FridgeUnitId (and FridgeId if required by MaintenanceSchedule model)
                    FridgeUnitId = viewModel.FridgeUnitId.Value,
                    FridgeId = unit.FridgeId, // Get the model ID from the unit
                    TechnicianId = _userManager.GetUserId(User),
                    ScheduledDate = viewModel.ScheduledDate,
                    Description = viewModel.Description,
                    Status = "Scheduled"
                };

                _context.MaintenanceSchedules.Add(schedule);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Maintenance scheduled successfully for {viewModel.FridgeDisplay} on {viewModel.ScheduledDate:dd MMM yyyy @ HH:mm}.";

                // Redirect to FridgeUnit Details
                return RedirectToAction(nameof(FridgeDetails), new { id = viewModel.FridgeUnitId.Value });
            }

            // 2. DIRECT FIX: Re-populate context and dropdowns on failure
            await PopulateCreateScheduleViewModel(viewModel);

            return View(viewModel);
        }

        // Inside MaintenanceTechnicianController.cs

        // POST: MaintenanceTechnician/CancelSchedule/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelSchedule(int id)
        {
            var currentUserId = _userManager.GetUserId(User);
            var schedule = await _context.MaintenanceSchedules
                .FirstOrDefaultAsync(ms => ms.Id == id && ms.TechnicianId == currentUserId);

            if (schedule == null)
            {
                TempData["ErrorMessage"] = "Schedule not found or you are not authorized to cancel it.";
                return RedirectToAction(nameof(MaintenanceSchedules));
            }

            schedule.Status = "Cancelled";
            _context.MaintenanceSchedules.Update(schedule);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Scheduled visit for unit {schedule.FridgeUnitId} has been cancelled.";
            return RedirectToAction(nameof(MaintenanceSchedules));
        }

        //=========================================================
        // HELPERS
        //=========================================================

        // Helper to populate the Fridge *Unit* dropdown and Severity for fault reporting
        private async Task PopulateCreateFaultReportViewModel(CreateFaultReportViewModel viewModel)
        {
            var currentUserId = _userManager.GetUserId(User);

            // Fetch only the units assigned to the technician and currently rented
            var units = await _context.FridgeUnits
                .Include(u => u.Customer)
                .Where(u => u.TechnicianUserId == currentUserId && u.IsRented)
                .OrderBy(u => u.SerialNumber)
                .ToListAsync();

            // FIX: Resolved CS1061/CS0103 by ensuring AvailableFridges exists on ViewModel and using 'unit'
            viewModel.AvailableFridges = units.Select(unit =>
            {
                return new SelectListItem
                {
                    Value = unit.Id.ToString(), // Use Unit ID
                    // Use the Customer from the Unit
                    Text = $"{unit.SerialNumber} ({unit.Customer?.BusinessName ?? "N/A"})"
                };
            }).ToList();

            ViewBag.SeverityOptions = Enum.GetValues(typeof(FaultSeverity))
                .Cast<FaultSeverity>()
                .Select(v => new SelectListItem { Text = v.ToString(), Value = v.ToString() });
        }

        //=========================================================
        // REPORTS
        //=========================================================

        [HttpGet]
        public async Task<IActionResult> Reports()
        {
            var currentUserId = _userManager.GetUserId(User);

            // 1. Calculate key metrics for the technician
            var totalAssigned = await _context.FridgeFaults
                .CountAsync(ff => ff.AssignedTechnicianId == currentUserId);

            var totalRepaired = await _context.FridgeFaults
                .CountAsync(ff => ff.AssignedTechnicianId == currentUserId &&
                                   (ff.Status == FaultStatus.Repaired || ff.Status == FaultStatus.Resolved));

            var totalScheduled = await _context.MaintenanceSchedules
                .CountAsync(ms => ms.TechnicianId == currentUserId && ms.Status != "Completed");

            // 2. Fetch data for a chart (Fault activity over time)
            var faultsDates = await _context.FridgeFaults
                .Where(f => f.AssignedTechnicianId == currentUserId)
                .Select(f => f.DateReported)
                .ToListAsync();

            var groupedData = faultsDates
                .Where(d => d.Year == DateTime.Now.Year)
                .GroupBy(d => d.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    Count = g.Count()
                })
                .ToList();

            var activityOverTime = Enumerable.Range(1, 12)
                .Select(m => new ChartDataPoint
                {
                    Label = new DateTime(DateTime.Now.Year, m, 1).ToString("MMM"),
                    Value = groupedData
                        .Where(g => g.Month == m)
                        .Select(g => g.Count)
                        .FirstOrDefault()
                })
                .ToList();

            // 3. Build the ViewModel
            var viewModel = new MaintenanceReportsViewModel
            {
                TotalAssignedFaults = totalAssigned,
                TotalRepairedFaults = totalRepaired,
                TotalScheduledMaintenance = totalScheduled,
                ActivityOverTime = activityOverTime
            };

            ViewData["Title"] = "Performance Reports";
            return View(viewModel);
        }


        // ACTION STUB FOR DOWNLOADABLE REPORT (Resolves routing error)
        [HttpGet]
        public IActionResult ExportReportsToPdf()
        {
            var content = "Maintenance Performance Report generated on " + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + ". Note: This is a text placeholder, not a functional PDF.";
            // Keep the .pdf extension but change the MIME type to text/plain
            var fileName = $"Maintenance_Report_{DateTime.Now:yyyyMMdd}.pdf";
            var mimeType = "text/plain";

            // ✅ FIX: Use the correct MIME type when returning the file.
            return File(System.Text.Encoding.UTF8.GetBytes(content), mimeType, fileName);
        }
    }
}