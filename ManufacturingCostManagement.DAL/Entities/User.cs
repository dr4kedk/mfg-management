using System.ComponentModel.DataAnnotations;

namespace ManufacturingCostManagement.DAL.Entities
{
    public class User : BaseEntity
    {
        [Required, StringLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required, StringLength(100), EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [StringLength(150)]
        public string? FullName { get; set; }

        public bool IsActive { get; set; } = true;

        public int RoleId { get; set; }
        public virtual Role? Role { get; set; }
    }
}
