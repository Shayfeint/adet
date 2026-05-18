using System.ComponentModel.DataAnnotations;

namespace ADET_Group_12.Models;

public static class SmartQRoles
{
    public const string Admin = "Admin";
    public const string Customer = "Customer";

    public static string ToDisplayName(string? role) => role switch
    {
        Admin => "Admin",
        Customer => "Customer",
        _ => "Guest"
    };
}

public sealed class LoginInput
{
    [Required]
    [StringLength(30, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;
}

public sealed class RegisterInput
{
    [Required]
    [StringLength(80)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [StringLength(30, MinimumLength = 3)]
    [RegularExpression("^[a-zA-Z0-9_.-]+$", ErrorMessage = "Use letters, numbers, dots, hyphens, or underscores.")]
    public string Username { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
