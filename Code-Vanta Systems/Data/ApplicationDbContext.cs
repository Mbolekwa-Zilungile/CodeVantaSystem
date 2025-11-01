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
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<FridgeAllocation> FridgeAllocations { get; set; }

        // REPLACE: Fridges with FridgeModels and FridgeUnits
        public DbSet<FridgeModel> FridgeModels { get; set; }
        public DbSet<FridgeUnit> FridgeUnits { get; set; }

        public DbSet<FridgeFault> FridgeFaults { get; set; }
        public DbSet<FridgeMaintenance> FridgeMaintenances { get; set; }
        public DbSet<MaintenanceSchedule> MaintenanceSchedules { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<PurchaseRequest> PurchaseRequests { get; set; }
        public DbSet<Quotation> Quotations { get; set; }
        public DbSet<DeliveryNote> DeliveryNotes { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<FridgeUsageLog> FridgeUsageLogs { get; set; }
        public DbSet<FridgeRequest> FridgeRequests { get; set; }
        public DbSet<InventoryItem> InventoryItems { get; set; }
        public DbSet<RequestQuotation> RequestQuotations { get; set; }
        public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
        public DbSet<PurchaseOrderItem> PurchaseOrderItems { get; set; }
        public DbSet<AuditTrail> AuditTrails { get; set; }
        public DbSet<WorkFlowApproval> WorkflowApprovals { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<ItemCatalog> ItemCatalogs { get; set; }
        public DbSet<PurchaseRequestItem> PurchaseRequestItems { get; set; }
        public DbSet<RentalOrder> RentalOrders { get; set; }
        public DbSet<RentalRequest> RentalRequests { get; set; }
        public DbSet<RentedFridge> RentedFridges { get; set; }
        public DbSet<Company> Company { get; set; }
        public DbSet<ServiceRecord> ServiceRecords { get; set; }
        public DbSet<ApplicationUser> Users { get; set; }
        public DbSet<IdentityUserRole<string>> UserRoles { get; set; }
        public DbSet<IdentityRole> Roles { get; set; }
        public DbSet<QuotationItem> QuotationItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        }
    }
}