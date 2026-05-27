using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ManufacturingCostManagement.BLL.DTOs;
using ManufacturingCostManagement.BLL.Interfaces;
using ManufacturingCostManagement.DAL.Entities;
using ManufacturingCostManagement.DAL.Repositories;

namespace ManufacturingCostManagement.BLL.Services
{
    public class UserService : BaseService<User>, IUserService
    {
        public UserService(IRepository<User> repo) : base(repo) { }

        protected override IQueryable<User> ApplySearch(IQueryable<User> query, string? keyword)
        {
            query = query.Include(u => u.Role);
            if (string.IsNullOrWhiteSpace(keyword)) return query.OrderBy(u => u.Username);
            keyword = keyword.ToLower();
            return query.Where(u =>
                u.Username.ToLower().Contains(keyword) ||
                u.Email.ToLower().Contains(keyword) ||
                (u.FullName != null && u.FullName.ToLower().Contains(keyword))
            ).OrderBy(u => u.Username);
        }

        public async Task<IEnumerable<User>> GetAllWithRoleAsync()
        {
            return await _repository.Query().Include(u => u.Role).ToListAsync();
        }

        public async Task<User?> GetWithRoleAsync(int id)
        {
            return await _repository.Query().Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<bool> ResetPasswordAsync(int userId, string newPassword)
        {
            var user = await _repository.GetByIdAsync(userId);
            if (user == null) return false;
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _repository.UpdateAsync(user);
            await _repository.SaveChangesAsync();
            return true;
        }

        public async Task<PagedResult<User>> FilterAsync(string? q, int? roleId, bool? isActive, int page, int pageSize)
        {
            var query = _repository.Query().Include(u => u.Role).AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var k = q.ToLower();
                query = query.Where(u => u.Username.ToLower().Contains(k)
                    || u.Email.ToLower().Contains(k)
                    || (u.FullName != null && u.FullName.ToLower().Contains(k)));
            }
            if (roleId.HasValue) query = query.Where(u => u.RoleId == roleId.Value);
            if (isActive.HasValue) query = query.Where(u => u.IsActive == isActive.Value);
            var total = await query.CountAsync();
            var items = await query.OrderBy(u => u.Username).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PagedResult<User> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
        }
    }

    public class RoleService : BaseService<Role>, IRoleService
    {
        public RoleService(IRepository<Role> repo) : base(repo) { }

        protected override IQueryable<Role> ApplySearch(IQueryable<Role> query, string? keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return query.OrderBy(r => r.Name);
            keyword = keyword.ToLower();
            return query.Where(r => r.Name.ToLower().Contains(keyword)).OrderBy(r => r.Name);
        }
    }

    public class SupplierService : BaseService<Supplier>, ISupplierService
    {
        public SupplierService(IRepository<Supplier> repo) : base(repo) { }

        protected override IQueryable<Supplier> ApplySearch(IQueryable<Supplier> query, string? keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return query.OrderBy(s => s.Name);
            keyword = keyword.ToLower();
            return query.Where(s =>
                s.Name.ToLower().Contains(keyword) ||
                (s.ContactPerson != null && s.ContactPerson.ToLower().Contains(keyword)) ||
                (s.Email != null && s.Email.ToLower().Contains(keyword))
            ).OrderBy(s => s.Name);
        }
    }

    public class DepartmentService : BaseService<Department>, IDepartmentService
    {
        public DepartmentService(IRepository<Department> repo) : base(repo) { }

        protected override IQueryable<Department> ApplySearch(IQueryable<Department> query, string? keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return query.OrderBy(d => d.Code);
            keyword = keyword.ToLower();
            return query.Where(d =>
                d.Code.ToLower().Contains(keyword) ||
                d.Name.ToLower().Contains(keyword) ||
                (d.Manager != null && d.Manager.ToLower().Contains(keyword))
            ).OrderBy(d => d.Code);
        }
    }

    public class MaterialService : BaseService<Material>, IMaterialService
    {
        public MaterialService(IRepository<Material> repo) : base(repo) { }

        protected override IQueryable<Material> ApplySearch(IQueryable<Material> query, string? keyword)
        {
            query = query.Include(m => m.Supplier);
            if (string.IsNullOrWhiteSpace(keyword)) return query.OrderBy(m => m.Code);
            keyword = keyword.ToLower();
            return query.Where(m =>
                m.Code.ToLower().Contains(keyword) ||
                m.Name.ToLower().Contains(keyword)
            ).OrderBy(m => m.Code);
        }

