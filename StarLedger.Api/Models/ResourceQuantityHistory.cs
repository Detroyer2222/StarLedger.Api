using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarLedger.Api.Models;

public sealed class ResourceQuantityHistory
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [ForeignKey("User")]
    public Guid UserId { get; set; }
    public User User { get; set; }

    [Required]
    [ForeignKey("Resource")]
    public int ResourceId { get; set; }
    public Resource Resource { get; set; }

    [Required]
    public DateOnly Timestamp { get; set; }

    [Required]
    public float Quantity { get; set; }
}