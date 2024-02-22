using StarLedger.Api.EndpointHandlers;
using StarLedger.Api.Models;

namespace StarLedger.Api.Extensions;

public static class EndpointRouteBuilderExtensions
{
    public static void RegisterIdentityEndpoints(this IEndpointRouteBuilder endpointRouteBuilder)
    {
        var identityEndpoints = endpointRouteBuilder.MapGroup("/identity")
            .WithParameterValidation()
            .WithOpenApi()
            .WithTags("Identity");

        identityEndpoints.MapIdentityApi<User>();
    }

    public static void RegisterUserEndpoints(this IEndpointRouteBuilder endpointRouteBuilder)
    {
        var userEndpoints = endpointRouteBuilder.MapGroup("/users")
            .WithParameterValidation()
            .RequireAuthorization()
            .WithOpenApi()
            .WithTags("User");
        var userWithGuidEndpoints = userEndpoints.MapGroup("/{userId:Guid}");

        userEndpoints.MapGet("", UserHandlers.GetUsersAsync)
            .WithName("GetUsers")
            .WithSummary("Gets all users.")
            .WithDescription("This endpoint returns all users.");
        userEndpoints.MapGet("/claims", UserHandlers.GetUserClaimsAsync)
            .WithName("GetUserClaims")
            .WithSummary("Gets claims for a specific user.")
            .WithDescription("This endpoint returns the information of the user associated to the Bearer Claims.");
        userWithGuidEndpoints.MapGet("", UserHandlers.GetUserAsync)
            .WithName("GetUser")
            .WithSummary("Gets a specific user information.")
            .WithDescription("This endpoint returns the user information associated to the specified userId.");
        userWithGuidEndpoints.MapPost("", UserHandlers.UpdateUserAsync)
            .WithName("UpdateUser")
            .WithSummary("Updates a specific user.")
            .WithDescription("This endpoint updates the user associated to the specified userId.");
        userWithGuidEndpoints.MapGet("/balance", UserHandlers.GetUserBalanceAsync)
            .WithName("GetUserBalance")
            .WithSummary("Gets the balance for a specific user.")
            .WithDescription("This endpoint returns the current balance of the user associated to the specified userId.");
        userWithGuidEndpoints.MapGet("/balance/history", UserHandlers.GetUserBalanceHistoryAsync)
            .WithName("GetUserBalanceHistory")
            .WithSummary("Gets the balance history for a specific user.")
            .WithDescription("This endpoint returns the balance history of the user associated to the specified userId.");
        userWithGuidEndpoints.MapPost("/balance/history", UserHandlers.UpdateUserBalanceAsync)
            .WithName("UpdateUserBalance")
            .WithSummary("Updates balance for a specific user.")
            .WithDescription("This endpoint updates and stores the current balance of the user associated to the specified userId.");
        userWithGuidEndpoints.MapDelete("", UserHandlers.DeleteUserAsync)
            .WithName("DeleteUser")
            .WithSummary("Deletes a specific user.")
            .WithDescription("This endpoint deletes the user associated to the specified userId.");
    }

    public static void RegisterUserResourceEndpoints(this IEndpointRouteBuilder endpointRouteBuilder)
    {
        var userResourceEndpoints = endpointRouteBuilder.MapGroup("/userResources")
            .WithParameterValidation()
            .RequireAuthorization()
            .WithOpenApi()
            .WithTags("User Resource");
        var userResourceWithUserGuidEndpoints = userResourceEndpoints.MapGroup("/{userId:Guid}");
        var userResourceWithUserGuidAndResourceIdEndpoints =
            userResourceWithUserGuidEndpoints.MapGroup("/{resourceId:int}");

        userResourceWithUserGuidEndpoints.MapGet("", UserResourceHandler.GetUserResourcesAsync)
            .WithName("GetUserResources")
            .WithSummary("Gets resources for a specific user.")
            .WithDescription("This endpoint returns all resources associated with the specified userId.");
        userResourceWithUserGuidAndResourceIdEndpoints.MapGet("", UserResourceHandler.GetUserResourceAsync)
            .WithName("GetUserResourceById")
            .WithDescription(
                "This endpoint returns a specific resource associated with the specified userId and resourceId.");
        userResourceWithUserGuidEndpoints.MapGet("/history", UserResourceHandler.GetUserResourceQuantityHistoryAsync)
            .WithName("GetUserResourceHistory")
            .WithSummary("Gets the history of resource quantities for a specific user.")
            .WithDescription("This endpoint returns the history of changes in resource quantities for a given user.");
        userResourceWithUserGuidEndpoints.MapPost("", UserResourceHandler.UpdateUserResourceAsync)
            .WithName("UpdateUserResource")
            .WithSummary("Updates a specific resource of a specific user.")
            .WithDescription("This endpoint updates a specific resource for a given user, and records this in the user's resource history.");
    }

