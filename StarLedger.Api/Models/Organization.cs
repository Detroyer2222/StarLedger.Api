using System.ComponentModel.DataAnnotations;

namespace StarLedger.Api.Models;

public sealed class Organization
{
    [Key] 
    public Guid OrganizationId { get; set; }
    
    [Required] 
    [MaxLength(100)] 
    public string Name { get; set; }

    public ICollection<User> Users { get; set; }
}