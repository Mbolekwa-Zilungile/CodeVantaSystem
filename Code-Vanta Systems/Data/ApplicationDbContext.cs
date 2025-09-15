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






        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure decimal precision
            modelBuilder.Entity<Quotation>()
                .Property(q => q.UnitPrice)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<PurchaseOrderItem>()
                .Property(poi => poi.UnitPrice)
                .HasColumnType("decimal(18,2)");

            // Fridge relationships
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

            // Fridge allocation
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

            // Fault technician assignment
            modelBuilder.Entity<FridgeFault>()
                .HasOne(ff => ff.AssignedTechnician)
                .WithMany(u => u.AssignedFaults)
                .HasForeignKey(ff => ff.AssignedTechnicianId)
                .OnDelete(DeleteBehavior.Restrict);

            // Maintenance technician assignment
            modelBuilder.Entity<FridgeMaintenance>()
                .HasOne(fm => fm.Technician)
                .WithMany(u => u.AssignedMaintenances)
                .HasForeignKey(fm => fm.TechnicianId)
                .OnDelete(DeleteBehavior.Restrict);

            // Service history
            modelBuilder.Entity<FridgeServiceHistory>()
                .HasOne(fsh => fsh.Fridge)
                .WithMany(f => f.ServiceHistories)
                .HasForeignKey(fsh => fsh.FridgeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Usage logs
            modelBuilder.Entity<FridgeUsageLog>()
                .HasOne(fu => fu.Fridge)
                .WithMany(f => f.UsageLogs)
                .HasForeignKey(fu => fu.FridgeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Maintenance schedule
            modelBuilder.Entity<MaintenanceSchedule>()
                .HasOne(ms => ms.Fridge)
                .WithMany(f => f.MaintenanceSchedules)
                .HasForeignKey(ms => ms.FridgeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Fridge requests
            modelBuilder.Entity<FridgeRequest>()
                .HasOne(fr => fr.Customer)
                .WithMany(c => c.FridgeRequests)
                .HasForeignKey(fr => fr.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FridgeRequest>()
                .HasOne(fr => fr.Location)
                .WithMany(l => l.FridgeRequests)
                .HasForeignKey(fr => fr.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            // Purchase order items
            modelBuilder.Entity<PurchaseOrderItem>()
                .HasOne(poi => poi.PurchaseOrder)
                .WithMany(po => po.Items)
                .HasForeignKey(poi => poi.PurchaseOrderId);

            modelBuilder.Entity<PurchaseOrderItem>()
                .HasOne(poi => poi.InventoryItem)
                .WithMany(ii => ii.PurchaseOrderItems)
                .HasForeignKey(poi => poi.InventoryItemId);

            // Employee ↔ User
            modelBuilder.Entity<ApplicationUser>()
                .HasOne(a => a.EmployeeProfile)
                .WithOne(e => e.User)
                .HasForeignKey<Employee>(e => e.ApplicationUserId);

            // Purchase request ↔ User
            modelBuilder.Entity<PurchaseRequest>()
                .HasOne(pr => pr.RequestedBy)
                .WithMany(u => u.PurchaseRequests)
                .HasForeignKey(pr => pr.RequestedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
}
