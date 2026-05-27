using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ManufacturingCostManagement.DAL.Entities
{
    public class Material : BaseEntity
    {
        [Required, StringLength(100)]
        public string Code { get; set; } = string.Empty;

        [Required, StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [StringLength(50)]
        public string Unit { get; set; } = "pcs";

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitCost { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal StockQuantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ReorderLevel { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public int? SupplierId { get; set; }
        public virtual Supplier? Supplier { get; set; }

        public virtual ICollection<BillOfMaterial> BillOfMaterials { get; set; } = new List<BillOfMaterial>();
    }
}
