using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;

namespace StarLedger.Api.Dtos.Organization;

public class OrganizationBalanceDto
{
    public Guid OrganizationId { get; set; }
    public long Balance { get; set; }
}