using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ManufacturingCostManagement.DAL.Entities
{
    public class Department : BaseEntity
    {
        [Required, StringLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required, StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [StringLength(150)]
        public string? Manager { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public virtual ICollection<ProductionOrder> ProductionOrders { get; set; } = new List<ProductionOrder>();
        public virtual ICollection<LaborCost> LaborCosts { get; set; } = new List<LaborCost>();
        public virtual ICollection<OverheadCost> OverheadCosts { get; set; } = new List<OverheadCost>();
    }
}
