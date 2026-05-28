# Bài thuyết trình: Ứng dụng Quản lý Chi phí Sản xuất

> Mục đích: trình bày toàn cảnh dự án cho giáo viên — các tính năng, cách hiện thực và vị trí mã nguồn tương ứng.

---

## 0. Thiết kế cơ sở dữ liệu

### 0.1. Sơ đồ quan hệ (ERD)

Toàn bộ schema (10 bảng nghiệp vụ + 2 bảng auth) khai báo trong:
**`ManufacturingCostManagement.DAL/Data/AppDbContext.cs`**

```
                     ┌──────────────────────┐
                     │      Roles           │
                     │  Id PK · Name UK     │
                     └─────┬────────┬───────┘
                  (Restrict)│        │(Cascade)
                  1───*     │        │1───*
                            ▼        ▼
                  ┌────────────┐  ┌──────────────────┐         ┌─────────────────┐
                  │   Users    │  │ RolePermissions  │1───*◄───┤   Permissions   │
                  │ Id PK      │  │ RoleId+PermId UK │         │ Id · Code UK    │
                  │ RoleId FK  │  └──────────────────┘         └─────────────────┘
                  │ UserName UK│           (Cascade)
                  │ Email UK   │
                  └────────────┘

  ┌────────────┐                                           ┌─────────────────┐
  │ Suppliers  │1                                          │   Departments   │
  │ Id PK      │     (SetNull)                             │ Id · Code UK    │
  └─────┬──────┘     0..1───*                              └──────┬──────────┘
        │                                                          │
        ▼                                                          │
  ┌─────────────────┐                                              │
  │   Materials     │                                              │
  │ Id · Code UK    │                                              │
  │ SupplierId FK?  │                                              │
  └────┬────────────┘                                              │
       │ (Restrict) *────1                                         │
       │                                                           │
       ▼                                                           │
  ┌─────────────────┐                                              │
  │ BillOfMaterials │           ┌────────────────┐                 │
  │ ProductId FK    │*────1     │   Products     │                 │
  │ MaterialId FK   │──────────►│ Id · Code UK   │                 │
  │ QuantityRequired│ (Cascade) │ SellingPrice   │                 │
  └─────────────────┘           └────────┬───────┘                 │
                                         │                         │
                                         │ (Restrict) 1───*        │
                                         ▼                         │
                                ┌────────────────────┐             │
                                │ ProductionOrders   │             │
                                │ Id · OrderCode UK  │             │
                                │ ProductId FK       │             │
                                │ DepartmentId FK?   │◄────────────┘
                                │ Status (enum)      │  (SetNull) *──0..1
                                │ TotalMaterialCost  │
                                │ TotalLaborCost     │
                                │ TotalOverheadCost  │
                                │ TotalCost          │
                                └────────┬───────────┘
                          (Cascade) 1───*│   (Cascade) 1───*
                ┌─────────────────────┐  │  ┌──────────────────────┐
                │     LaborCosts      │◄─┘  │    OverheadCosts     │
                │ Id                  │     │ Id · Type (enum)     │
                │ ProductionOrderId   │     │ ProductionOrderId    │
                │ DepartmentId FK?    │     │ DepartmentId FK?     │
                │ WorkerName          │     │ Category             │
                │ HoursWorked         │     │ Amount               │
                │ HourlyRate          │     │                      │
                │ ───────────────────│     │ ────────────────────│
                │  CalculateTotal()   │     │  CalculateTotal()    │
                │  = Hours × Rate     │     │  = Amount            │
                └─────────────────────┘     └──────────────────────┘
                       (cùng kế thừa abstract class CostEntry — đa hình)
```

### 0.2. Bảng quan hệ chi tiết

