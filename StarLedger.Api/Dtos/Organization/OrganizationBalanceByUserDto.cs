using System.ComponentModel.DataAnnotations;

namespace StarLedger.Api.Dtos.Organization;

public class OrganizationBalanceByUserDto
{
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }
    public string? UserName { get; set; }
    public long Balance { get; set; }
}