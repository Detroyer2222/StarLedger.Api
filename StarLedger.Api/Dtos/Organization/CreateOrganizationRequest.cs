using System.ComponentModel.DataAnnotations;

namespace StarLedger.Api.Dtos.Organization;

public class CreateOrganizationRequest
{
    [Required]
    public Guid CreatedBy { get; set; }
    
    [Required]
    [StringLength(100), MinLength(3)]
    public string OrganizationName { get; set; }
}