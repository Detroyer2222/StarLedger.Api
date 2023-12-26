using System.ComponentModel.DataAnnotations;

namespace StarLedger.Api.Dtos.User;

public class UpdateUserBalanceRequestDto
{
    [Required]
    public long UpdateAmount { get; set; }
    [Required]
    public UpdateType UpdateType { get; set; }
}