| # | Bảng cha → Bảng con | Loại | OnDelete | Ý nghĩa nghiệp vụ | Khai báo |
|---|---|---|---|---|---|
| 1 | **Roles** 1 → * **Users** | 1-n | Restrict | Một vai trò có nhiều người dùng; không cho xoá vai trò khi còn user | `AppDbContext.cs:44–47` |
| 2 | **Roles** 1 → * **RolePermissions** | 1-n | Cascade | Xoá vai trò sẽ xoá kèm dòng phân quyền | `AppDbContext.cs:37–38` |
| 3 | **Permissions** 1 → * **RolePermissions** | 1-n | Cascade | Xoá quyền sẽ xoá kèm dòng phân quyền | `AppDbContext.cs:40–41` |
| 4 | **Roles ↔ Permissions** qua `RolePermissions` | **n-n** | — | Mỗi vai trò có nhiều quyền, mỗi quyền có thể gán nhiều vai trò. Unique index `(RoleId, PermissionId)` chống trùng. | `AppDbContext.cs:33–34` |
| 5 | **Suppliers** 1 → * **Materials** | 1-n | SetNull | NVL thuộc nhà cung cấp; khi xoá NCC, các NVL chỉ bị set `SupplierId = NULL` (không mất hàng tồn) | `AppDbContext.cs:50–53` |
| 6 | **Products** 1 → * **BillOfMaterials** | 1-n | Cascade | Mỗi sản phẩm có nhiều dòng BOM; xoá sản phẩm thì xoá luôn BOM | `AppDbContext.cs:56–59` |
| 7 | **Materials** 1 → * **BillOfMaterials** | 1-n | Restrict | Một NVL được dùng trong nhiều BOM; không cho xoá NVL khi còn BOM tham chiếu | `AppDbContext.cs:62–65` |
| 8 | **Products ↔ Materials** qua `BillOfMaterials` | **n-n** có **payload** | — | Quan hệ many-to-many kèm thuộc tính `QuantityRequired` (định mức NVL cho 1 đơn vị sản phẩm) | `Entities/BillOfMaterial.cs` |
| 9 | **Products** 1 → * **ProductionOrders** | 1-n | Restrict | Mỗi đơn sản xuất luôn thuộc 1 sản phẩm; không cho xoá sản phẩm khi còn đơn | `AppDbContext.cs:68–71` |
| 10 | **Departments** 1 → * **ProductionOrders** | 1-n (tuỳ chọn) | SetNull | Đơn hàng có thể không gán bộ phận (`DepartmentId` nullable) | `AppDbContext.cs:74–77` |
| 11 | **ProductionOrders** 1 → * **LaborCosts** | 1-n | Cascade | Xoá đơn → xoá luôn chi phí nhân công của đơn đó | `AppDbContext.cs:80–83` |
| 12 | **ProductionOrders** 1 → * **OverheadCosts** | 1-n | Cascade | Tương tự cho chi phí SXC | `AppDbContext.cs:92–95` |
| 13 | **Departments** 1 → * **LaborCosts** / **OverheadCosts** | 1-n (tuỳ chọn) | SetNull | Mỗi mục chi phí gắn bộ phận chịu trách nhiệm; xoá bộ phận chỉ NULL hoá | `AppDbContext.cs:86–89, 98–101` |

### 0.3. Lựa chọn OnDelete — vì sao?

| OnDelete | Khi nào dùng | Ví dụ |
|---|---|---|
| **Cascade** | Quan hệ chứa-bị-chứa: dữ liệu con không còn nghĩa nếu cha mất | Đơn sản xuất bị xoá → mục Labor/Overhead bị xoá theo |
| **Restrict** | Bảo vệ dữ liệu lịch sử / nghiệp vụ | Không cho xoá Product khi còn ProductionOrder; không cho xoá Material khi còn BOM |
| **SetNull** | Quan hệ tuỳ chọn — cha có thể "biến mất" mà con vẫn dùng được | Xoá Supplier ⇒ Material không còn nhà cung cấp; xoá Department ⇒ đơn hàng vẫn còn |

### 0.4. Unique index (chống trùng dữ liệu)

| Bảng | Cột | Ý nghĩa |
|---|---|---|
| Roles | `Name` | Tên vai trò là duy nhất |
| Users | `Username`, `Email` (mỗi cột 1 unique index) | Đăng nhập duy nhất |
| Departments | `Code` | Mã bộ phận duy nhất (DEP-ASM, DEP-MAC, …) |
| Materials | `Code` | Mã NVL duy nhất (MAT-001, …) |
| Products | `Code` | Mã sản phẩm duy nhất (PRD-001, …) |
| ProductionOrders | `OrderCode` | Mã đơn duy nhất (PO-20260326-001, …) |
| Permissions | `Code` | Mã quyền duy nhất (`Products.Edit`, …) |
| RolePermissions | `(RoleId, PermissionId)` | Một quyền chỉ gán 1 lần cho 1 vai trò |

