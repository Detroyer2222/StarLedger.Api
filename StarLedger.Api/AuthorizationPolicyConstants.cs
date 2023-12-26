namespace StarLedger.Api;

public static class AuthorizationPolicyConstants
{
    public const string OrganizationClaimType = "Organization";
    
    public const string OrganizationOwnerPolicy = "OrganizationOwner";
    public const string OrganizationOwnerClaimType = "OrganizationOwner";
    
    public const string OrganizationAdminPolicy = "OrganizationAdmin";
    public const string OrganizationAdminClaimType = "OrganizationAdmin";
    
    public const string DeveloperPolicy = "Developer";
    public const string DeveloperClaimType = "Developer";
}