namespace StarLedger.Api.Dtos.UserResource;

public class ResourceQuantityHistoryDto
{
    public Guid UserId { get; set; }
    public int ResourceId { get; set; }
    public float Quantity { get; set; }
    public DateOnly Timestamp { get; set; }
}