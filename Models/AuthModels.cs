using System.ComponentModel.DataAnnotations;

namespace ADET_Group_12.Models;

public static class SmartQRoles
{
    public const string Customer = "Customer";
    public const string ServiceProvider = "ServiceProvider";

    public static string ToDisplayName(string? role) => role switch
    {
        ServiceProvider => "Service provider",
        Customer => "Customer",
        _ => "Guest"
    };

    public static bool IsSupported(string? role)
    {
        return role is Customer or ServiceProvider;
    }
}

public sealed class LoginInput
{
    [StringLength(80)]
    public string? DisplayName { get; set; }

    [Required]
    public string Role { get; set; } = SmartQRoles.Customer;

    [StringLength(30)]
    public string? AccessCode { get; set; }
}
