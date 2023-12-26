namespace StarLedger.Api.Dtos.Organization;

public class OrganizationResourceDto
{
    public Guid OrganizationId { get; set; }
    public int ResourceId { get; set; }
    public float Quantity { get; set; }
}