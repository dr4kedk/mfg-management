using System.ComponentModel.DataAnnotations.Schema;

namespace ManufacturingCostManagement.DAL.Entities
{
    public class BillOfMaterial : BaseEntity
    {
        public int ProductId { get; set; }
        public virtual Product? Product { get; set; }

        public int MaterialId { get; set; }
        public virtual Material? Material { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal QuantityRequired { get; set; }
    }
}
