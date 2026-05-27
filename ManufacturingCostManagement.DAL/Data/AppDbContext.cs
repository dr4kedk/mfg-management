using Microsoft.EntityFrameworkCore;
using ManufacturingCostManagement.DAL.Entities;

namespace ManufacturingCostManagement.DAL.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Role> Roles => Set<Role>();
        public DbSet<User> Users => Set<User>();
        public DbSet<Supplier> Suppliers => Set<Supplier>();
        public DbSet<Material> Materials => Set<Material>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<BillOfMaterial> BillOfMaterials => Set<BillOfMaterial>();
        public DbSet<ProductionOrder> ProductionOrders => Set<ProductionOrder>();
        public DbSet<LaborCost> LaborCosts => Set<LaborCost>();
        public DbSet<OverheadCost> OverheadCosts => Set<OverheadCost>();
        public DbSet<Department> Departments => Set<Department>();
        public DbSet<Permission> Permissions => Set<Permission>();
        public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Role>().HasIndex(r => r.Name).IsUnique();
            modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();
            modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
            modelBuilder.Entity<Material>().HasIndex(m => m.Code).IsUnique();
            modelBuilder.Entity<Product>().HasIndex(p => p.Code).IsUnique();
            modelBuilder.Entity<ProductionOrder>().HasIndex(p => p.OrderCode).IsUnique();
            modelBuilder.Entity<Department>().HasIndex(d => d.Code).IsUnique();
            modelBuilder.Entity<Permission>().HasIndex(p => p.Code).IsUnique();
            modelBuilder.Entity<RolePermission>().HasIndex(rp => new { rp.RoleId, rp.PermissionId }).IsUnique();
            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Role).WithMany(r => r.RolePermissions)
                .HasForeignKey(rp => rp.RoleId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Permission).WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionId).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Material>()
                .HasOne(m => m.Supplier)
                .WithMany(s => s.Materials)
                .HasForeignKey(m => m.SupplierId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<BillOfMaterial>()
                .HasOne(b => b.Product)
                .WithMany(p => p.BillOfMaterials)
                .HasForeignKey(b => b.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BillOfMaterial>()
                .HasOne(b => b.Material)
                .WithMany(m => m.BillOfMaterials)
                .HasForeignKey(b => b.MaterialId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductionOrder>()
                .HasOne(p => p.Product)
                .WithMany(p => p.ProductionOrders)
                .HasForeignKey(p => p.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductionOrder>()
                .HasOne(p => p.Department)
                .WithMany(d => d.ProductionOrders)
                .HasForeignKey(p => p.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<LaborCost>()
                .HasOne(l => l.ProductionOrder)
                .WithMany(p => p.LaborCosts)
                .HasForeignKey(l => l.ProductionOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LaborCost>()
                .HasOne(l => l.Department)
                .WithMany(d => d.LaborCosts)
                .HasForeignKey(l => l.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<OverheadCost>()
                .HasOne(o => o.ProductionOrder)
                .WithMany(p => p.OverheadCosts)
                .HasForeignKey(o => o.ProductionOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OverheadCost>()
                .HasOne(o => o.Department)
                .WithMany(d => d.OverheadCosts)
                .HasForeignKey(o => o.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