    public static void RegisterOrganizationEndpoints(this IEndpointRouteBuilder endpointRouteBuilder)
    {
        var organizationEndpoints = endpointRouteBuilder.MapGroup("/organizations")
            .WithParameterValidation()
            .RequireAuthorization()
            .WithOpenApi()
            .WithTags("Organization");
        var organizationWithGuidEndpoints = organizationEndpoints.MapGroup("/{organizationId:Guid}");

        organizationEndpoints.MapGet("", OrganizationHandlers.GetOrganizationsAsync)
            .WithName("GetOrganizations")
            .WithSummary("Gets all organizations.")
            .WithDescription("This endpoint returns all organizations.");
        organizationWithGuidEndpoints.MapGet("", OrganizationHandlers.GetOrganizationAsync)
            .WithName("GetOrganizationById")
            .WithSummary("Gets a specific organization.")
            .WithDescription("This endpoint returns organization details for the given organizationId.");
        organizationEndpoints.MapPost("", OrganizationHandlers.CreateOrganizationAsync)
            .WithName("CreateOrganization")
            .WithSummary("Creates a new organization.")
            .WithDescription("This endpoint allows users to create a new organization.");
        organizationWithGuidEndpoints.MapPost("/user", OrganizationHandlers.AddUserToOrganizationAsync)
            .WithName("AddUserToOrganization")
            .RequireAuthorization(SecurityConstants.OrganizationAdminPolicy)
            .WithSummary("Adds a user to an organization.")
            .WithDescription("This endpoint allows administrators to add a user to their organization.");
        organizationWithGuidEndpoints.MapPost("/user/admin", OrganizationHandlers.MakeUserAdminInOrganizationAsync)
            .WithName("MakeUserAdminInOrganization")
            .RequireAuthorization(SecurityConstants.OrganizationOwnerPolicy)
            .WithSummary("Promotes a user to be an administrator in an organization.")
            .WithDescription("This endpoint allows the organization owner to promote a user to organization administrator.");
        organizationWithGuidEndpoints
            .MapDelete("/user/{userId:Guid}", OrganizationHandlers.DeleteUserFromOrganizationAsync)
            .WithName("DeleteUserFromOrganization")
            .RequireAuthorization(SecurityConstants.OrganizationAdminPolicy)
            .WithSummary("Removes a user from an organization.")
            .WithDescription("This endpoint allows an organization's administrators to remove a user from their organization.");
        organizationWithGuidEndpoints.MapDelete("", OrganizationHandlers.DeleteOrganizationAsync)
            .WithName("DeleteOrganization")
            .RequireAuthorization(SecurityConstants.OrganizationOwnerPolicy)
            .WithSummary("Deletes a specific organization.")
            .WithDescription("This endpoint allows the organization owner to delete an organization.");

        organizationWithGuidEndpoints.MapGet("/balance", OrganizationHandlers.GetOrganizationBalanceAsync)
            .WithName("GetOrganizationBalance")
            .WithTags("Organization Resources")
            .WithSummary("Gets the balance for a specific organization.")
            .WithDescription("This endpoint returns the current balance of the specified organization.");
        organizationWithGuidEndpoints
            .MapGet("/balance/history", OrganizationHandlers.GetOrganizationBalanceHistoryAsync)
            .WithName("GetOrganizationBalanceHistory")
            .WithTags("Organization Resources")
            .WithSummary("Gets the history of balance changes for a specific organization.")
            .WithDescription("This endpoint returns the history of balance changes for the specified organization.");
        organizationWithGuidEndpoints.MapGet("/resources", OrganizationHandlers.GetOrganizationResourcesAsync)
            .WithName("GetOrganizationResources")
            .WithTags("Organization Resources")
            .WithSummary("Gets the resources for a specific organization.")
            .WithDescription("This endpoint returns the resources of the specified organization.");
        organizationWithGuidEndpoints
            .MapGet("/resources/history", OrganizationHandlers.GetOrganizationResourceHistoryAsync)
            .WithName("GetOrganizationResourcesHistory")
            .WithTags("Organization Resources")
            .WithSummary("Gets the history of resource changes for a specific organization.")
            .WithDescription("This endpoint returns the history of resource changes for the specified organization.");
        organizationWithGuidEndpoints.MapGet("/balanceByUser", OrganizationHandlers.GetOrganizationBalanceByUserAsync)
            .WithName("GetOrganizationBalanceByUser")
            .WithTags("Organization Resources")
            .WithSummary("Gets the balance for a specific user in an organization.")
            .WithDescription("This endpoint returns the current balance of the specified user within a specific organization.");
        organizationWithGuidEndpoints
            .MapGet("/resourcesByUser", OrganizationHandlers.GetOrganizationResourcesByUserAsync)
            .WithName("GetOrganizationResourcesByUser")
            .WithTags("Organization Resources")
            .WithSummary("Gets the resources for a specific user in an organization.")
            .WithDescription("This endpoint returns the resources of the specified user within a specific organization.");
    }

    public static void RegisterResourceEndpoints(this IEndpointRouteBuilder endpointRouteBuilder)
    {
        var resourceEndpoints = endpointRouteBuilder.MapGroup("/resources")
            .WithOpenApi()
            .WithTags("Resource");
        var resourceWithIdEndpoints = resourceEndpoints.MapGroup("/{resourceId:int}");

        resourceEndpoints.MapGet("", ResourceHandler.GetResourcesAsync)
            .WithName("GetResources")
            .WithSummary("Gets all resources.")
            .WithDescription("This endpoint returns all resources.");
        resourceEndpoints.MapPost("", ResourceHandler.UpdateResourcesAsync)
            .WithName("UpdateResources")
            .WithSummary("Update all Resources")
            .WithDescription("This endpoint updates all resources.\n When not available, a new resource will be created")
            .RequireAuthorization(SecurityConstants.DeveloperPolicy);
        resourceWithIdEndpoints.MapGet("", ResourceHandler.GetResourceAsync)
            .WithName("GetResourceById")
            .WithSummary("Gets a specific resource.")
            .WithDescription("This endpoint returns the details for a resource specified by its resourceId.");
    }
    // TODO: Add Dev endpoints
}