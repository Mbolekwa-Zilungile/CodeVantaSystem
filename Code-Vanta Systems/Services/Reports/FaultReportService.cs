using Code_Vanta_Systems.Data;
using Code_Vanta_Systems.ViewModels.Reports;
using Code_Vanta_Systems.Services.Reports;
using Code_Vanta_Systems.Models;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Code_Vanta_Systems.Models.Enums;


public class FaultReportService : IFaultReportService
{
    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _env;

    public FaultReportService(ApplicationDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public async Task<MonthlyReportViewModel> GetMonthlyReportAsync(
        string technicianId, 
        int month, 
        int year, 
        string statusFilter = null, 
        bool includeUnassigned = false)
    {
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1);

        var query = _db.FridgeFaults
            .AsNoTracking()
            .Include(f => f.Fridge)
            .Where(f => f.DateReported >= start && f.DateReported < end);

        if (!string.IsNullOrEmpty(technicianId))
        {
            query = includeUnassigned
                ? query.Where(f => f.AssignedTechnicianId == technicianId || f.AssignedTechnicianId == null)
                : query.Where(f => f.AssignedTechnicianId == technicianId);
        }

        if (!string.IsNullOrEmpty(statusFilter) && Enum.TryParse<FaultStatus>(statusFilter, out var parsedStatus))
        {
            query = query.Where(f => f.Status == parsedStatus);
        }

        var grouped = await query
            .GroupBy(f => new { Serial = f.Fridge.SerialNumber, Status = f.Status })
            .Select(g => new MonthlyFaultDto
            {
                FridgeSerial = g.Key.Serial,
                Status = g.Key.Status.ToString(),
                Count = g.Count()
            })
            .OrderByDescending(r => r.Count)
            .ToListAsync();

        var vm = new MonthlyReportViewModel
        {
            Month = month,
            Year = year,
            StatusFilter = statusFilter,
            Rows = grouped
        };

        var chart = grouped
            .GroupBy(r => r.FridgeSerial)
            .Select(g => new { Serial = g.Key, Total = g.Sum(x => x.Count) })
            .OrderByDescending(x => x.Total)
            .ToList();

        vm.ChartLabels = chart.Select(x => x.Serial).ToList();
        vm.ChartValues = chart.Select(x => x.Total).ToList();

        return vm;
    }

    public async Task<byte[]> GenerateMonthlyReportPdfAsync(
        string technicianId, 
        int month, 
        int year, 
        string statusFilter = null, 
        bool includeUnassigned = false)
    {
        var vm = await GetMonthlyReportAsync(technicianId, month, year, statusFilter, includeUnassigned);

        var document = QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(20);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header()
                    .PaddingBottom(5)
                    .Row(row =>
                    {
                        row.RelativeColumn().Stack(stack =>
                        {
                            stack.Item().Text($"Monthly Fault Report").SemiBold().FontSize(16);
                            stack.Item().Text($"Technician ID: {technicianId}").FontSize(10);
                            stack.Item().Text($"Period: {month:D2}/{year}").FontSize(10);
                        });

                        row.ConstantColumn(120).AlignRight().Stack(stack =>
                        {
                            stack.Item().Text($"{DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm} UTC").FontSize(10);
                        });
                    });

                page.Content()
                    .PaddingVertical(10)
                    .Column(col =>
                    {
                        col.Spacing(8);
                        col.Item().Text($"Summary by Fridge and Status").Bold();

                        // Table
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("Fridge Serial").Bold();
                                header.Cell().Element(CellStyle).Text("Status").Bold();
                                header.Cell().Element(CellStyle).AlignRight().Text("Count").Bold();
                            });

                            foreach (var row in vm.Rows)
                            {
                                table.Cell().Element(CellStyle).Text(row.FridgeSerial ?? "-");
                                table.Cell().Element(CellStyle).Text(row.Status ?? "-");
                                table.Cell().Element(CellStyle).AlignRight().Text(row.Count.ToString());
                            }

                            static IContainer CellStyle(IContainer container)
                            {
                                return container.Padding(4).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
                            }
                        });

                        // Totals
                        col.Item().PaddingTop(8).Row(r =>
                        {
                            var total = vm.Rows.Sum(x => x.Count);
                            r.ConstantColumn(250).Text($"Total faults: {total}").Bold();
                            r.RelativeColumn().AlignRight().Text(""); // spacer
                        });
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(x => x.Span("Fridge Management System • Generated by system").FontSize(9));
            });
        });

        return document.GeneratePdf();
    }
}
