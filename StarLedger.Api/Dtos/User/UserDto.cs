namespace StarLedger.Api.Dtos.User;

public class UserDto
{
    public Guid UserId { get; set; }
    public required string UserName { get; set; }
}