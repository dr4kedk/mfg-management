using System.Collections.Generic;
using System.Threading.Tasks;
using ManufacturingCostManagement.BLL.DTOs;
using ManufacturingCostManagement.DAL.Entities;

namespace ManufacturingCostManagement.BLL.Interfaces
{
    public interface IAuthService
    {
        Task<User?> AuthenticateAsync(string username, string password);
        Task<User> RegisterAsync(User user, string password, int roleId);
        Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword);
    }

    public interface IUserService : IBaseService<User>
    {
        Task<IEnumerable<User>> GetAllWithRoleAsync();
        Task<User?> GetWithRoleAsync(int id);
        Task<bool> ResetPasswordAsync(int userId, string newPassword);
        Task<PagedResult<User>> FilterAsync(string? q, int? roleId, bool? isActive, int page, int pageSize);
    }

    public interface IRoleService : IBaseService<Role> { }

    public interface ISupplierService : IBaseService<Supplier> { }

    public interface IDepartmentService : IBaseService<Department> { }

    public interface IMaterialService : IBaseService<Material>
    {
        Task<IEnumerable<Material>> GetLowStockAsync();
        Task<IEnumerable<Material>> GetWithSupplierAsync();
        Task<PagedResult<Material>> FilterAsync(string? q, int? supplierId, bool lowStock, int page, int pageSize);
    }

    public interface IProductService : IBaseService<Product>
    {
        Task<Product?> GetWithBomAsync(int id);
        Task<decimal> CalculateMaterialCostAsync(int productId);
        Task<PagedResult<Product>> FilterAsync(string? q, string? category, int page, int pageSize);
        Task<IEnumerable<string>> GetCategoriesAsync();
    }

    public interface IBomService : IBaseService<BillOfMaterial>
    {
        Task<IEnumerable<BillOfMaterial>> GetByProductAsync(int productId);
    }

    public interface IProductionOrderService : IBaseService<ProductionOrder>
    {
        Task<ProductionOrder?> GetWithDetailsAsync(int id);
        Task<ProductionOrder> RecalculateCostsAsync(int id);
        Task<bool> CompleteOrderAsync(int id);
        Task<PagedResult<ProductionOrder>> FilterAsync(string? q, ProductionStatus? status, int? departmentId, int? productId, int page, int pageSize);
    }

    public interface ILaborCostService : IBaseService<LaborCost> { }
    public interface IOverheadCostService : IBaseService<OverheadCost> { }

    public interface IStatisticsService
    {
        Task<DashboardStatsDto> GetDashboardStatsAsync();
        Task<IEnumerable<ProductCostBreakdownDto>> GetProductCostBreakdownsAsync();
        Task<IEnumerable<MonthlyCostDto>> GetMonthlyCostsAsync(int monthsBack = 12);
        Task<IEnumerable<DepartmentCostDto>> GetDepartmentCostsAsync();
    }
}
