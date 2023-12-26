using System.ComponentModel.DataAnnotations;

namespace StarLedger.Api.Dtos.Organization;

public class AddUserToOrganizationRequest
{
    [Required]
    public Guid UserId { get; set; }
}