        public async Task<IEnumerable<Material>> GetLowStockAsync()
        {
            return await _repository.Query()
                .Where(m => m.StockQuantity <= m.ReorderLevel)
                .Include(m => m.Supplier)
                .ToListAsync();
        }

        public async Task<IEnumerable<Material>> GetWithSupplierAsync()
        {
            return await _repository.Query().Include(m => m.Supplier).ToListAsync();
        }

        public async Task<PagedResult<Material>> FilterAsync(string? q, int? supplierId, bool lowStock, int page, int pageSize)
        {
            var query = _repository.Query().Include(m => m.Supplier).AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var k = q.ToLower();
                query = query.Where(m => m.Code.ToLower().Contains(k) || m.Name.ToLower().Contains(k));
            }
            if (supplierId.HasValue) query = query.Where(m => m.SupplierId == supplierId.Value);
            if (lowStock) query = query.Where(m => m.StockQuantity <= m.ReorderLevel);
            var total = await query.CountAsync();
            var items = await query.OrderBy(m => m.Code).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PagedResult<Material> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
        }
    }

    public class ProductService : BaseService<Product>, IProductService
    {
        public ProductService(IRepository<Product> repo) : base(repo) { }

        protected override IQueryable<Product> ApplySearch(IQueryable<Product> query, string? keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return query.OrderBy(p => p.Code);
            keyword = keyword.ToLower();
            return query.Where(p =>
                p.Code.ToLower().Contains(keyword) ||
                p.Name.ToLower().Contains(keyword) ||
                (p.Category != null && p.Category.ToLower().Contains(keyword))
            ).OrderBy(p => p.Code);
        }

        public async Task<Product?> GetWithBomAsync(int id)
        {
            return await _repository.Query()
                .Include(p => p.BillOfMaterials)
                    .ThenInclude(b => b.Material)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<decimal> CalculateMaterialCostAsync(int productId)
        {
            var product = await GetWithBomAsync(productId);
            if (product == null) return 0;
            return product.BillOfMaterials.Sum(b => b.QuantityRequired * (b.Material?.UnitCost ?? 0));
        }

        public async Task<PagedResult<Product>> FilterAsync(string? q, string? category, int page, int pageSize)
        {
            var query = _repository.Query().AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var k = q.ToLower();
                query = query.Where(p => p.Code.ToLower().Contains(k) || p.Name.ToLower().Contains(k));
            }
            if (!string.IsNullOrWhiteSpace(category)) query = query.Where(p => p.Category == category);
            var total = await query.CountAsync();
            var items = await query.OrderBy(p => p.Code).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PagedResult<Product> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
        }

        public async Task<IEnumerable<string>> GetCategoriesAsync()
        {
            return await _repository.Query()
                .Where(p => p.Category != null && p.Category != "")
                .Select(p => p.Category!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
        }
    }

    public class BomService : BaseService<BillOfMaterial>, IBomService
    {
        public BomService(IRepository<BillOfMaterial> repo) : base(repo) { }

        protected override IQueryable<BillOfMaterial> ApplySearch(IQueryable<BillOfMaterial> query, string? keyword)
        {
            return query.Include(b => b.Product).Include(b => b.Material);
        }

        public async Task<IEnumerable<BillOfMaterial>> GetByProductAsync(int productId)
        {
            return await _repository.Query()
                .Where(b => b.ProductId == productId)
                .Include(b => b.Material)
                .ToListAsync();
        }
    }

    public class LaborCostService : BaseService<LaborCost>, ILaborCostService
    {
        public LaborCostService(IRepository<LaborCost> repo) : base(repo) { }

        protected override IQueryable<LaborCost> ApplySearch(IQueryable<LaborCost> query, string? keyword)
        {
            query = query.Include(l => l.ProductionOrder);
            if (string.IsNullOrWhiteSpace(keyword)) return query.OrderByDescending(l => l.IncurredAt);
            keyword = keyword.ToLower();
            return query.Where(l => l.WorkerName.ToLower().Contains(keyword))
                .OrderByDescending(l => l.IncurredAt);
        }
    }

    public class OverheadCostService : BaseService<OverheadCost>, IOverheadCostService
    {
        public OverheadCostService(IRepository<OverheadCost> repo) : base(repo) { }

        protected override IQueryable<OverheadCost> ApplySearch(IQueryable<OverheadCost> query, string? keyword)
        {
            query = query.Include(o => o.ProductionOrder);
            if (string.IsNullOrWhiteSpace(keyword)) return query.OrderByDescending(o => o.IncurredAt);
            keyword = keyword.ToLower();
            return query.Where(o => o.Category != null && o.Category.ToLower().Contains(keyword))
                .OrderByDescending(o => o.IncurredAt);
        }
    }
}
