namespace StarLedger.Api.Dtos.UserResource;

public class UserResourceDto
{
    public Guid UserId { get; set; }
    public int ResourceId { get; set; }
    public float Quantity { get; set; }
}