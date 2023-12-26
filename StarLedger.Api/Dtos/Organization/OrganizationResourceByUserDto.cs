namespace StarLedger.Api.Dtos.Organization;

public class OrganizationResourceByUserDto
{
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }
    public string? UserName { get; set; }
    public int ResourceId { get; set; }
    public float Quantity { get; set; }
}