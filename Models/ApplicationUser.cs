using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace SoulSignal.Models;

public class ApplicationUser : IdentityUser
{
    [Required, MaxLength(80)]
    public string DisplayName { get; set; } = string.Empty;
    public int? DistributorId { get; set; }
    public Distributor? Distributor { get; set; }
}

public static class AppRoles
{
    public const string Listener = "Listener";
    public const string Admin = "Admin";
    public const string Distributor = "Distributor";
}
