using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ManufacturingCostManagement.DAL.Entities
{
    public class Permission : BaseEntity
    {
        [Required, StringLength(100)]
        public string Code { get; set; } = string.Empty;    // e.g. "Products.Edit"

        [Required, StringLength(50)]
        public string Module { get; set; } = string.Empty;  // e.g. "Products"

        [Required, StringLength(30)]
        public string Action { get; set; } = string.Empty;  // e.g. "Edit"

        [StringLength(200)]
        public string? Description { get; set; }

        public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }

    public class RolePermission : BaseEntity
    {
        public int RoleId { get; set; }
        public virtual Role? Role { get; set; }

        public int PermissionId { get; set; }
        public virtual Permission? Permission { get; set; }
    }
}
