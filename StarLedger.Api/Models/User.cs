using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace StarLedger.Api.Models;

public sealed class User : IdentityUser<Guid>
{
    public string? StarCitizenHandle { get; set; }
    public long Balance { get; set; }
    
    [ForeignKey("Organization")]
    public Guid? OrganizationId { get; set; }

    public bool IsAdmin { get; set; }
    public bool IsOwner { get; set; }
    public Organization Organization { get; set; }
    public ICollection<UserResource> UserResources { get; set; }
}