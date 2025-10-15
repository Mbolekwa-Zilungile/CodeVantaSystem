using Code_Vanta_Systems.ViewModels.Reports;

namespace Code_Vanta_Systems.Services.Reports
{
    public interface IFaultReportService
    {
        Task<MonthlyReportViewModel> GetMonthlyReportAsync(string technicianId, int month, int year, string statusFilter = null, bool includeUnassigned = false);
        Task<byte[]> GenerateMonthlyReportPdfAsync(string technicianId, int month, int year, string statusFilter = null, bool includeUnassigned = false);
    }

}