Khai báo: `AppDbContext.cs` từ dòng **22–33** (`HasIndex(...).IsUnique()`).

### 0.5. Soft-delete

Tất cả entity kế thừa `BaseEntity` có cờ `IsDeleted` (`Entities/BaseEntity.cs`).
`Repository<T>` luôn lọc `Where(e => !e.IsDeleted)` trong các hàm `GetAllAsync`, `GetByIdAsync`, `Query()` (`DAL/Repositories/Repository.cs`) → "xoá" thực ra chỉ là **ẩn**, không mất dữ liệu lịch sử.

### 0.6. Enum lưu trữ dưới dạng `int`

| Enum | Giá trị | Bảng | Trường |
|---|---|---|---|
| `ProductionStatus` | 0=Pending · 1=InProgress · 2=Completed · 3=Cancelled | ProductionOrders | Status |
| `OverheadType` | 0=Electricity · 1=Rent · 2=Equipment · 3=Maintenance · 4=Other | OverheadCosts | Type |

EF Core map enum → int trong cột SQL, vẫn dùng được trong filter (`?status=2` ⇒ Completed).

---

## 1. Tổng quan dự án

**Tên đề tài:** Quản lý chi phí sản xuất theo từng bộ phận (Manufacturing Cost Management).

**Vấn đề giải quyết:** doanh nghiệp sản xuất cần theo dõi đầy đủ ba thành phần chi phí cho mỗi đơn hàng — **nguyên vật liệu, nhân công, sản xuất chung** — và phân tích chi phí theo từng **bộ phận** để biết tiền đi đâu trong nhà máy.

**Yêu cầu môn học đã đạt:**

| Yêu cầu | Đáp ứng |
|---|---|
| ASP.NET Core MVC | Project `ManufacturingCostManagement.Web` (.NET 8) |
| OOP: class, interface, kế thừa, đa hình | Có đủ — xem mục 3 |
| Kiến trúc 3 lớp | 3 project tách biệt: DAL → BLL → Web |
| SQL Server + EF Core | EF Core 8 + SQL Server qua Docker |
| CRUD, Search, Statistic | Tất cả 8 bảng có CRUD, search, filter |
| Phân quyền theo Role | 4 vai trò + ma trận quyền tuỳ chỉnh |
| Tính năng phụ | Đa ngôn ngữ EN/VI, Báo cáo biểu đồ, BOM, Cost rollup, Permission matrix |

---

## 2. Kiến trúc 3 lớp

```
┌─────────────────────────────────────────────┐
│  Lớp Trình bày (Web)                        │  ManufacturingCostManagement.Web
│   • Controllers, Views (Razor), ViewModels  │
│   • Authentication, Authorization, i18n     │
└─────────────────┬───────────────────────────┘
                  │ tham chiếu
┌─────────────────▼───────────────────────────┐
│  Lớp Nghiệp vụ (BLL)                        │  ManufacturingCostManagement.BLL
│   • Services + Interfaces                   │
│   • Tính chi phí, thống kê                  │
│   • Seeding dữ liệu mẫu                     │
└─────────────────┬───────────────────────────┘
                  │ tham chiếu
┌─────────────────▼───────────────────────────┐
│  Lớp Dữ liệu (DAL)                          │  ManufacturingCostManagement.DAL
│   • Entities, DbContext, Repositories       │
│   • Migrations (EF Core)                    │
└─────────────────────────────────────────────┘
```

Có thể thấy quan hệ tham chiếu trong file solution: `ManufacturingCostManagement.sln`.

---

## 3. OOP — Class, Interface, Kế thừa, Đa hình

### 3.1. Class & Interface
- **Class:** mọi entity (`Supplier`, `Material`, `Product`, …), service, controller đều là class.
- **Interface:** `IBaseService<T>`, `IRepository<T>`, `IAuthService`, `IStatisticsService`, …
  - `ManufacturingCostManagement.BLL/Interfaces/IBaseService.cs`
  - `ManufacturingCostManagement.BLL/Interfaces/IServices.cs`
  - `ManufacturingCostManagement.DAL/Repositories/IRepository.cs`

