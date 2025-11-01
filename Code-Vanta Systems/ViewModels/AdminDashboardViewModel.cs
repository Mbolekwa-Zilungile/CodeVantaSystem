using Code_Vanta_Systems.Models;
using System.Collections.Generic;

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

        // Add these missing properties for fridge statistics
        public int TotalFridgeUnits { get; set; }
        public int AvailableFridgeUnits { get; set; }
        public int AllocatedFridgeUnits { get; set; }
        public int InRepairFridgeUnits { get; set; }
        public int RetiredFridgeUnits { get; set; }

        // Supplier statistics
        public int ActiveSuppliersCount { get; set; }
        public int InactiveSuppliersCount { get; set; }
        public int TotalSuppliers { get; set; }

        // Rental statistics
        public int PendingRentalRequests { get; set; }
        public int ApprovedRentalRequests { get; set; }
        public int TotalRentalRequests { get; set; }

        // Fault statistics
        public int TotalFaults { get; set; }
        public int OpenFaults { get; set; }
        public int InProgressFaults { get; set; }
        public int ResolvedFaults { get; set; }

        public int ActiveCount => ActiveEmployees?.Count ?? 0;
        public int InactiveCount => InactiveEmployees?.Count ?? 0;
        public int CustomerCount => Customers?.Count ?? 0;

        // Additional calculated properties for better UI display
        public decimal FridgeUtilizationRate => TotalFridgeUnits > 0
            ? (decimal)AllocatedFridgeUnits / TotalFridgeUnits * 100
            : 0;

        public decimal FridgeAvailabilityRate => TotalFridgeUnits > 0
            ? (decimal)AvailableFridgeUnits / TotalFridgeUnits * 100
            : 0;

        public decimal FaultResolutionRate => TotalFaults > 0
            ? (decimal)ResolvedFaults / TotalFaults * 100
            : 0;
    }
}