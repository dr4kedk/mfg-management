using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ManufacturingCostManagement.DAL.Entities
{
    public class Supplier : BaseEntity
    {
        [Required, StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Phone { get; set; }

        [StringLength(100), EmailAddress]
        public string? Email { get; set; }

        [StringLength(250)]
        public string? Address { get; set; }

        [StringLength(100)]
        public string? ContactPerson { get; set; }

        public virtual ICollection<Material> Materials { get; set; } = new List<Material>();
    }
}
