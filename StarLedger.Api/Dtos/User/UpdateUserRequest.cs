using System.ComponentModel.DataAnnotations;

namespace StarLedger.Api.Dtos.User;

public class UpdateUserRequest
{
    [Required]
    public required string DisplayName { get; set; }
}