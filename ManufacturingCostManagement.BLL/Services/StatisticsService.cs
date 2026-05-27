using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ManufacturingCostManagement.BLL.DTOs;
using ManufacturingCostManagement.BLL.Interfaces;
using ManufacturingCostManagement.DAL.Entities;
using ManufacturingCostManagement.DAL.Repositories;

namespace ManufacturingCostManagement.BLL.Services
{
    public class StatisticsService : IStatisticsService
    {
        private readonly IRepository<Product> _productRepo;
        private readonly IRepository<Material> _materialRepo;
        private readonly IRepository<Supplier> _supplierRepo;
        private readonly IRepository<ProductionOrder> _orderRepo;
        private readonly IRepository<Department> _deptRepo;
        private readonly IRepository<LaborCost> _laborRepo;
        private readonly IRepository<OverheadCost> _overheadRepo;
        private readonly IProductService _productService;

        public StatisticsService(
            IRepository<Product> productRepo,
            IRepository<Material> materialRepo,
            IRepository<Supplier> supplierRepo,
            IRepository<ProductionOrder> orderRepo,
            IRepository<Department> deptRepo,
            IRepository<LaborCost> laborRepo,
            IRepository<OverheadCost> overheadRepo,
            IProductService productService)
        {
            _productRepo = productRepo;
            _materialRepo = materialRepo;
            _supplierRepo = supplierRepo;
            _orderRepo = orderRepo;
            _deptRepo = deptRepo;
            _laborRepo = laborRepo;
            _overheadRepo = overheadRepo;
            _productService = productService;
        }

        public async Task<IEnumerable<DepartmentCostDto>> GetDepartmentCostsAsync()
        {
            var departments = await _deptRepo.Query().ToListAsync();
            var orders = await _orderRepo.Query().ToListAsync();
            var labors = await _laborRepo.Query().ToListAsync();
            var overheads = await _overheadRepo.Query().ToListAsync();

            var result = new List<DepartmentCostDto>();
            foreach (var d in departments)
            {
                var deptOrders = orders.Where(o => o.DepartmentId == d.Id).ToList();
                var deptLabor = labors.Where(l => l.DepartmentId == d.Id).Sum(l => l.CalculateTotal());
                var deptOverhead = overheads.Where(o => o.DepartmentId == d.Id).Sum(o => o.CalculateTotal());
                var deptMaterial = deptOrders.Sum(o => o.TotalMaterialCost);

                result.Add(new DepartmentCostDto
                {
                    DepartmentCode = d.Code,
                    DepartmentName = d.Name,
                    MaterialCost = deptMaterial,
                    LaborCost = deptLabor,
                    OverheadCost = deptOverhead,
                    TotalCost = deptMaterial + deptLabor + deptOverhead,
                    OrderCount = deptOrders.Count
                });
            }
            return result.OrderByDescending(r => r.TotalCost);
        }

        public async Task<DashboardStatsDto> GetDashboardStatsAsync()
        {
            var orders = await _orderRepo.Query().Include(o => o.Product).ToListAsync();
            var materials = await _materialRepo.Query().ToListAsync();

            var stats = new DashboardStatsDto
            {
                TotalProducts = await _productRepo.CountAsync(),
                TotalMaterials = materials.Count,
                TotalSuppliers = await _supplierRepo.CountAsync(),
                TotalProductionOrders = orders.Count,
                PendingOrders = orders.Count(o => o.Status == ProductionStatus.Pending),
                InProgressOrders = orders.Count(o => o.Status == ProductionStatus.InProgress),
                CompletedOrders = orders.Count(o => o.Status == ProductionStatus.Completed),
                TotalMaterialCost = orders.Sum(o => o.TotalMaterialCost),
                TotalLaborCost = orders.Sum(o => o.TotalLaborCost),
                TotalOverheadCost = orders.Sum(o => o.TotalOverheadCost),
                TotalCost = orders.Sum(o => o.TotalCost),
                LowStockMaterials = materials.Count(m => m.StockQuantity <= m.ReorderLevel),
                MonthlyCosts = (await GetMonthlyCostsAsync()).ToList(),
                TopCostProducts = orders
                    .Where(o => o.Product != null)
                    .GroupBy(o => o.Product!.Name)
                    .Select(g => new TopProductDto
                    {
                        ProductName = g.Key,
                        TotalCost = g.Sum(o => o.TotalCost),
                        OrderCount = g.Count()
                    })
                    .OrderByDescending(t => t.TotalCost)
                    .Take(5)
                    .ToList()
            };

            return stats;
        }

        public async Task<IEnumerable<ProductCostBreakdownDto>> GetProductCostBreakdownsAsync()
        {
            var products = await _productRepo.Query().ToListAsync();
            var orders = await _orderRepo.Query().ToListAsync();
            var result = new List<ProductCostBreakdownDto>();

            foreach (var product in products)
            {
                var materialCost = await _productService.CalculateMaterialCostAsync(product.Id);
                var productOrders = orders.Where(o => o.ProductId == product.Id && o.Quantity > 0).ToList();

                decimal avgLabor = 0, avgOverhead = 0;
                if (productOrders.Any())
                {
                    avgLabor = productOrders.Average(o => o.TotalLaborCost / o.Quantity);
                    avgOverhead = productOrders.Average(o => o.TotalOverheadCost / o.Quantity);
                }

                var totalCost = materialCost + avgLabor + avgOverhead;
                var margin = product.SellingPrice - totalCost;
                var marginPercent = product.SellingPrice > 0 ? (margin / product.SellingPrice) * 100 : 0;

                result.Add(new ProductCostBreakdownDto
                {
                    ProductName = product.Name,
                    ProductCode = product.Code,
                    MaterialCostPerUnit = materialCost,
                    AverageLaborCostPerUnit = avgLabor,
                    AverageOverheadCostPerUnit = avgOverhead,
                    TotalCostPerUnit = totalCost,
                    SellingPrice = product.SellingPrice,
                    ProfitMargin = margin,
                    ProfitMarginPercent = marginPercent
                });
            }
            return result.OrderByDescending(r => r.ProfitMarginPercent);
        }

        public async Task<IEnumerable<MonthlyCostDto>> GetMonthlyCostsAsync(int monthsBack = 12)
        {
            var startDate = DateTime.UtcNow.AddMonths(-monthsBack);
            var orders = await _orderRepo.Query()
                .Where(o => o.StartDate >= startDate)
                .ToListAsync();

            return orders
                .GroupBy(o => new { o.StartDate.Year, o.StartDate.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g => new MonthlyCostDto
                {
                    Month = $"{g.Key.Year}-{g.Key.Month:D2}",
                    MaterialCost = g.Sum(o => o.TotalMaterialCost),
                    LaborCost = g.Sum(o => o.TotalLaborCost),
                    OverheadCost = g.Sum(o => o.TotalOverheadCost),
                    TotalCost = g.Sum(o => o.TotalCost)
                })
                .ToList();
        }
    }
}
