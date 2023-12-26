namespace StarLedger.Api.Dtos.User;

public class UserBalanceHistoryDto
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public DateOnly Timestamp { get; set; }
    public long Balance { get; set; }
}