using Code_Vanta_Systems.Models;

namespace Code_Vanta_Systems.ViewModels
{
    public class AdminDashboardViewModel
    {
            public List<Employee> ActiveEmployees { get; set; }
            public List<Employee> InactiveEmployees { get; set; }
            public List<Employee> Customers { get; set; }

            public int ActiveCount => ActiveEmployees?.Count ?? 0;
            public int InactiveCount => InactiveEmployees?.Count ?? 0;
            public int CustomerCount => Customers?.Count ?? 0;
        

    }
}
