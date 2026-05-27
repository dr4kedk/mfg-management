using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ManufacturingCostManagement.DAL.Entities
{
    public abstract class CostEntry : BaseEntity
    {
        public int ProductionOrderId { get; set; }
        public virtual ProductionOrder? ProductionOrder { get; set; }

        public int? DepartmentId { get; set; }
        public virtual Department? Department { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public DateTime IncurredAt { get; set; } = DateTime.UtcNow;

        [StringLength(500)]
        public string? Description { get; set; }

        public abstract string CostType { get; }
        public abstract decimal CalculateTotal();
    }

    public class LaborCost : CostEntry
    {
        [StringLength(150)]
        public string WorkerName { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal HoursWorked { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal HourlyRate { get; set; }

        public override string CostType => "Labor";

        public override decimal CalculateTotal() => HoursWorked * HourlyRate;
    }

    public enum OverheadType
    {
        Electricity = 0,
        Rent = 1,
        Equipment = 2,
        Maintenance = 3,
        Other = 4
    }

    public class OverheadCost : CostEntry
    {
        public OverheadType Type { get; set; } = OverheadType.Other;

        [StringLength(150)]
        public string? Category { get; set; }

        public override string CostType => $"Overhead-{Type}";

        public override decimal CalculateTotal() => Amount;
    }
}
