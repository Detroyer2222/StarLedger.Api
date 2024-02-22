namespace StarLedger.Api.Dtos.User;

public class FullUserDto
{
    public Guid UserId { get; set; }
    public Guid OrganizationId { get; set; }
    public string? StarCitizenHandle { get; set; }
    public required string Email { get; set; }
}