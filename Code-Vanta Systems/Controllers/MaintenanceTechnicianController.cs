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
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
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

        public async Task<IActionResult> RescheduleSchedule(int id)
        {
            var currentUserId = _userManager.GetUserId(User);
            var schedule = await _context.MaintenanceSchedules
                // Fetch the schedule and ensure it's assigned to the current technician and not completed/cancelled
                .FirstOrDefaultAsync(ms => ms.Id == id && ms.TechnicianId == currentUserId && ms.Status == "Scheduled");

            if (schedule == null)
            {
                TempData["ErrorMessage"] = "Schedule not found or not eligible for rescheduling.";
                return RedirectToAction(nameof(MaintenanceSchedules));
            }

            // Map the existing schedule data to the CreateScheduleViewModel
            var viewModel = new CreateScheduleViewModel
            {
                ScheduleId = schedule.Id, // NEW: Used to identify the schedule being updated
                FridgeUnitId = schedule.FridgeUnitId,
                ScheduledDate = schedule.ScheduledDate.Date.AddHours(9), // Default to 9 AM, ready for user modification
                Description = schedule.Description,
                IsReschedule = true // Flag to indicate this is an update, not a new schedule
            };

            await PopulateCreateScheduleViewModel(viewModel);

            // Set the selected unit ID so the dropdown is pre-selected
            if (viewModel.AvailableFridges.Any(f => f.Value == schedule.FridgeUnitId.ToString()))
            {
                viewModel.AvailableFridges.First(f => f.Value == schedule.FridgeUnitId.ToString()).Selected = true;
            }

            ViewData["Title"] = $"Reschedule Maintenance Visit #{schedule.Id}";
            return View("CreateSchedule", viewModel); // Reuse the CreateSchedule view
        }

        // POST Method for Rescheduling an existing Schedule
        // POST: MaintenanceTechnician/RescheduleSchedule
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RescheduleSchedule(CreateScheduleViewModel viewModel)
        {
            // 1. Mandatory check for ScheduleId and FridgeUnitId
            if (!viewModel.ScheduleId.HasValue || !viewModel.FridgeUnitId.HasValue)
            {
                ModelState.AddModelError("", "Missing schedule or unit information.");
            }

            if (ModelState.IsValid)
            {
                var currentUserId = _userManager.GetUserId(User);
                var schedule = await _context.MaintenanceSchedules
                    .FirstOrDefaultAsync(ms => ms.Id == viewModel.ScheduleId.Value && ms.TechnicianId == currentUserId);

                if (schedule == null)
                {
                    TempData["ErrorMessage"] = "Schedule not found or you are not authorized to update it.";
                    return RedirectToAction(nameof(MaintenanceSchedules));
                }

                // 2. Update the schedule details
                schedule.FridgeUnitId = viewModel.FridgeUnitId.Value;
                schedule.ScheduledDate = viewModel.ScheduledDate;
                schedule.Description = viewModel.Description;
                schedule.Status = "Scheduled"; // Reset status just in case

                _context.MaintenanceSchedules.Update(schedule);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Maintenance schedule #{schedule.Id} successfully **rescheduled** for {viewModel.ScheduledDate:dd MMM yyyy @ HH:mm}.";

                return RedirectToAction(nameof(MaintenanceSchedules)); // Redirect back to the schedule list
            }

            // 3. Re-populate context and dropdowns on failure
            await PopulateCreateScheduleViewModel(viewModel);
            viewModel.IsReschedule = true; // Maintain the context flag

            return View("CreateSchedule", viewModel);
        }

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

        private byte[] CreatePerformanceReportPdf(MaintenanceReportsViewModel model, List<ChartDataPoint> repairedData)
        {
            // Ensure QuestPDF registration is done once (optional, but safer)
            QuestPDF.Settings.License = LicenseType.Community;

            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(20);

                    // ------------------ HEADER ------------------
                    page.Header().Row(row =>
                    {
                        row.ConstantColumn(150).Text("CODE VANTA SYSTEMS").Bold().FontSize(12).FontColor(Colors.BlueGrey.Darken2);
                        row.RelativeColumn().Text("MAINTENANCE TECHNICIAN PERFORMANCE REPORT")
                            .Bold().FontSize(18).FontColor(Colors.Red.Medium).AlignRight();
                    });

                    // ------------------ CONTENT ------------------
                    page.Content().Column(col =>
                    {
                        col.Spacing(20);

                        // --- 1. Technician Details ---
                        // FIX: Use Container() before PaddingBottom
                        col.Item().Container().PaddingBottom(5).Text($"Report for: {_userManager.GetUserAsync(User).Result?.FullName ?? "N/A"}")
                            .FontSize(14).SemiBold();

                        // FIX: Use Container() before PaddingBottom
                        col.Item().Container().PaddingBottom(5).Text($"Date Generated: {DateTime.Now:dd MMMM yyyy HH:mm}")
                            .FontSize(10).FontColor(Colors.Grey.Medium);

                        col.Item().LineHorizontal(1);

                        // --- 2. Key Performance Indicators (KPIs) ---
                        col.Item().Container().PaddingBottom(5).Text("Key Metrics Summary")
    .Bold().FontSize(14);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            // Row 1
                            table.Cell().Background(Colors.BlueGrey.Lighten5).Padding(5).Text("Total Faults Assigned").SemiBold();
                            table.Cell().Background(Colors.BlueGrey.Lighten5).Padding(5).Text(model.TotalAssignedFaults.ToString()).AlignRight();

                            // Row 2
                            table.Cell().Padding(5).Text("Total Faults Repaired/Resolved").SemiBold();
                            table.Cell().Padding(5).Text(model.TotalRepairedFaults.ToString()).AlignRight();

                            // Row 3
                            table.Cell().Background(Colors.BlueGrey.Lighten5).Padding(5).Text("Total Allocated Units (Oversight)").SemiBold();
                            table.Cell().Background(Colors.BlueGrey.Lighten5).Padding(5).Text(model.TotalAllocatedUnits.ToString()).AlignRight();

                            // Row 4
                            table.Cell().Padding(5).Text("Total Service Records Created").SemiBold();
                            table.Cell().Padding(5).Text(model.TotalServiceRecords.ToString()).AlignRight();

                        });

                        // --- 3. Workload Trends (Tabular Data) ---
                        // FIX: Use Container() before PaddingTop/PaddingBottom
                        col.Item().Container().PaddingTop(15).PaddingBottom(5).Text("Monthly Workload Trends (Current Year)")
                            .Bold().FontSize(14);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(80);
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Month").Bold();
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Faults Assigned").Bold().AlignRight();
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Faults Repaired").Bold().AlignRight();
                            });

                            for (int i = 0; i < model.ActivityOverTime.Count; i++)
                            {
                                var assigned = model.ActivityOverTime[i];
                                var repaired = repairedData.FirstOrDefault(d => d.Label == assigned.Label) ?? new ChartDataPoint { Value = 0 };

                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(assigned.Label);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(assigned.Value.ToString()).AlignRight();
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(repaired.Value.ToString()).AlignRight();
                            }
                        });
                    });

                    // ------------------ FOOTER ------------------
                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Page ").FontSize(10);
                        text.CurrentPageNumber().FontSize(10);
                        text.Span(" of ").FontSize(10);
                        text.TotalPages().FontSize(10);
                    });
                });
            });

            return document.GeneratePdf();
        }


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

            // NEW METRIC: Total Service Records created by this technician
            var totalServiceRecords = await _context.ServiceRecords
                .CountAsync(sr => sr.TechnicianUserId == currentUserId);

            var totalScheduled = await _context.MaintenanceSchedules
                .CountAsync(ms => ms.TechnicianId == currentUserId && ms.Status != "Completed");

            // NEW METRIC: Total Allocated Units 
            var totalAllocatedUnits = await _context.FridgeUnits
                .CountAsync(u => u.TechnicianUserId == currentUserId && u.IsRented);


            // 2. Fetch data for the charts (Fault activity over time)

            // Series 1: Faults Reported/Assigned (Incoming Workload)
            var assignedFaultsDates = await _context.FridgeFaults
                .Where(f => f.AssignedTechnicianId == currentUserId)
                .Select(f => f.DateReported)
                .ToListAsync();

            var assignedGroupedData = assignedFaultsDates
                .Where(d => d.Year == DateTime.Now.Year)
                .GroupBy(d => d.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    Count = g.Count()
                })
                .ToList();

            // Series 2: Faults Repaired/Resolved (Outgoing Workload)
            var repairedFaultsDates = await _context.FridgeFaults
                .Where(f => f.AssignedTechnicianId == currentUserId && (f.Status == FaultStatus.Repaired || f.Status == FaultStatus.Resolved))
                .Select(f => f.ResolutionDate.HasValue ? f.ResolutionDate.Value : DateTime.MinValue) // Use ResolutionDate
                .ToListAsync();

            var repairedGroupedData = repairedFaultsDates
                .Where(d => d.Year == DateTime.Now.Year)
                .GroupBy(d => d.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    Count = g.Count()
                })
                .ToList();


            // 3. Build the ViewModel
            var activityOverTimeAssigned = Enumerable.Range(1, 12)
                .Select(m => new ChartDataPoint
                {
                    Label = new DateTime(DateTime.Now.Year, m, 1).ToString("MMM"),
                    Value = assignedGroupedData.FirstOrDefault(g => g.Month == m)?.Count ?? 0
                })
                .ToList();

            var activityOverTimeRepaired = Enumerable.Range(1, 12)
                .Select(m => new ChartDataPoint
                {
                    Label = new DateTime(DateTime.Now.Year, m, 1).ToString("MMM"),
                    Value = repairedGroupedData.FirstOrDefault(g => g.Month == m)?.Count ?? 0
                })
                .ToList();

            var viewModel = new MaintenanceReportsViewModel
            {
                TotalAssignedFaults = totalAssigned,
                TotalRepairedFaults = totalRepaired,
                TotalScheduledMaintenance = totalScheduled,
                TotalAllocatedUnits = totalAllocatedUnits,
                TotalServiceRecords = totalServiceRecords,
                // Pass Series 1 as the main list
                ActivityOverTime = activityOverTimeAssigned,
                // Pass Series 2 as a separate property (assuming ViewModel update)
                ActivityOverTimeRepaired = activityOverTimeRepaired
            };

            // Pass Series 2 data via ViewBag for the chart (fallback/guarantee)
            ViewBag.ActivityOverTimeRepaired = activityOverTimeRepaired;


            ViewData["Title"] = "Performance Reports";
            return View(viewModel);
        }


        // ACTION STUB FOR DOWNLOADABLE REPORT (MIME type corrected)
        [HttpGet]
        public async Task<IActionResult> ExportReportsToPdf()
        {
            var currentUserId = _userManager.GetUserId(User);

            // Fetch all report data exactly as done in the Reports() method
            var totalAssigned = await _context.FridgeFaults.CountAsync(ff => ff.AssignedTechnicianId == currentUserId);
            var totalRepaired = await _context.FridgeFaults.CountAsync(ff => ff.AssignedTechnicianId == currentUserId && (ff.Status == FaultStatus.Repaired || ff.Status == FaultStatus.Resolved));
            var totalServiceRecords = await _context.ServiceRecords.CountAsync(sr => sr.TechnicianUserId == currentUserId);
            var totalScheduled = await _context.MaintenanceSchedules.CountAsync(ms => ms.TechnicianId == currentUserId && ms.Status != "Completed");
            var totalAllocatedUnits = await _context.FridgeUnits.CountAsync(u => u.TechnicianUserId == currentUserId && u.IsRented);

            // --- Generate Chart Data Series 1 (Assigned) ---
            var assignedFaultsDates = await _context.FridgeFaults.Where(f => f.AssignedTechnicianId == currentUserId).Select(f => f.DateReported).ToListAsync();
            var assignedGroupedData = assignedFaultsDates.Where(d => d.Year == DateTime.Now.Year).GroupBy(d => d.Month).Select(g => new { Key = g.Key, Count = g.Count() }).ToList();

            // FIX CS1061: Use 'Key' for lookup
            var activityOverTimeAssigned = Enumerable.Range(1, 12).Select(m => new ChartDataPoint { Label = new DateTime(DateTime.Now.Year, m, 1).ToString("MMM"), Value = assignedGroupedData.FirstOrDefault(g => g.Key == m)?.Count ?? 0 }).ToList();

            // --- Generate Chart Data Series 2 (Repaired) ---
            var repairedFaultsDates = await _context.FridgeFaults.Where(f => f.AssignedTechnicianId == currentUserId && (f.Status == FaultStatus.Repaired || f.Status == FaultStatus.Resolved)).Select(f => f.ResolutionDate.HasValue ? f.ResolutionDate.Value : DateTime.MinValue).ToListAsync();
            var repairedGroupedData = repairedFaultsDates.Where(d => d.Year == DateTime.Now.Year).GroupBy(d => d.Month).Select(g => new { Key = g.Key, Count = g.Count() }).ToList();

            // FIX CS1061: Use 'Key' for lookup
            var activityOverTimeRepaired = Enumerable.Range(1, 12).Select(m => new ChartDataPoint { Label = new DateTime(DateTime.Now.Year, m, 1).ToString("MMM"), Value = repairedGroupedData.FirstOrDefault(g => g.Key == m)?.Count ?? 0 }).ToList();


            var viewModel = new MaintenanceReportsViewModel
            {
                TotalAssignedFaults = totalAssigned,
                TotalRepairedFaults = totalRepaired,
                TotalScheduledMaintenance = totalScheduled,
                TotalAllocatedUnits = totalAllocatedUnits,
                TotalServiceRecords = totalServiceRecords,
                ActivityOverTime = activityOverTimeAssigned,
                ActivityOverTimeRepaired = activityOverTimeRepaired
            };

            // 3. Generate the PDF
            var pdfBytes = CreatePerformanceReportPdf(viewModel, activityOverTimeRepaired);

            var fileName = $"Performance_Report_{_userManager.GetUserName(User)}_{DateTime.Now:yyyyMMdd}.pdf";

            // 4. Return the file
            return File(pdfBytes, "application/pdf", fileName);
        }

    }
}
