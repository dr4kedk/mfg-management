using System.ComponentModel.DataAnnotations;

namespace ManufacturingCostManagement.Web.ViewModels
{
    public class LoginViewModel
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }

    public class RegisterUserViewModel
    {
        [Required, StringLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [StringLength(150)]
        public string? FullName { get; set; }

        [Required, MinLength(6), DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public int RoleId { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required, DataType(DataType.Password)]
        public string OldPassword { get; set; } = string.Empty;

        [Required, MinLength(6), DataType(DataType.Password)]
        public string NewPassword { get; set; } = string.Empty;

        [Required, Compare(nameof(NewPassword)), DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
