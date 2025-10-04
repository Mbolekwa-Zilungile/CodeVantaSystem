using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Code_Vanta_Systems.Models;


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
        public DbSet<Fridge> Fridges { get; set; } // Inventory of fridges
        public DbSet<FridgeFault> FridgeFaults { get; set; } // Records faults reported on fridges
        public DbSet<FridgeMaintenance> FridgeMaintenances { get; set; } // Maintenance activities on fridges
        public DbSet<MaintenanceSchedule> MaintenanceSchedules { get; set; } // Planned maintenance schedules
        public DbSet<FridgeServiceHistory> FridgeServiceHistories { get; set; } // Historical service records
        public DbSet<Supplier> Suppliers { get; set; } // Suppliers providing fridges or parts
        public DbSet<PurchaseRequest> PurchaseRequests { get; set; } // Requests for purchasing items
        public DbSet<Quotation> Quotations { get; set; } // Supplier quotations (already configured)
        public DbSet<DeliveryNote> DeliveryNotes { get; set; } // Delivery confirmations
        public DbSet<ActivityLog> ActivityLogs { get; set; } // Logs of system or user activities
        public DbSet<Notification> Notifications { get; set; } // System or user notifications
        public DbSet<Document> Documents { get; set; } // Uploaded documents (already configured)
        public DbSet<FridgeUsageLog> FridgeUsageLogs { get; set; } // Usage tracking for fridges
        public DbSet<FridgeRequest> FridgeRequests { get; set; } // Requests for fridge allocations
        public DbSet<InventoryItem> InventoryItems { get; set; } // Items in inventory
        public DbSet<RequestQuotation> RequestQuotations { get; set; } // Requests sent to suppliers for quotations
        public DbSet<PurchaseOrder> PurchaseOrders { get; set; } // Orders placed with suppliers
        public DbSet<PurchaseOrderItem> PurchaseOrderItems { get; set; } // Items within a purchase order
        public DbSet<AuditTrail> AuditTrails { get; set; } // Tracks changes for auditing
        public DbSet<WorkFlowApproval> WorkflowApprovals { get; set; } // Workflow-based approval records
        public DbSet<Employee> Employees { get; set; }
        public DbSet<ItemCatalog> ItemCatalogs { get; set; }
        public DbSet<PurchaseRequestItem> PurchaseRequestItems { get; set; }
       




        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply all separate configuration classes
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

            // Optional: Explicit relationships

            // PurchaseRequest -> PurchaseRequestItem
            modelBuilder.Entity<PurchaseRequestItem>()
              .HasOne(p => p.ItemCatalog)
              .WithMany()
              .HasForeignKey(p => p.ItemCatalogId)
              .OnDelete(DeleteBehavior.Restrict);
            // PurchaseRequestItem -> ItemCatalog
            modelBuilder.Entity<PurchaseRequestItem>()
                .HasOne<ItemCatalog>() // No navigation in your current model, can add if needed
                .WithMany()
                .HasForeignKey("ItemCatalogId") // Shadow property if not in model
                .OnDelete(DeleteBehavior.Restrict);

            // PurchaseRequest -> RequestedBy (ApplicationUser)
            modelBuilder.Entity<PurchaseRequest>()
                .HasOne(r => r.RequestedBy)
                .WithMany() // Or navigation property in ApplicationUser if exists
                .HasForeignKey(r => r.RequestedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
