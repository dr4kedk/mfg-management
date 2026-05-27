using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ManufacturingCostManagement.BLL.DTOs;
using ManufacturingCostManagement.BLL.Interfaces;
using ManufacturingCostManagement.DAL.Entities;
using ManufacturingCostManagement.DAL.Repositories;

namespace ManufacturingCostManagement.BLL.Services
{
    public class ProductionOrderService : BaseService<ProductionOrder>, IProductionOrderService
    {
        private readonly IProductService _productService;

        public ProductionOrderService(IRepository<ProductionOrder> repo, IProductService productService)
            : base(repo)
        {
            _productService = productService;
        }

        protected override IQueryable<ProductionOrder> ApplySearch(IQueryable<ProductionOrder> query, string? keyword)
        {
            query = query.Include(p => p.Product).Include(p => p.Department);
            if (string.IsNullOrWhiteSpace(keyword)) return query.OrderByDescending(p => p.StartDate);
            keyword = keyword.ToLower();
            return query.Where(p =>
                p.OrderCode.ToLower().Contains(keyword) ||
                (p.Product != null && p.Product.Name.ToLower().Contains(keyword)) ||
                (p.Department != null && p.Department.Name.ToLower().Contains(keyword))
            ).OrderByDescending(p => p.StartDate);
        }

        public async Task<ProductionOrder?> GetWithDetailsAsync(int id)
        {
            return await _repository.Query()
                .Include(p => p.Product)
                .Include(p => p.Department)
                .Include(p => p.LaborCosts).ThenInclude(l => l.Department)
                .Include(p => p.OverheadCosts).ThenInclude(o => o.Department)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public override async Task<ProductionOrder> CreateAsync(ProductionOrder entity)
        {
            var materialCostPerUnit = await _productService.CalculateMaterialCostAsync(entity.ProductId);
            entity.TotalMaterialCost = materialCostPerUnit * entity.Quantity;
            entity.TotalCost = entity.TotalMaterialCost + entity.TotalLaborCost + entity.TotalOverheadCost;
            return await base.CreateAsync(entity);
        }

        public async Task<ProductionOrder> RecalculateCostsAsync(int id)
        {
            var order = await GetWithDetailsAsync(id)
                ?? throw new InvalidOperationException("Order not found");

            var materialCostPerUnit = await _productService.CalculateMaterialCostAsync(order.ProductId);
            order.TotalMaterialCost = materialCostPerUnit * order.Quantity;
            order.TotalLaborCost = order.LaborCosts.Sum(l => l.CalculateTotal());
            order.TotalOverheadCost = order.OverheadCosts.Sum(o => o.CalculateTotal());
            order.TotalCost = order.TotalMaterialCost + order.TotalLaborCost + order.TotalOverheadCost;

            await _repository.UpdateAsync(order);
            await _repository.SaveChangesAsync();
            return order;
        }

        public async Task<bool> CompleteOrderAsync(int id)
        {
            var order = await _repository.GetByIdAsync(id);
            if (order == null) return false;
            order.Status = ProductionStatus.Completed;
            order.EndDate = DateTime.UtcNow;
            await _repository.UpdateAsync(order);
            await _repository.SaveChangesAsync();
            return true;
        }

        public async Task<PagedResult<ProductionOrder>> FilterAsync(string? q, ProductionStatus? status, int? departmentId, int? productId, int page, int pageSize)
        {
            var query = _repository.Query()
                .Include(p => p.Product)
                .Include(p => p.Department)
                .AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var k = q.ToLower();
                query = query.Where(p => p.OrderCode.ToLower().Contains(k)
                    || (p.Product != null && p.Product.Name.ToLower().Contains(k)));
            }
            if (status.HasValue) query = query.Where(p => p.Status == status.Value);
            if (departmentId.HasValue) query = query.Where(p => p.DepartmentId == departmentId.Value);
            if (productId.HasValue) query = query.Where(p => p.ProductId == productId.Value);
            var total = await query.CountAsync();
            var items = await query.OrderByDescending(p => p.StartDate).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PagedResult<ProductionOrder> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
        }
    }
}
