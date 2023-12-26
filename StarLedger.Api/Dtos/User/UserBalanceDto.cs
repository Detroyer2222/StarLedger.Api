namespace StarLedger.Api.Dtos.User;

public class UserBalanceDto
{
    public Guid UserId { get; set; }
    public long Balance { get; set; }
}