using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarLedger.Api.Models;

public sealed class UserBalanceHistory
{
    [Key]
    public int Id { get; set; }

    [Required]
    [ForeignKey("User")]
    public Guid UserId { get; set; }
    public User User { get; set; }

    [Required]
    public DateOnly Timestamp { get; set; }

    [Required]
    public long Balance { get; set; }
}