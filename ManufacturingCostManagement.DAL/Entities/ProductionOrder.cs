using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ManufacturingCostManagement.DAL.Entities
{
    public enum ProductionStatus
    {
        Pending = 0,
        InProgress = 1,
        Completed = 2,
        Cancelled = 3
    }

    public class ProductionOrder : BaseEntity
    {
        [Required, StringLength(100)]
        public string OrderCode { get; set; } = string.Empty;

        public int ProductId { get; set; }
        public virtual Product? Product { get; set; }

        public int? DepartmentId { get; set; }
        public virtual Department? Department { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Quantity { get; set; }

        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime? EndDate { get; set; }

        public ProductionStatus Status { get; set; } = ProductionStatus.Pending;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalMaterialCost { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalLaborCost { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalOverheadCost { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalCost { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public virtual ICollection<LaborCost> LaborCosts { get; set; } = new List<LaborCost>();
        public virtual ICollection<OverheadCost> OverheadCosts { get; set; } = new List<OverheadCost>();
    }
}
