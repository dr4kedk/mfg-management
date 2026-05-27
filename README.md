# Manufacturing Cost Management

ASP.NET Core 8 MVC application for tracking manufacturing costs across materials, labor, and overhead.

## Architecture (3-Layer)

```
ManufacturingCostManagement.Web   - Presentation (Controllers / Views / ViewModels)
ManufacturingCostManagement.BLL   - Business Logic (Services + DTOs + Interfaces)
ManufacturingCostManagement.DAL   - Data Access (Entities + DbContext + Repositories)
```

## OOP Demonstrations

| Concept        | Where it lives                                                                                |
|----------------|------------------------------------------------------------------------------------------------|
| Class          | All entity, service, controller files                                                          |
| Interface      | `IRepository<T>`, `IBaseService<T>`, `IAuthService`, `IStatisticsService`, etc.                 |
| Inheritance    | `BaseEntity` -> all entities; `CostEntry` -> `LaborCost`, `OverheadCost`; `BaseService<T>` -> concrete services |
| Polymorphism   | `CostEntry.CalculateTotal()` overridden by LaborCost (hours*rate) and OverheadCost (amount); `BaseService<T>.ApplySearch` overridden in every concrete service |

## Tech Stack
- ASP.NET Core 8 MVC
- Entity Framework Core 8 (SQL Server)
- Cookie Authentication + Role-based Authorization (Admin / Manager / Accountant / Employee)
- BCrypt for password hashing
- Bootstrap 5 + Chart.js (CDN)

## Features

### Core CRUD modules
- **Suppliers** — manage raw-material vendors
- **Materials** — raw material inventory with stock + reorder levels
- **Products** — finished goods catalogue
- **Bill of Materials (BOM)** — links products to required materials per unit
- **Production Orders** — manufacturing runs with status (Pending/InProgress/Completed/Cancelled)
- **Labor Costs** — workers, hours, hourly rate per production order
- **Overhead Costs** — electricity/rent/equipment/maintenance/other per production order
- **Users & Roles** — admin user management

### Search
- Server-side, paginated search on all entity lists by keyword

### Statistics / Reports
- **Dashboard** — counts, status breakdown, total costs, monthly stacked-bar cost chart, top-5 costly products doughnut
- **Cost Analysis** — per-product cost breakdown (material/labor/overhead/unit), selling price, profit margin %
- **Low Stock** — materials at or below reorder level

### Cost calculation engine
- Material cost is computed from the BOM (`sum(qty * unitCost)`)
- Production order total = material + labor (`hours * rate`) + overhead (`amount`)
- Recalculate button on order details refreshes all totals using polymorphic `CalculateTotal()` on each `CostEntry`

### Role-based authorization
| Role        | Permissions                                                                |
|-------------|----------------------------------------------------------------------------|
| Admin       | Everything, including users / delete operations                            |
| Manager     | Manage suppliers/materials/products/BOM/production orders                  |
| Accountant  | Add labor & overhead cost entries, view all                                |
| Employee    | View-only access to all modules                                            |

## Default Accounts (seeded)
| Username    | Password    | Role       |
|-------------|-------------|------------|
| admin       | admin123    | Admin      |
| manager     | manager123  | Manager    |
| accountant  | acc123      | Accountant |

## Running

### Prerequisites
- .NET 8 SDK
- SQL Server (LocalDB on Windows, or full SQL Server)

### Connection string
Edit `ManufacturingCostManagement.Web/appsettings.json`:
```json
"DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=ManufacturingCostDB;Trusted_Connection=True;TrustServerCertificate=True"
```

### Build & run
```bash
dotnet build
dotnet run --project ManufacturingCostManagement.Web
```

The app will:
1. Apply migrations automatically (`db.MigrateAsync()`)
2. Seed roles, default users, suppliers, materials, products, and BOM entries
3. Open at https://localhost:5xxx (login as `admin/admin123`)

### EF Migrations (if you change entities)
```bash
dotnet ef migrations add <Name> --project ManufacturingCostManagement.DAL --startup-project ManufacturingCostManagement.Web
dotnet ef database update --project ManufacturingCostManagement.DAL --startup-project ManufacturingCostManagement.Web
```
