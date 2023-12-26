using StarLedger.Api.Dtos.User;

namespace StarLedger.Api.Dtos.Organization;

public class OrganizationWithUserDto
{
    public Guid OrganizationId { get; set; }
    public required List<UserDto> Users { get; set; }
}