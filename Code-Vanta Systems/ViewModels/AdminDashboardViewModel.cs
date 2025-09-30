using Code_Vanta_Systems.Models;

namespace Code_Vanta_Systems.ViewModels
{
    public class AdminDashboardViewModel
    {
            public List<Employee> ActiveEmployees { get; set; }
            public List<Employee> InactiveEmployees { get; set; }
            public List<Employee> Customers { get; set; }

            public int TotalCustomers { get; set; }        // Total count (Active + Inactive)
            public int ActiveCustomersCount { get; set; }
            public int InactiveCustomersCount { get; set; }
            public int EmployeeCount { get; set; }
            public int TotalSystemUsers { get; set; }

            // Placeholder values
            public int OpenFaultsCount { get; set; }
            public int PendingMaintenanceCount { get; set; }
            public int TotalFridgesInInventory { get; set; }

            public int ActiveCount => ActiveEmployees?.Count ?? 0;
            public int InactiveCount => InactiveEmployees?.Count ?? 0;
            public int CustomerCount => Customers?.Count ?? 0;
        

    }
}
