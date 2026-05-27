using System.Collections.Generic;

namespace ManufacturingCostManagement.BLL.DTOs
{
    public class DashboardStatsDto
    {
        public int TotalProducts { get; set; }
        public int TotalMaterials { get; set; }
        public int TotalSuppliers { get; set; }
        public int TotalProductionOrders { get; set; }
        public int PendingOrders { get; set; }
        public int InProgressOrders { get; set; }
        public int CompletedOrders { get; set; }
        public decimal TotalMaterialCost { get; set; }
        public decimal TotalLaborCost { get; set; }
        public decimal TotalOverheadCost { get; set; }
        public decimal TotalCost { get; set; }
        public int LowStockMaterials { get; set; }
        public List<MonthlyCostDto> MonthlyCosts { get; set; } = new();
        public List<TopProductDto> TopCostProducts { get; set; } = new();
    }

    public class MonthlyCostDto
    {
        public string Month { get; set; } = string.Empty;
        public decimal MaterialCost { get; set; }
        public decimal LaborCost { get; set; }
        public decimal OverheadCost { get; set; }
        public decimal TotalCost { get; set; }
    }

    public class TopProductDto
    {
        public string ProductName { get; set; } = string.Empty;
        public decimal TotalCost { get; set; }
        public int OrderCount { get; set; }
    }

    public class DepartmentCostDto
    {
        public string DepartmentName { get; set; } = string.Empty;
        public string DepartmentCode { get; set; } = string.Empty;
        public decimal MaterialCost { get; set; }
        public decimal LaborCost { get; set; }
        public decimal OverheadCost { get; set; }
        public decimal TotalCost { get; set; }
        public int OrderCount { get; set; }
    }

    public class ProductCostBreakdownDto
    {
        public string ProductName { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public decimal MaterialCostPerUnit { get; set; }
        public decimal AverageLaborCostPerUnit { get; set; }
        public decimal AverageOverheadCostPerUnit { get; set; }
        public decimal TotalCostPerUnit { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal ProfitMargin { get; set; }
        public decimal ProfitMarginPercent { get; set; }
    }
}
