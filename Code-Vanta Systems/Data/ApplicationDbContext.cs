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

        // DbSets for all models
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<FridgeAllocation> FridgeAllocations { get; set; }
        public DbSet<Fridge> Fridges { get; set; }
        public DbSet<FridgeFault> FridgeFaults { get; set; }
        public DbSet<FridgeMaintenance> FridgeMaintenances { get; set; }
        public DbSet<MaintenanceSchedule> MaintenanceSchedules { get; set; }
        public DbSet<FridgeServiceHistory> FridgeServiceHistories { get; set; }
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




        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Decimal precision configuration
            modelBuilder.Entity<Quotation>()
                .Property(q => q.UnitPrice)
                .HasColumnType("decimal(18,2)");

            // Avoid multiple cascade paths for Fridge -> Customer -> Location
            modelBuilder.Entity<Fridge>()
                .HasOne(f => f.Location)
                .WithMany(l => l.Fridges)
                .HasForeignKey(f => f.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Fridge>()
                .HasOne(f => f.Supplier)
                .WithMany(s => s.Fridges)
                .HasForeignKey(f => f.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            // FridgeAllocation relationships
            modelBuilder.Entity<FridgeAllocation>()
                .HasOne(fa => fa.Fridge)
                .WithMany(f => f.Allocations)
                .HasForeignKey(fa => fa.FridgeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FridgeAllocation>()
                .HasOne(fa => fa.Customer)
                .WithMany(c => c.Allocations)
                .HasForeignKey(fa => fa.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // FridgeFault technician assignment
            modelBuilder.Entity<FridgeFault>()
                .HasOne(ff => ff.AssignedTechnician)
                .WithMany(u => u.AssignedFaults)
                .HasForeignKey(ff => ff.AssignedTechnicianId)
                .OnDelete(DeleteBehavior.Restrict);

            // FridgeMaintenance technician assignment
            modelBuilder.Entity<FridgeMaintenance>()
                .HasOne(fm => fm.Technician)
                .WithMany(u => u.AssignedMaintenances)
                .HasForeignKey(fm => fm.TechnicianId)
                .OnDelete(DeleteBehavior.Restrict);


            modelBuilder.Entity<FridgeRequest>()
                    .HasOne(fr => fr.Customer)
                    .WithMany(c => c.FridgeRequests)
                    .HasForeignKey(fr => fr.CustomerId)
                    .OnDelete(DeleteBehavior.Cascade); // This avoids multiple cascade paths

            modelBuilder.Entity<FridgeRequest>()
                .HasOne(fr => fr.Location)
                .WithMany(l => l.FridgeRequests)
                .HasForeignKey(fr => fr.LocationId)
                .OnDelete(DeleteBehavior.Restrict); // This avoids multiple cascade paths

            //PurchaseOrder.Items is the collection navigation for PurchaseOrderItem.PurchaseOrder.
            modelBuilder.Entity<PurchaseOrderItem>()
                .HasOne(poi => poi.PurchaseOrder)
                .WithMany(po => po.Items)
                .HasForeignKey(poi => poi.PurchaseOrderId);

            //Enforce uniqueness of each InventoryItem per PurchaseOrder
            modelBuilder.Entity<PurchaseOrderItem>()
                .HasOne(poi => poi.InventoryItem)
                .WithMany(ii => ii.PurchaseOrderItems)
                .HasForeignKey(poi => poi.InventoryItemId);

        }
    }
}
