using Code_Vanta_Systems.Models.Enums;
using Code_Vanta_Systems.Services.Reports;
using Code_Vanta_Systems.ViewModels.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;


[Authorize(Roles = "Fault Technician")]
public class ReportsController : Controller
{
    private readonly IFaultReportService _reportService;

    public ReportsController(IFaultReportService reportService)
    {
        _reportService = reportService;
    }

    // GET: /Reports/Monthly
    public async Task<IActionResult> Monthly(int? month, int? year, string statusFilter, bool includeUnassigned = false)
    {
        var now = DateTime.UtcNow;
        int m = month ?? now.Month;
        int y = year ?? now.Year;

        var technicianId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var vm = await _reportService.GetMonthlyReportAsync(technicianId, m, y, statusFilter, includeUnassigned);

        ViewBag.StatusOptions = Enum.GetValues(typeof(FaultStatus))
                                    .Cast<FaultStatus>()
                                    .Select(e => e.ToString())
                                    .ToList();

        return View(vm);
    }

    // GET: /Reports/DownloadPdf
    [HttpGet]
    public async Task<IActionResult> DownloadPdf(int month, int year, string statusFilter, bool includeUnassigned = false)
    {
        var technicianId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var pdfBytes = await _reportService.GenerateMonthlyReportPdfAsync(technicianId, month, year, statusFilter, includeUnassigned);

        var fileName = $"MonthlyFaultReport_{year}_{month:D2}.pdf";
        return File(pdfBytes, "application/pdf", fileName);
    }
}