### 3.2. Kế thừa (Inheritance)

**Toàn bộ entity kế thừa `BaseEntity`** để có `Id`, `CreatedAt`, `UpdatedAt`, `IsDeleted` chung:
- `ManufacturingCostManagement.DAL/Entities/BaseEntity.cs`

**Service cụ thể kế thừa từ `BaseService<T>` generic** để tái sử dụng logic CRUD + phân trang:
- `ManufacturingCostManagement.BLL/Services/BaseService.cs:12`  → `public abstract class BaseService<T> : IBaseService<T> where T : BaseEntity`
- Ví dụ: `MaterialService : BaseService<Material>` tại `EntityServices.cs:110`.

### 3.3. Đa hình (Polymorphism) — **ví dụ tiêu biểu nhất**

File **`ManufacturingCostManagement.DAL/Entities/CostEntry.cs`** định nghĩa class trừu tượng:

```csharp
public abstract class CostEntry : BaseEntity        // dòng 7
{
    public abstract string CostType { get; }        // dòng 23
    public abstract decimal CalculateTotal();       // dòng 24
}
```

Hai lớp con override `CalculateTotal()` theo cách **hoàn toàn khác nhau** — đây chính là đa hình:

```csharp
public class LaborCost : CostEntry                                       // dòng 27
{
    public override decimal CalculateTotal() => HoursWorked * HourlyRate;// dòng 40
}

public class OverheadCost : CostEntry                                    // dòng 52
{
    public override decimal CalculateTotal() => Amount;                  // dòng 61
}
```

**Nơi đa hình được gọi** (cùng một hàm `CalculateTotal()` xử lý cả Labor lẫn Overhead):

- `ManufacturingCostManagement.BLL/Services/ProductionOrderService.cs:59–60`
  ```csharp
  order.TotalLaborCost    = order.LaborCosts.Sum(l => l.CalculateTotal());
  order.TotalOverheadCost = order.OverheadCosts.Sum(o => o.CalculateTotal());
  ```
- `ManufacturingCostManagement.BLL/Services/StatisticsService.cs:56–57` — gộp chi phí theo bộ phận.
- `ManufacturingCostManagement.Web/Controllers/ProductionOrdersController.cs:151` — khi thêm nhân công, tự gọi `CalculateTotal()` để gán `Amount`.

Ngoài ra, `BaseService<T>.ApplySearch()` là **template method** — lớp con bắt buộc override để cung cấp logic tìm kiếm riêng:
- `BaseService.cs:66` → `protected abstract IQueryable<T> ApplySearch(IQueryable<T> query, string? keyword);`
- Mỗi service con (xem `EntityServices.cs`) có cách tìm khác nhau (Material tìm theo Code/Name, ProductionOrder tìm theo OrderCode/Product/Department, …).

---

## 4. Mô hình dữ liệu (Database)

10 bảng chính, dùng EF Core Code-First → Migration:

| Bảng | File | Mục đích |
|---|---|---|
| Roles | `Entities/Role.cs` | Vai trò người dùng |
| Users | `Entities/User.cs` | Tài khoản |
| Permissions | `Entities/Permission.cs` | Catalog quyền |
| RolePermissions | `Entities/Permission.cs` | Bảng nối Role × Permission |
| Departments | `Entities/Department.cs` | Bộ phận sản xuất |
| Suppliers | `Entities/Supplier.cs` | Nhà cung cấp |
| Materials | `Entities/Material.cs` | Nguyên vật liệu |
| Products | `Entities/Product.cs` | Sản phẩm |
| BillOfMaterials | `Entities/BillOfMaterial.cs` | Định mức NVL (BOM) |
| ProductionOrders | `Entities/ProductionOrder.cs` | Đơn sản xuất |
| LaborCosts, OverheadCosts | `Entities/CostEntry.cs` | Chi phí nhân công/SXC |

**DbContext** tập trung mọi `DbSet` và quan hệ:
- `ManufacturingCostManagement.DAL/Data/AppDbContext.cs`

