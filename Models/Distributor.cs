using System.ComponentModel.DataAnnotations;

namespace SoulSignal.Models;

// A simulated distribution source; provisioned accounts are linked to one source.
public class Distributor
{
    public int Id { get; set; }
    [Required, MaxLength(120)] public string Name { get; set; } = string.Empty;
    [Required, MaxLength(30)] public string Code { get; set; } = string.Empty;
    [MaxLength(500)] public string Description { get; set; } = string.Empty;
    public bool IsSimulation { get; set; } = true;
}
