using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ManufacturingCostManagement.DAL.Data;
using ManufacturingCostManagement.DAL.Entities;

namespace ManufacturingCostManagement.BLL.Services
{
    public static class DbSeeder
    {
        private static readonly Random _rng = new(42);

        public static async Task SeedAsync(AppDbContext context, IEnumerable<(string Code, string Module, string Action, string Description)>? permissions = null, IDictionary<string, string[]>? rolePermissionMap = null)
        {
            await context.Database.MigrateAsync();

            await SeedRolesAsync(context);
            await SeedDepartmentsAsync(context);
            await SeedUsersAsync(context);
            await MigrateUsernamesAsync(context);
            await SeedSuppliersAsync(context);
            await SeedMaterialsAsync(context);
            await SeedProductsAsync(context);
            await SeedBomAsync(context);
            await SeedProductionOrdersAsync(context);
            await SeedLaborCostsAsync(context);
            await SeedOverheadCostsAsync(context);
            await RecalculateOrderTotalsAsync(context);

            if (permissions != null) await SeedPermissionsAsync(context, permissions, rolePermissionMap);
        }

        private static async Task SeedPermissionsAsync(
            AppDbContext context,
            IEnumerable<(string Code, string Module, string Action, string Description)> all,
            IDictionary<string, string[]>? rolePermissionMap)
        {
            // upsert permission catalog
            foreach (var (code, module, action, desc) in all)
            {
                var existing = await context.Permissions.FirstOrDefaultAsync(p => p.Code == code);
                if (existing == null)
                {
                    context.Permissions.Add(new Permission { Code = code, Module = module, Action = action, Description = desc });
                }
                else
                {
                    existing.Module = module;
                    existing.Action = action;
                    existing.Description = desc;
                }
            }
            await context.SaveChangesAsync();

            if (rolePermissionMap == null) return;

            // Only initialize role mappings if no rows exist yet (otherwise we'd overwrite admin's changes)
            if (await context.RolePermissions.AnyAsync()) return;

            var allPerms = await context.Permissions.ToListAsync();
            var allCodes = allPerms.Select(p => p.Code).ToHashSet();

            foreach (var kvp in rolePermissionMap)
            {
                var role = await context.Roles.FirstOrDefaultAsync(r => r.Name == kvp.Key);
                if (role == null) continue;

                IEnumerable<string> codesToAdd = kvp.Value.Length == 1 && kvp.Value[0] == "*"
                    ? allCodes
                    : kvp.Value.Where(c => allCodes.Contains(c));

                foreach (var code in codesToAdd)
                {
                    var perm = allPerms.First(p => p.Code == code);
                    context.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = perm.Id });
                }
            }
            await context.SaveChangesAsync();
        }

        // One-time migration: rename "user01"..."userNN" to the prefix of their email
        private static async Task MigrateUsernamesAsync(AppDbContext context)
        {
            var legacy = await context.Users
                .Where(u => u.Username.StartsWith("user") && u.Username != "user")
                .ToListAsync();
            if (!legacy.Any()) return;

            foreach (var u in legacy)
            {
                var at = u.Email.IndexOf('@');
                if (at <= 0) continue;
                var candidate = u.Email.Substring(0, at).ToLowerInvariant();
                var clash = await context.Users.AnyAsync(x => x.Username == candidate && x.Id != u.Id);
                if (!clash) u.Username = candidate;
            }
            await context.SaveChangesAsync();
        }

        private static async Task SeedRolesAsync(AppDbContext context)
        {
            if (await context.Roles.AnyAsync()) return;
            context.Roles.AddRange(
                new Role { Name = "Admin", Description = "Full access to system" },
                new Role { Name = "Manager", Description = "Manage production and view all data" },
                new Role { Name = "Accountant", Description = "Manage costs and view reports" },
                new Role { Name = "Employee", Description = "View-only access" }
            );
            await context.SaveChangesAsync();
        }

        private static async Task SeedDepartmentsAsync(AppDbContext context)
        {
            var depts = new[]
            {
                ("DEP-ASM", "Assembly", "Mr. Tran Van An", "Final product assembly line"),
                ("DEP-MAC", "Machining", "Mr. Le Van Binh", "CNC machining and metalwork"),
                ("DEP-PNT", "Painting", "Ms. Nguyen Thi Cuc", "Surface coating and painting"),
                ("DEP-WEL", "Welding", "Mr. Pham Van Duc", "Welding and metal joining"),
                ("DEP-ELE", "Electronics", "Ms. Vu Thi Hanh", "PCB assembly and electronics"),
                ("DEP-QC",  "Quality Control", "Mr. Hoang Van Phuc", "Quality inspection and testing"),
                ("DEP-PKG", "Packaging", "Ms. Dang Thi Giang", "Final packaging and labeling"),
                ("DEP-WHS", "Warehouse", "Mr. Bui Van Hai", "Raw material and finished goods storage"),
                ("DEP-MNT", "Maintenance", "Mr. Do Van Khanh", "Equipment maintenance and repair"),
                ("DEP-RND", "R&D", "Dr. Mai Thi Lan", "Product research and development")
            };
            foreach (var (code, name, manager, desc) in depts)
            {
                var existing = await context.Departments.FirstOrDefaultAsync(d => d.Code == code);
                if (existing == null)
                {
                    context.Departments.Add(new Department { Code = code, Name = name, Manager = manager, Description = desc });
                }
                else
                {
                    existing.Name = name;
                    existing.Manager = manager;
                    existing.Description = desc;
                }
            }
            await context.SaveChangesAsync();
        }

        private static async Task SeedUsersAsync(AppDbContext context)
        {
            if (await context.Users.AnyAsync()) return;
            var adminRoleId = context.Roles.First(r => r.Name == "Admin").Id;
            var managerRoleId = context.Roles.First(r => r.Name == "Manager").Id;
            var accountantRoleId = context.Roles.First(r => r.Name == "Accountant").Id;
            var employeeRoleId = context.Roles.First(r => r.Name == "Employee").Id;

            var users = new List<User>
            {
                new() { Username = "admin", Email = "admin@mfg.local", FullName = "System Administrator", PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"), RoleId = adminRoleId, IsActive = true },
                new() { Username = "manager", Email = "manager@mfg.local", FullName = "Production Manager", PasswordHash = BCrypt.Net.BCrypt.HashPassword("manager123"), RoleId = managerRoleId, IsActive = true },
                new() { Username = "accountant", Email = "accountant@mfg.local", FullName = "Cost Accountant", PasswordHash = BCrypt.Net.BCrypt.HashPassword("acc123"), RoleId = accountantRoleId, IsActive = true }
            };

            var firstNames = new[] { "An", "Binh", "Cuong", "Dung", "Em", "Phong", "Giang", "Huy", "Khanh", "Long", "Minh", "Nam", "Oanh", "Phuc", "Quynh", "Son", "Thao", "Uyen", "Viet", "Yen" };
            var lastNames = new[] { "Nguyen", "Tran", "Le", "Pham", "Hoang", "Vu", "Dang", "Bui", "Do", "Ho" };
            var roles = new[] { managerRoleId, accountantRoleId, employeeRoleId, employeeRoleId, employeeRoleId };

            for (int i = 0; i < 22; i++)
            {
                var fn = firstNames[_rng.Next(firstNames.Length)];
                var ln = lastNames[_rng.Next(lastNames.Length)];
                var userKey = $"{fn.ToLower()}.{ln.ToLower()}{i + 1}";
                users.Add(new User
                {
                    Username = userKey,
                    Email = $"{userKey}@mfg.local",
                    FullName = $"{ln} Van {fn}",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                    RoleId = roles[_rng.Next(roles.Length)],
                    IsActive = _rng.NextDouble() > 0.1
                });
            }
            context.Users.AddRange(users);
            await context.SaveChangesAsync();
        }

        private static async Task SeedSuppliersAsync(AppDbContext context)
        {
            if (await context.Suppliers.AnyAsync()) return;
            var data = new[]
            {
                ("Steel Corp Vietnam", "Mr. John Nguyen", "123 Industrial Rd, HCM"),
                ("Plastic World Co.", "Ms. Anna Tran", "45 Polymer Ave, Hanoi"),
                ("Electronics Inc.", "Mr. Lee Pham", "78 Circuit St, HCM"),
                ("CopperLine Vietnam", "Mr. David Le", "12 Wire St, Da Nang"),
                ("Glass & Optics JSC", "Ms. Linh Hoang", "56 Crystal Rd, Hai Phong"),
                ("Rubber Mart", "Mr. Khanh Vu", "78 Latex Ave, HCM"),
                ("Adhesive Pro", "Ms. Mai Dang", "90 Bond St, Hanoi"),
                ("Premium Aluminum", "Mr. Hung Bui", "11 Light Metal Rd, HCM"),
                ("Display Tech Co.", "Ms. Hoa Do", "22 Panel Ave, Hai Phong"),
                ("Battery Solutions", "Mr. Tuan Ho", "33 Power St, HCM"),
                ("Chemicals & Solvents", "Ms. Phuong Tran", "44 Chemical Rd, Hanoi"),
                ("Bolt & Fastener Co.", "Mr. Quang Le", "55 Hardware Ave, HCM"),
                ("Wood Source JSC", "Ms. Loan Pham", "66 Timber St, Da Nang"),
                ("Textile Mart", "Mr. Trung Hoang", "77 Fabric Rd, HCM"),
                ("Packaging Hub", "Ms. Trang Vu", "88 Box Ave, Hanoi"),
                ("Microchip Asia", "Mr. Bao Bui", "99 Silicon St, HCM"),
                ("LED Lighting JSC", "Ms. Yen Do", "101 Light Rd, Hai Phong"),
                ("Motor Components Co.", "Mr. Khoa Ho", "112 Motor Ave, HCM"),
                ("Sensor Systems Vietnam", "Ms. Nga Tran", "123 Sensor St, Hanoi"),
                ("Industrial Tools JSC", "Mr. Phat Le", "134 Tool Rd, HCM"),
                ("Cable Pro", "Ms. Hanh Pham", "145 Cable Ave, Da Nang"),
                ("Recycled Materials Co.", "Mr. Son Hoang", "156 Green St, HCM"),
                ("Resin & Polymer JSC", "Ms. Lan Vu", "167 Resin Rd, Hanoi"),
                ("Hardware Plus", "Mr. Tien Bui", "178 Hardware Ave, HCM"),
                ("Precision Parts Vietnam", "Ms. Thuy Do", "189 Precision St, Hai Phong")
            };
            int idx = 0;
            context.Suppliers.AddRange(data.Select(d => new Supplier
            {
                Name = d.Item1,
                ContactPerson = d.Item2,
                Phone = $"09{_rng.Next(10000000, 99999999)}",
                Email = $"sales{++idx}@{d.Item1.Replace(" ", "").ToLower()}.com",
                Address = d.Item3
            }));
            await context.SaveChangesAsync();
        }

        private static async Task SeedMaterialsAsync(AppDbContext context)
        {
            if (await context.Materials.AnyAsync()) return;
            var supplierIds = await context.Suppliers.Select(s => s.Id).ToListAsync();
            var matSpec = new (string Name, string Unit, decimal Cost)[]
            {
                ("Steel Sheet 2mm", "kg", 25000), ("Plastic Pellet ABS", "kg", 45000),
                ("Microcontroller AVR", "pcs", 35000), ("Copper Wire 1.5mm", "m", 8000),
                ("LCD Display 16x2", "pcs", 120000), ("Aluminum Sheet 1mm", "kg", 60000),
                ("Rubber Gasket", "pcs", 3500), ("Silicone Sealant", "tube", 28000),
                ("Tempered Glass 3mm", "m2", 180000), ("LED Strip 5m", "roll", 75000),
                ("Lithium Battery 2000mAh", "pcs", 95000), ("PCB Single-Sided", "pcs", 22000),
                ("Resistor Pack 1k", "pack", 15000), ("Capacitor 100uF", "pcs", 1200),
                ("Screw M3x10", "box", 12000), ("Hex Bolt M6", "box", 18000),
                ("Plywood 12mm", "m2", 95000), ("Cotton Fabric", "m", 22000),
                ("Cardboard Box Large", "pcs", 5000), ("Bubble Wrap 50m", "roll", 65000),
                ("Stainless Steel Tube", "m", 38000), ("Cable Sleeve PVC", "m", 4500),
                ("Power Adapter 12V", "pcs", 85000), ("DC Motor 24V", "pcs", 140000),
                ("Temperature Sensor", "pcs", 45000), ("Pressure Switch", "pcs", 65000),
                ("Heat Shrink Tube", "m", 2500), ("Solder Wire", "kg", 280000)
            };

            for (int i = 0; i < matSpec.Length; i++)
            {
                var s = matSpec[i];
                context.Materials.Add(new Material
                {
                    Code = $"MAT-{i + 1:D3}",
                    Name = s.Name,
                    Unit = s.Unit,
                    UnitCost = s.Cost,
                    StockQuantity = _rng.Next(20, 800),
                    ReorderLevel = _rng.Next(30, 150),
                    SupplierId = supplierIds[_rng.Next(supplierIds.Count)]
                });
            }
            await context.SaveChangesAsync();
        }

        private static async Task SeedProductsAsync(AppDbContext context)
        {
            if (await context.Products.AnyAsync()) return;
            var prodSpec = new (string Name, string Category, decimal Price)[]
            {
                ("Smart Thermostat", "Electronics", 850000),
                ("Steel Bracket Kit", "Hardware", 220000),
                ("Industrial Control Panel", "Electronics", 1500000),
                ("LED Light Fixture", "Lighting", 320000),
                ("Aluminum Window Frame", "Construction", 1200000),
                ("Pressure Gauge Assy", "Instruments", 580000),
                ("Wooden Storage Cabinet", "Furniture", 2200000),
                ("Power Supply Unit 12V", "Electronics", 480000),
                ("Smart Door Lock", "Security", 1650000),
                ("Conveyor Roller", "Industrial", 950000),
                ("Air Filter Cartridge", "HVAC", 280000),
                ("Sensor Module Kit", "Electronics", 720000),
                ("Steel Pipe Assembly", "Hardware", 350000),
                ("LED Strip Controller", "Lighting", 410000),
                ("Solar Panel Junction Box", "Energy", 1180000),
                ("Industrial Fan 24V", "HVAC", 890000),
                ("Touchscreen Display 7\"", "Electronics", 2450000),
                ("Battery Pack 12V", "Energy", 1320000),
                ("Hydraulic Cylinder", "Industrial", 3850000),
                ("Motor Controller Board", "Electronics", 980000),
                ("Stainless Steel Tank", "Industrial", 4200000),
                ("Cable Junction Box", "Electrical", 185000),
                ("PVC Pipe Assembly", "Plumbing", 220000),
                ("Wall Bracket Set", "Hardware", 165000),
                ("Power Strip 6-Outlet", "Electronics", 240000)
            };

            for (int i = 0; i < prodSpec.Length; i++)
            {
                var s = prodSpec[i];
                context.Products.Add(new Product
                {
                    Code = $"PRD-{i + 1:D3}",
                    Name = s.Name,
                    Category = s.Category,
                    Unit = "pcs",
                    SellingPrice = s.Price,
                    Description = $"{s.Name} - manufactured product"
                });
            }
            await context.SaveChangesAsync();
        }

        private static async Task SeedBomAsync(AppDbContext context)
        {
            if (await context.BillOfMaterials.AnyAsync()) return;
            var products = await context.Products.ToListAsync();
            var materialIds = await context.Materials.Select(m => m.Id).ToListAsync();
            var boms = new List<BillOfMaterial>();
            foreach (var p in products)
            {
                var matCount = _rng.Next(2, 6);
                var picked = materialIds.OrderBy(_ => _rng.Next()).Take(matCount);
                foreach (var mId in picked)
                {
                    boms.Add(new BillOfMaterial
                    {
                        ProductId = p.Id,
                        MaterialId = mId,
                        QuantityRequired = Math.Round((decimal)(_rng.NextDouble() * 5 + 0.1), 4)
                    });
                }
            }
            context.BillOfMaterials.AddRange(boms);
            await context.SaveChangesAsync();
        }

        private static async Task SeedProductionOrdersAsync(AppDbContext context)
        {
            if (await context.ProductionOrders.AnyAsync()) return;
            var productIds = await context.Products.Select(p => p.Id).ToListAsync();
            var deptIds = await context.Departments.Select(d => d.Id).ToListAsync();
            var statuses = new[] { ProductionStatus.Pending, ProductionStatus.InProgress, ProductionStatus.Completed, ProductionStatus.Completed, ProductionStatus.Completed };

            for (int i = 0; i < 30; i++)
            {
                var status = statuses[_rng.Next(statuses.Length)];
                var startDate = DateTime.UtcNow.AddDays(-_rng.Next(0, 270));
                context.ProductionOrders.Add(new ProductionOrder
                {
                    OrderCode = $"PO-{startDate:yyyyMMdd}-{i + 1:D3}",
                    ProductId = productIds[_rng.Next(productIds.Count)],
                    DepartmentId = deptIds[_rng.Next(deptIds.Count)],
                    Quantity = _rng.Next(10, 500),
                    StartDate = startDate,
                    EndDate = status == ProductionStatus.Completed ? startDate.AddDays(_rng.Next(3, 21)) : (DateTime?)null,
                    Status = status,
                    Notes = $"Batch order #{i + 1}"
                });
            }
            await context.SaveChangesAsync();
        }

        private static async Task SeedLaborCostsAsync(AppDbContext context)
        {
            if (await context.LaborCosts.AnyAsync()) return;
            var orders = await context.ProductionOrders.ToListAsync();
            var workerNames = new[] { "Nguyen Van A", "Tran Van B", "Le Van C", "Pham Van D", "Hoang Van E", "Vu Van F", "Dang Van G", "Bui Van H", "Do Thi I", "Ho Thi K", "Mai Van L", "Phan Thi M", "Truong Van N", "Ly Thi O", "Cao Van P" };

            foreach (var order in orders)
            {
                var laborEntries = _rng.Next(1, 4);
                for (int j = 0; j < laborEntries; j++)
                {
                    var hours = Math.Round((decimal)(_rng.NextDouble() * 40 + 8), 2);
                    var rate = (decimal)_rng.Next(40000, 120000);
                    context.LaborCosts.Add(new LaborCost
                    {
                        ProductionOrderId = order.Id,
                        DepartmentId = order.DepartmentId,
                        WorkerName = workerNames[_rng.Next(workerNames.Length)],
                        HoursWorked = hours,
                        HourlyRate = rate,
                        Amount = hours * rate,
                        IncurredAt = order.StartDate.AddDays(_rng.Next(0, 10)),
                        Description = $"Labor for {order.OrderCode}"
                    });
                }
            }
            await context.SaveChangesAsync();
        }

        private static async Task SeedOverheadCostsAsync(AppDbContext context)
        {
            if (await context.OverheadCosts.AnyAsync()) return;
            var orders = await context.ProductionOrders.ToListAsync();
            var overheadTypes = new[] {
                (OverheadType.Electricity, "Workshop electricity"),
                (OverheadType.Rent, "Factory floor rent allocation"),
                (OverheadType.Equipment, "CNC machine depreciation"),
                (OverheadType.Maintenance, "Tool maintenance"),
                (OverheadType.Other, "Cleaning supplies")
            };

            foreach (var order in orders)
            {
                var overheadEntries = _rng.Next(1, 3);
                for (int j = 0; j < overheadEntries; j++)
                {
                    var (type, cat) = overheadTypes[_rng.Next(overheadTypes.Length)];
                    context.OverheadCosts.Add(new OverheadCost
                    {
                        ProductionOrderId = order.Id,
                        DepartmentId = order.DepartmentId,
                        Type = type,
                        Category = cat,
                        Amount = _rng.Next(200000, 5000000),
                        IncurredAt = order.StartDate.AddDays(_rng.Next(0, 10)),
                        Description = $"{type} cost for {order.OrderCode}"
                    });
                }
            }
            await context.SaveChangesAsync();
        }

        private static async Task RecalculateOrderTotalsAsync(AppDbContext context)
        {
            var orders = await context.ProductionOrders
                .Include(o => o.LaborCosts)
                .Include(o => o.OverheadCosts)
                .ToListAsync();

            var boms = await context.BillOfMaterials.Include(b => b.Material).ToListAsync();

            foreach (var o in orders)
            {
                if (o.TotalCost != 0) continue;
                var materialCostPerUnit = boms
                    .Where(b => b.ProductId == o.ProductId)
                    .Sum(b => b.QuantityRequired * (b.Material?.UnitCost ?? 0));
                o.TotalMaterialCost = materialCostPerUnit * o.Quantity;
                o.TotalLaborCost = o.LaborCosts.Sum(l => l.CalculateTotal());
                o.TotalOverheadCost = o.OverheadCosts.Sum(x => x.CalculateTotal());
                o.TotalCost = o.TotalMaterialCost + o.TotalLaborCost + o.TotalOverheadCost;
            }
            await context.SaveChangesAsync();
        }
    }
}
