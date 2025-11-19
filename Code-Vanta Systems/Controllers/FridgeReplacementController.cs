using Code_Vanta_Systems.Data;
using Code_Vanta_Systems.Models;
using Code_Vanta_Systems.Models.Enums;
using Code_Vanta_Systems.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Code_Vanta_Systems.Controllers
{
    //[Authorize(Roles = "Admin")]
    public class FridgeReplacementController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FridgeReplacementController(ApplicationDbContext context)
        {
            _context = context;
        }

       
        public async Task<IActionResult> Index(string search, RequestStatus? status, int pageIndex = 1)
        {
            const int pageSize = 10;

            
            var query = _context.FridgeRequests
                .Include(r => r.Customer)
                .Include(r => r.Location)
                .Include(r => r.RequestedBy)
                .Where(r => r.Notes.Contains("REPLACEMENT NEEDED") || r.Notes.Contains("scrapped unit"))
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(r => r.Status == status.Value);
            }

            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                query = query.Where(r =>
                    r.Customer.BusinessName.ToLower().Contains(search) ||
                    r.Notes.ToLower().Contains(search) ||
                    r.RequestedBy.FullName.ToLower().Contains(search)
                );
            }

            var totalCount = await query.CountAsync();

           
            var requests = await query
                .OrderByDescending(r => r.RequestedAt)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            
            var viewModels = new List<ReplacementRequestViewModel>();

            foreach (var request in requests)
            {
               
                var scrappedSerial = ExtractSerialFromNotes(request.Notes);

                
                var faultInfo = await GetFaultInfoFromRequest(request);

                viewModels.Add(new ReplacementRequestViewModel
                {
                    RequestId = request.Id,
                    FaultId = faultInfo.FaultId,
                    ScrappedUnitSerial = scrappedSerial,
                    CustomerName = request.Customer.BusinessName,
                    LocationName = request.Location.Address,
                    RequestedBy = request.RequestedBy.FullName,
                    RequestedAt = request.RequestedAt,
                    Notes = request.Notes,
                    Status = request.Status,
                    ScrappedFridgeModel = faultInfo.ScrappedFridgeModel,
                    ScrappedDate = faultInfo.ScrappedDate
                });
            }

            
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            
            ViewData["currentPage"] = pageIndex;
            ViewData["totalPages"] = totalPages;
            ViewData["search"] = search;
            ViewData["selectedStatus"] = status;

            return View(viewModels);
        }

       
        public async Task<IActionResult> Details(int id)
        {
            var request = await _context.FridgeRequests
                .Include(r => r.Customer)
                .Include(r => r.Location)
                .Include(r => r.RequestedBy)
                .FirstOrDefaultAsync(r => r.Id == id && (r.Notes.Contains("REPLACEMENT NEEDED") || r.Notes.Contains("scrapped unit")));

            if (request == null)
            {
                TempData["ErrorMessage"] = "Replacement request not found.";
                return RedirectToAction(nameof(Index));
            }

            
            var scrappedSerial = ExtractSerialFromNotes(request.Notes);

           
            var faultInfo = await GetFaultInfoFromRequest(request);

            var viewModel = new ReplacementRequestViewModel
            {
                RequestId = request.Id,
                FaultId = faultInfo.FaultId,
                ScrappedUnitSerial = scrappedSerial,
                CustomerName = request.Customer.BusinessName,
                LocationName = request.Location.Address,
                RequestedBy = request.RequestedBy.FullName,
                RequestedAt = request.RequestedAt,
                Notes = request.Notes,
                Status = request.Status,
                ScrappedFridgeModel = faultInfo.ScrappedFridgeModel,
                ScrappedDate = faultInfo.ScrappedDate
            };

            
            ViewBag.AvailableFridges = await _context.Fridges
                .Where(f => f.Status == FridgeStatus.Available) 
                .Select(f => new { f.Id, f.Model, f.Supplier, f.SerialNumber, f.RentalPricePerMonth })
                .ToListAsync();

            return View(viewModel);
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, RequestStatus newStatus, string adminNotes = "")
        {
            var request = await _context.FridgeRequests
                .Include(r => r.Customer)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
            {
                TempData["ErrorMessage"] = "Replacement request not found.";
                return RedirectToAction(nameof(Index));
            }

            var oldStatus = request.Status;
            request.Status = newStatus;

           
            if (!string.IsNullOrEmpty(adminNotes))
            {
                request.Notes += $"\n\nAdmin Update ({DateTime.Now:yyyy-MM-dd HH:mm}): {adminNotes}";
            }

          
            var audit = new AuditTrail
            {
                EntityName = "FridgeRequest",
                EntityId = request.Id,
                ChangedByUserId = User.Identity.Name,
                ChangeType = "Status Update",
                ChangeDate = DateTime.Now,
                ChangeDetails = $"Replacement request status changed from {oldStatus} to {newStatus}. {adminNotes}"
            };

            try
            {
                _context.Update(request);
                _context.AuditTrails.Add(audit);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Replacement request #{id} status updated to {newStatus}.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error updating status: {ex.Message}";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

      
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignReplacement(int requestId, int fridgeId, string newSerialNumber, string adminNotes = "")
        {
            var request = await _context.FridgeRequests
                .Include(r => r.Customer)
                .Include(r => r.Location)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null)
            {
                TempData["ErrorMessage"] = "Replacement request not found.";
                return RedirectToAction(nameof(Index));
            }

            var fridge = await _context.Fridges.FindAsync(fridgeId);
            if (fridge == null)
            {
                TempData["ErrorMessage"] = "Selected fridge model not found.";
                return RedirectToAction(nameof(Details), new { id = requestId });
            }

            try
            {
                var newFridgeUnit = new FridgeUnit
                {
                    SerialNumber = newSerialNumber,
                    FridgeId = fridgeId,
                    CustomerId = request.CustomerId,
                   
                    Condition = FridgeCondition.Operational,
                    Status = FridgeStatus.Operational,
                   
                };

                _context.FridgeUnits.Add(newFridgeUnit);

               
                request.Status = RequestStatus.Completed;
                request.Notes += $"\n\nREPLACEMENT ASSIGNED ({DateTime.Now:yyyy-MM-dd HH:mm}): " +
                               $"New unit {newSerialNumber} ({fridge.Model}) assigned. {adminNotes}";

               
                var requestAudit = new AuditTrail
                {
                    EntityName = "FridgeRequest",
                    EntityId = request.Id,
                    ChangedByUserId = User.Identity.Name,
                    ChangeType = "Replacement Assigned",
                    ChangeDate = DateTime.Now,
                    ChangeDetails = $"New fridge unit {newSerialNumber} assigned as replacement. Request completed."
                };

                var unitAudit = new AuditTrail
                {
                    EntityName = "FridgeUnit",
                    EntityId = newFridgeUnit.Id,
                    ChangedByUserId = User.Identity.Name,
                    ChangeType = "Replacement Unit Created",
                    ChangeDate = DateTime.Now,
                    ChangeDetails = $"Created as replacement for scrapped unit. Customer: {request.Customer.BusinessName}"
                };

                _context.AuditTrails.Add(requestAudit);
                _context.AuditTrails.Add(unitAudit);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Replacement assigned successfully! New unit: {newSerialNumber}";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error assigning replacement: {ex.Message}";
            }

            return RedirectToAction(nameof(Details), new { id = requestId });
        }

        private string ExtractSerialFromNotes(string notes)
        {
            if (string.IsNullOrEmpty(notes)) return "Unknown";

            var patterns = new[] { "scrapped unit:", "unit:", "Serial:" };
            foreach (var pattern in patterns)
            {
                var index = notes.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
                if (index >= 0)
                {
                    var start = index + pattern.Length;
                    var end = notes.IndexOfAny(new[] { '.', ',', ' ', '\n' }, start);
                    if (end == -1) end = notes.Length;
                    return notes.Substring(start, end - start).Trim();
                }
            }
            return "Unknown";
        }

        private async Task<(int FaultId, string ScrappedFridgeModel, DateTime ScrappedDate)> GetFaultInfoFromRequest(FridgeRequest request)
        {
            var serial = ExtractSerialFromNotes(request.Notes);

            var fault = await _context.FridgeFaults
                .Include(f => f.FridgeUnit)
                .ThenInclude(u => u.Fridge)
                .FirstOrDefaultAsync(f => f.FridgeUnit.SerialNumber == serial && f.Status == FaultStatus.Scrapped);

            if (fault != null)
            {
                return (fault.Id, fault.FridgeUnit.Fridge?.Model ?? "Unknown", fault.ResolutionDate ?? fault.DateReported);
            }

            return (0, "Unknown", DateTime.MinValue);
        }
    }
}