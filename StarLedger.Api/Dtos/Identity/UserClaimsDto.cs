using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace StarLedger.Api.Dtos.Identity;

public class UserClaimsDto
{
    [Required]
    public required Dictionary<string, string> Claims { get; set; }
}