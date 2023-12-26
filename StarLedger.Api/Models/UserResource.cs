using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarLedger.Api.Models;

public sealed class UserResource
{
    [Key]
    public int UserResourceId { get; set; }

    [Required]
    [ForeignKey("User")]
    public Guid UserId { get; set; }
    public User User { get; set; }

    [Required]
    [ForeignKey("Resource")]
    public int ResourceId { get; set; }
    public Resource Resource { get; set; }

    [Required]
    public float Quantity { get; set; }
}