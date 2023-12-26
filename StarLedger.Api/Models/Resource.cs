using System.ComponentModel.DataAnnotations;

namespace StarLedger.Api.Models;

public sealed class Resource
{
    [Key]
    public int ResourceId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; }

    [Required]
    [MaxLength(4)]
    public string Code { get; set; }

    [MaxLength(50)]
    public string Type { get; set; }

    [Required]
    public double PriceBuy { get; set; }

    [Required]
    public double PriceSell { get; set; }

    public ICollection<UserResource> UserResources { get; set; }

    public DateTime LastUpdated { get; set; }
}