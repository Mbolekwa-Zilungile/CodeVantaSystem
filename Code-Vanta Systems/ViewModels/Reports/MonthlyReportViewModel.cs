namespace Code_Vanta_Systems.ViewModels.Reports
{
    public class MonthlyReportViewModel
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public string StatusFilter { get; set; }         
        public bool ShowUnassigned { get; set; }         
        public List<MonthlyFaultDto> Rows { get; set; } = new();
        public List<string> ChartLabels { get; set; } = new();
        public List<int> ChartValues { get; set; } = new();
    }
}
