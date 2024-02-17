using Microsoft.AspNetCore.Identity;

namespace StarLedger.Api;

public static class SecurityConstants
{
    public const string OrganizationClaimType = "Organization";
    
    public const string OrganizationOwnerPolicy = "OrganizationOwner";
    public const string OrganizationOwnerRole = "Owner";
    
    public const string OrganizationAdminPolicy = "OrganizationAdmin";
    public const string OrganizationAdminRole = "Admin";
    
    public const string DeveloperPolicy = "Developer";
    public const string DeveloperRole = "Developer";

    public static async Task ConfigureRoles(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        if (!await roleManager.RoleExistsAsync(OrganizationOwnerRole))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>(OrganizationOwnerRole));
        }

        if (!await roleManager.RoleExistsAsync(OrganizationAdminRole))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>(OrganizationAdminRole));
        }

        if (!await roleManager.RoleExistsAsync(DeveloperRole))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>(DeveloperRole));
        }
    }   
}