**Migrations** đã tạo:
- `InitialCreate` (20260526144642) — khởi tạo schema
- `AddDepartment` (20260526155253) — thêm bảng Department + FK
- `AddPermissions` (20260527164359) — thêm bảng Permission/RolePermission

Mỗi lần khởi động ứng dụng, `DbSeeder.SeedAsync()` gọi `MigrateAsync()` rồi nhồi dữ liệu mẫu:
- `ManufacturingCostManagement.BLL/Services/DbSeeder.cs:15`

**Dữ liệu mẫu** (deterministic — `Random(42)`):
- 4 Role · 10 Department · 25 User · 25 Supplier · 28 Material · 25 Product · 86 BOM · 30 Production Order · 60 Labor · 43 Overhead · 41 Permission.

---

## 5. Tính năng CRUD + Search + Filter

### 5.1. Pattern chung
Tất cả 8 module dùng cùng kiến trúc:

- **Repository chung** (`Repository<T>`) → `DAL/Repositories/Repository.cs`
- **Service** kế thừa `BaseService<T>` → `BLL/Services/EntityServices.cs`
- **Controller** dùng service, không truy cập DB trực tiếp
- **View** dùng partial `_SearchBar.cshtml`, `_Pager.cshtml`

### 5.2. Filter nâng cao (bộ lọc nhiều tiêu chí)

Mỗi service có thêm method `FilterAsync(...)`. Ví dụ:

- **Materials** — lọc theo nhà cung cấp + sắp hết:
  - `BLL/Services/EntityServices.cs:138` → `FilterAsync(string? q, int? supplierId, bool lowStock, …)`
- **Products** — lọc theo danh mục:
  - `EntityServices.cs:184`
- **ProductionOrders** — lọc theo Status / Department / Product:
  - `BLL/Services/ProductionOrderService.cs:71` → `FilterAsync(... ProductionStatus? status, int? departmentId, int? productId, …)`
- **Users** — lọc theo Role / Active:
  - `EntityServices.cs:48`

**Bộ phân trang giữ nguyên filter** khi chuyển trang — `Views/Shared/_Pager.cshtml` đọc nguyên query-string và chỉ thay `page=`.

### 5.3. View Filter Bar
Đặt trong card header của mỗi trang Index, ví dụ `Views/ProductionOrders/Index.cshtml` — gồm input tìm kiếm + select Status + select Department + select Product + nút **Lọc** và **Xóa bộ lọc**.

---

## 6. Tính chi phí sản xuất (Cost Calculation Engine)

### 6.1. Công thức

Một đơn sản xuất có **3 thành phần chi phí**:

| Thành phần | Công thức |
|---|---|
| Nguyên vật liệu | `Σ (BOM.QuantityRequired × Material.UnitCost) × Order.Quantity` |
| Nhân công | `Σ LaborCost.CalculateTotal()` = `Σ (HoursWorked × HourlyRate)` |
| Sản xuất chung | `Σ OverheadCost.CalculateTotal()` = `Σ Amount` |
| **TỔNG** | `Material + Labor + Overhead` |

### 6.2. Vị trí code

- **Chi phí NVL trên 1 đơn vị sản phẩm**:
  - `BLL/Services/EntityServices.cs:177` → `ProductService.CalculateMaterialCostAsync(int productId)`
- **Recalculate toàn bộ tổng cho 1 đơn hàng**:
  - `BLL/Services/ProductionOrderService.cs:52–63` → `RecalculateCostsAsync(int id)`
- **Gọi recalc tự động khi thêm Labor/Overhead**:
  - `Web/Controllers/ProductionOrdersController.cs:151–158` (Labor)
  - `ProductionOrdersController.cs:167–174` (Overhead)
- **Nút "Tính lại" trên giao diện**:
  - `Views/ProductionOrders/Details.cshtml` — action `Recalculate`.

---

## 7. Báo cáo & Bảng điều khiển (Statistics)

### 7.1. Service thống kê
`BLL/Services/StatisticsService.cs` cung cấp 4 báo cáo:

