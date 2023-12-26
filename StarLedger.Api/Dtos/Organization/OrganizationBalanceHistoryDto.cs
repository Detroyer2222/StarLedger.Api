namespace StarLedger.Api.Dtos.Organization;

public class OrganizationBalanceHistoryDto
{
    public Guid OrganizationId { get; set; }
    public DateOnly Timestamp { get; set; }
    public long Balance { get; set; }
}