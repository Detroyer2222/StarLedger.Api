namespace StarLedger.Api.Dtos.Organization;

public class OrganizationResourceQuantityHistory
{
    public Guid OrganizationId { get; set; }
    public int ResourceId { get; set; }
    public float Quantity { get; set; }
    public DateOnly Timestamp { get; set; }
}