| Phương thức | Dòng | Báo cáo |
|---|---|---|
| `GetDashboardStatsAsync()` | 74 | KPI + cost rollup + monthly + top 5 |
| `GetProductCostBreakdownsAsync()` | 111 | Chi phí từng sản phẩm + biên lợi nhuận |
| `GetMonthlyCostsAsync()` | 149 | Chi phí 12 tháng gần nhất |
| `GetDepartmentCostsAsync()` | 45 | Chi phí theo bộ phận |

### 7.2. Biểu đồ (Chart.js 4)

- **Biểu đồ cột xếp chồng theo tháng** — `monthlyChart`
- **Biểu đồ tròn top 5 sản phẩm tốn kém** — `topProductsChart`
- **Biểu đồ tròn trạng thái sản xuất** — `statusChart`

Tất cả tại: `Views/Dashboard/Index.cshtml:193` (script section).

Đặc điểm nâng cao:
- **Plugin tự viết** vẽ chữ "TỔNG / 30 / đơn hàng" giữa donut — `centerTextPlugin` (dòng 215).
- Tooltip dạng dark-card có dòng "Tổng" cộng dồn các stack — dùng `Object.assign({}, tooltipStyle, …)` (dòng 280).
- Trục Y rút gọn `1.6B / 800M / …` — hàm `fmtCompact` (dòng 196).

### 7.3. Biểu đồ chi phí theo bộ phận
`Views/Dashboard/DepartmentCosts.cshtml` — biểu đồ cột ngang xếp chồng, hiển thị `Chi phí NVL / Nhân công / SXC` cho mỗi bộ phận.

---

## 8. Bảo mật & Phân quyền

### 8.1. Đăng nhập
- Mật khẩu băm bằng **BCrypt** — `BLL/Services/AuthService.cs:26, 32, 44, 45`.
- Cookie Authentication 8 giờ, sliding expiration — `Program.cs:35–43`.

### 8.2. Tạo Claims khi đăng nhập
`Web/Controllers/AccountController.cs:49–60`:

```csharp
var claims = new List<Claim> { /* NameIdentifier, Name, Email, Role, FullName */ };

var permissions = await _permissionService.GetCodesForRoleAsync(user.RoleId);
foreach (var p in permissions) claims.Add(new Claim("permission", p));
```

Claim `permission` được nhúng vào cookie → mỗi request đã có đủ thông tin quyền, không cần truy vấn DB.

### 8.3. Ma trận quyền có thể tuỳ chỉnh
- **Danh mục quyền** — `Web/Authorization/PermissionCatalog.cs`
  - 8 module × 4 hành động CRUD = 32 quyền + 9 quyền đặc biệt (Reports, Dashboard, Users, Permissions.Manage)
- **Service** — `Web/Authorization/PermissionService.cs`
- **Dynamic Policy Provider** — `Web/Authorization/PermissionPolicy.cs:34`
  - Cho phép viết `[Authorize(Policy = "Products.Edit")]` mà không cần đăng ký từng policy.
- **Controller** — `Web/Controllers/PermissionsController.cs` (chỉ Admin)
- **View ma trận** — `Views/Permissions/Index.cshtml`
  - Bảng checkbox: hàng = quyền (nhóm theo module), cột = vai trò.

### 8.4. Helper kiểm tra quyền trong view/controller
`Web/Authorization/PermissionPolicy.cs:61`:
```csharp
public static bool HasPermission(this ClaimsPrincipal user, string code)
{
    if (user.IsInRole("Admin")) return true;            // Admin bypass
    return user.HasClaim("permission", code);
}
```

---

## 9. Đa ngôn ngữ (i18n) — Tiếng Anh / Tiếng Việt

### 9.1. Kho từ điển
- `Web/Localization/AppText.cs` — 657 dòng, hơn 230 cặp khóa-giá trị × 2 ngôn ngữ.
- Hàm `AppText.Get(key)` trả chuỗi đúng theo culture hiện tại — dòng 649.

### 9.2. Cấu hình Request Localization
`Program.cs:58`:
```csharp
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = AppText.SupportedCultures.Select(c => new CultureInfo(c)).ToArray();
    options.DefaultRequestCulture = new RequestCulture("en");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    options.RequestCultureProviders.Insert(0, new CookieRequestCultureProvider());
});
```
- Middleware: `app.UseRequestLocalization();` (Program.cs:96).

