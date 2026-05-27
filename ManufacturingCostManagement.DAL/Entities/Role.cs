using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ManufacturingCostManagement.DAL.Entities
{
    public class Role : BaseEntity
    {
        [Required, StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Description { get; set; }

        public virtual ICollection<User> Users { get; set; } = new List<User>();
        public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}
