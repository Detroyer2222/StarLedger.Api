namespace StarLedger.Api.Dtos.Organization;

public class OrganizationDto
{
    public Guid OrganizationId { get; set; }
    public required string Name { get; set; }
}