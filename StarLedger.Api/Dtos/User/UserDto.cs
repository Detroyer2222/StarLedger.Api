namespace StarLedger.Api.Dtos.User;

public class UserDto
{
    public Guid UserId { get; set; }
    public required string Email { get; set; }
    public string? StarCitizenHandle { get; set; }
}