### 9.3. Chuyển ngữ trong View
- Đăng ký namespace tại `Views/_ViewImports.cshtml`.
- Helper `Html.T("Nav.Dashboard")` trả chuỗi đã chọn ngôn ngữ — `Web/Localization/HtmlExtensions.cs`.

### 9.4. Nút đổi ngôn ngữ
- Trên thanh nav: dropdown 🌐 EN/VI — `Views/Shared/_Layout.cshtml`.
- Action ghi cookie 1 năm — `Web/Controllers/LanguageController.cs`.

### 9.5. Mẹo tránh lỗi mã hoá Unicode trong JS
`Views/Dashboard/Index.cshtml` — phải dùng `JavaScriptEncoder.UnsafeRelaxedJsonEscaping` để Razor không HTML-encode "Hoàn thành" thành `&#x1EDD;`:

```csharp
var jsonOpts = new JsonSerializerOptions {
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
};
```

---

## 10. Tài liệu & Triển khai

| File | Nội dung |
|---|---|
| `README.md` | Giới thiệu + chạy nhanh |
| `DEPLOY.md` | Cài đặt trên **Ubuntu Server** + **Windows** + systemd + nginx |
| `TOUR.md` | (Tài liệu bạn đang đọc) |
| `docker-compose.yml` | Azure SQL Edge (cho Apple Silicon) |
| `docker-compose.linux.yml` | SQL Server 2022 chính thức (Ubuntu / Windows) |
| `/Docs` (in-app) | Trang tài liệu trong ứng dụng — `Web/Controllers/DocsController.cs` + `Views/Docs/Index.cshtml` |

---

## 11. Kịch bản trình diễn cho thầy/cô

1. **Trang đăng nhập** — chuyển VI/EN bằng nút 🌐 để chứng minh đa ngôn ngữ.
2. **Đăng nhập admin / admin123** — vào Dashboard, chỉ ra:
   - 4 KPI cards
   - 4 thẻ chi phí với % của tổng
   - Biểu đồ cột chồng theo tháng (hover xem tooltip dark)
   - Biểu đồ donut Top 5 (có số tổng ở giữa)
   - Biểu đồ donut trạng thái (có số đơn ở giữa)
3. **Production → chọn 1 đơn hàng** → trang Details:
   - Thẻ "Phân tích chi phí" (Material + Labor + Overhead + Total)
   - Thêm 1 dòng Nhân công → nhấn Save → tổng tự cập nhật **(đây là đa hình `CalculateTotal()`)**
4. **Departments** → chuyển sang **Reports → Cost by Department** → biểu đồ ngang xếp chồng — chứng minh tính năng "chi phí theo từng bộ phận".
5. **Products → 1 sản phẩm** → BOM + thẻ "Tỷ suất lợi nhuận" để show **report**.
6. **Materials → Low Stock report** → bảng nguyên liệu sắp hết với badge cảnh báo.
7. **Filter trên Production / Materials** — đổi Department, đổi Status, kiểm tra phân trang giữ filter.
8. **User dropdown → Phân quyền vai trò** (chỉ Admin thấy) — bật/tắt checkbox để show **role-permission matrix**.
9. **Sign out → đăng nhập manager** — chỉ ra menu admin biến mất, chứng minh authorization hoạt động.
10. **Mở docker container** → `docker ps` để show **SQL Server chạy trên Docker**.
11. **Mở `dotnet ef migrations list`** → kể chuyện 3 migration đã chạy.
12. **Mở Visual Studio Code** → trỏ:
    - `CostEntry.cs` — abstract class + 2 override (đa hình)
    - `BaseService.cs` — generic + template method
    - `PermissionPolicy.cs:46` — dynamic policy provider

---

## 12. Tổng kết

> Ứng dụng đã đáp ứng đầy đủ yêu cầu môn học và bổ sung nhiều tính năng nâng cao: ma trận phân quyền cấu hình được, đa ngôn ngữ, biểu đồ tương tác, phân tích chi phí theo bộ phận, triển khai Docker.
>
> Tổng cộng: **3 project · ~165 file · ~6 000 dòng code · 11 controller · 41 quyền · 10 entity · 3 migration · 2 ngôn ngữ.**

Cảm ơn thầy/cô đã lắng nghe.
