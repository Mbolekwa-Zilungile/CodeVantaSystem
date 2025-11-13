using Code_Vanta_Systems.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;


namespace Code_Vanta_Systems.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSets represent tables in the database
        public DbSet<Customer> Customers { get; set; } // Stores customer details
        public DbSet<Location> Locations { get; set; } // Physical locations for fridges or customers
        public DbSet<FridgeAllocation> FridgeAllocations { get; set; } // Tracks fridge assignments to customers

        // Replaces DbSet<Fridge> Fridges:
        public DbSet<FridgeModel> FridgeModels { get; set; } // Fridge Model definition (Catalogue)
        public DbSet<FridgeUnit> FridgeUnits { get; set; } // Individual Physical Fridge Units

        public DbSet<FridgeFault> FridgeFaults { get; set; } // Records faults reported on fridges
        public DbSet<FridgeMaintenance> FridgeMaintenances { get; set; } // Maintenance activities on fridges
        public DbSet<MaintenanceSchedule> MaintenanceSchedules { get; set; } // Planned maintenance schedules
        public DbSet<Supplier> Suppliers { get; set; } // Suppliers providing fridges or parts

        // Purchasing Subsystem Entities
        public DbSet<PurchaseRequest> PurchaseRequests { get; set; } // Requests for purchasing items
        public DbSet<PurchaseRequestItem> PurchaseRequestItems { get; set; }
        public DbSet<RequestQuotation> RequestQuotations { get; set; } // Request for Quotation header
        public DbSet<Quotation> Quotations { get; set; } // Supplier quotations header
        public DbSet<QuotationItem> QuotationItems { get; set; }
        public DbSet<PurchaseOrder> PurchaseOrders { get; set; } // Finalized orders sent to suppliers
        public DbSet<PurchaseOrderItem> PurchaseOrderItems { get; set; } // Line items for a Purchase Order
        public DbSet<DeliveryNote> DeliveryNotes { get; set; } // Records goods received from suppliers
        public DbSet<ItemCatalog> ItemCatalogs { get; set; } // Catalog for parts and non-fridge items
        public DbSet<InventoryItem> InventoryItems { get; set; } // Tracks quantity of parts/items in stock

        // Audit, Logs, and Workflow Entities
        public DbSet<AuditTrail> AuditTrails { get; set; } // Tracks changes for auditing
        public DbSet<ActivityLog> ActivityLogs { get; set; } // Records user actions/activities
        public DbSet<WorkFlowApproval> WorkflowApprovals { get; set; } // Workflow-based approval records
        public DbSet<Notification> Notifications { get; set; } // User notification system
        public DbSet<Document> Documents { get; set; } // Generic document storage/linking

        // Rental/Customer Entities
        public DbSet<FridgeRequest> FridgeRequests { get; set; } // Customer requests for fridges
        public DbSet<RentalOrder> RentalOrders { get; set; }
        public DbSet<RentalOrderItem> RentalOrderItems { get; set; }
        public DbSet<RentalRequest> RentalRequests { get; set; }
        public DbSet<RentedFridge> RentedFridges { get; set; }
        public DbSet<Company> Company { get; set; }

        // Technician/Sensor Entities
        public DbSet<ServiceRecord> ServiceRecords { get; set; }
        public DbSet<TechnicianSchedule> TechnicianSchedules { get; set; } // Technician visit schedules
        public DbSet<SensorReading> SensorReadings { get; set; } // Telemetry data from fridges
        public DbSet<FridgeUsageLog> FridgeUsageLogs { get; set; } // Records when a fridge is accessed

        // User/Identity Entities
        public DbSet<Employee> Employees { get; set; }
        public DbSet<ApplicationUser> Users { get; set; }
        public DbSet<IdentityUserRole<string>> UserRoles { get; set; } // If using default Identity
        public DbSet<IdentityRole> Roles { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Call base method first for Identity configurations
            base.OnModelCreating(modelBuilder);

            // Apply all separate configuration classes
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        }
    }
}