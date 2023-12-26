namespace StarLedger.Api.Dtos.User;

public class FullUserDto
{
    public Guid UserId { get; set; }
    public Guid OrganizationId { get; set; }
    public required string UserName { get; set; }
    
    public required string Email { get; set; }
}