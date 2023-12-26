using System.ComponentModel.DataAnnotations;

namespace StarLedger.Api.Dtos.UserResource;

public class UpdateUserResourceRequestDto
{
    [Required]
    public int ResourceId { get; set; }
    [Required]
    public float Quantity { get; set; }
    [Required]
    public UpdateType UpdateType { get; set; }
}