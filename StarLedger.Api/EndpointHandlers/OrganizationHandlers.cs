using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarLedger.Api.Dtos.Organization;
using StarLedger.Api.Dtos.User;
using StarLedger.Api.Extensions;
using StarLedger.Api.Models;

namespace StarLedger.Api.EndpointHandlers;

public static class OrganizationHandlers
{
    public static async Task<Results<NoContent, Ok<List<OrganizationDto>>>> GetOrganizationsAsync(
        StarLedgerDbContext dbContext,
        ILogger<OrganizationDto> logger)
    {
        var organizations = await dbContext.Organizations.
            Select(o => new OrganizationDto
            {
                OrganizationId = o.OrganizationId,
                Name = o.Name
            })
            .ToListAsync();

        if (organizations.Count < 1)
        {
            logger.LogWarning("No Organizations found");
            return TypedResults.NoContent();
        }

        return TypedResults.Ok(organizations);
    }

    public static async Task<Results<NotFound<string>, Ok<OrganizationWithUserDto>>> GetOrganizationAsync(
        StarLedgerDbContext dbContext,
        ILogger<OrganizationDto> logger,
        [FromRoute] Guid organizationId,
        [FromQuery] bool? withUser)
    {
        var organization = await dbContext.Organizations.Include(o => o.Users)
            .Where(o => o.OrganizationId == organizationId)
            .Select(o => new OrganizationWithUserDto
            {
                OrganizationId = o.OrganizationId,
                Users = o.Users.Select(u => new UserDto
                {
                    UserId = u.Id,
                    Email = u.Email,
                    StarCitizenHandle = u.StarCitizenHandle
                }).ToList()
            }).FirstOrDefaultAsync();

        if (organization is null)
        {
            logger.LogWarning("Organization with GUID: {0} was not found", organizationId);
            return TypedResults.NotFound($"Organization with GUID: {organizationId} was not found");
        }
        logger.LogInformation("Organization with GUID: {0} returned", organizationId);
        return TypedResults.Ok(organization);
    }

    public static async Task<Results<NotFound<string>, ValidationProblem, Created<OrganizationDto>>> CreateOrganizationAsync(
        StarLedgerDbContext dbContext,
        UserManager<User> userManager,
        LinkGenerator linkGenerator,
        HttpContext httpContext,
        ILogger<CreateOrganizationRequest> logger,
        [FromBody] CreateOrganizationRequest request)
    {
        var newOrganization = new Organization
        {
            OrganizationId = Guid.NewGuid(),
            Name = request.OrganizationName
        };

        logger.LogInformation("Created new organization with ID {OrganizationId} and Name {Name}", newOrganization.OrganizationId, newOrganization.Name);

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == request.CreatedBy);
        if (user is null)
        {
            logger.LogWarning("User with Guid: {0} was not found", request.CreatedBy);
            return TypedResults.NotFound("User was not found");
        }

        var organizationClaim = new Claim(SecurityConstants.OrganizationClaimType, newOrganization.OrganizationId.ToString());
        var claimsResult = await userManager.AddClaimAsync(user, organizationClaim);
        
        if (!claimsResult.Succeeded)
        {
            logger.LogError("Updating Claims of User with Guid: {0} resulted in errors: {1}", request.CreatedBy, claimsResult.Errors);
            return ValidationProblemExtension.CreateValidationProblem(claimsResult);
        }

        var rolesResult = await userManager.AddToRolesAsync(user, new[] { SecurityConstants.OrganizationOwnerRole, SecurityConstants.OrganizationAdminRole });
        if (!rolesResult.Succeeded)
        {
            logger.LogError("Updating Claims of User with Guid: {0} resulted in errors: {1}", request.CreatedBy, claimsResult.Errors);
            return ValidationProblemExtension.CreateValidationProblem(claimsResult);
        }

        user.OrganizationId = newOrganization.OrganizationId;
        user.IsOwner = true;
        user.IsAdmin = true;
        var updateResult = await userManager.UpdateAsync(user);
        dbContext.Organizations.Add(newOrganization);
        await dbContext.SaveChangesAsync();
        if (!updateResult.Succeeded)
        {
            logger.LogError("Updating Roles of User with Guid: {0} resulted in errors: {1}", request.CreatedBy, rolesResult.Errors);
            return ValidationProblemExtension.CreateValidationProblem(rolesResult);
        }

        var linkToOrganization = linkGenerator.GetUriByName(
            httpContext,
            "GetOrganizationById",
            new { organizationId = newOrganization.OrganizationId });

        return TypedResults.Created(linkToOrganization, new OrganizationDto
        {
            OrganizationId = newOrganization.OrganizationId,
            Name = newOrganization.Name
        });
    }

    public static async Task<Results<NotFound<string>, ValidationProblem, Ok<OrganizationWithUserDto>>> AddUserToOrganizationAsync(
        StarLedgerDbContext dbContext,
        ClaimsPrincipal claimsPrincipal,
        UserManager<User> userManager,
        HttpContext httpContext,
        ILogger<AddUserToOrganizationRequest> logger,
        [FromRoute] Guid organizationId,
        [FromBody] AddUserToOrganizationRequest request)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null)
        {
            logger.LogWarning("User with ID {UserId} not found.", request.UserId);
            return TypedResults.NotFound($"User with ID {request.UserId} not found.");
        }

        var organization = await dbContext.Organizations.Include(o => o.Users).FirstOrDefaultAsync(o => o.OrganizationId == organizationId);
        if (organization is null)
        {
            logger.LogWarning("Organization with ID {0} not found.", organizationId);
            return TypedResults.NotFound($"Organization with ID {organizationId} not found.");
        }

        user.OrganizationId = organizationId;
        logger.LogInformation("User with ID {0} added to Organization with ID {1}.", user.Id, organization.OrganizationId);
        var organizationWithUsers = new OrganizationWithUserDto
        {
            OrganizationId = organization.OrganizationId,
            Users = organization.Users.Select(u => new UserDto
            {
                UserId = u.Id,
                Email = u.Email,
                StarCitizenHandle = u.StarCitizenHandle
            }).ToList()
        };

        await dbContext.SaveChangesAsync();

        var organizationClaim = new Claim(SecurityConstants.OrganizationClaimType, organization.OrganizationId.ToString());
        //Check if claim already exists
        var existingClaim = await userManager.GetClaimsAsync(user);
        if (existingClaim.Any(c => c.Type == SecurityConstants.OrganizationClaimType))
        {
            logger.LogInformation("Claim Key {0} Value {1} already exists for User with Guid: {2}", organizationClaim.Type, organizationClaim.Value, user.Id);
            return TypedResults.Ok(organizationWithUsers);
        }
        var result = await userManager.AddClaimAsync(user, organizationClaim);
        if (!result.Succeeded)
        {
            logger.LogError("Updating Claims from User with Guid: {0} resulted in errors: {1}", user.Id, result.Errors);
            return ValidationProblemExtension.CreateValidationProblem(result);
        }
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            logger.LogError("Updating Claims from User with Guid: {0} resulted in errors: {1}", user.Id, result.Errors);
            return ValidationProblemExtension.CreateValidationProblem(result);
        }

        return TypedResults.Ok(organizationWithUsers);
    }

    public static async Task<Results<NotFound<string>, ValidationProblem, Ok<OrganizationWithUserDto>>> MakeUserAdminInOrganizationAsync(
    StarLedgerDbContext dbContext,
    UserManager<User> userManager,
    ILogger<AddUserToOrganizationRequest> logger,
    [FromRoute] Guid organizationId,
    [FromBody] AddUserToOrganizationRequest request)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null)
        {
            logger.LogWarning("User with ID {UserId} not found.", request.UserId);
            return TypedResults.NotFound($"User with ID {request.UserId} not found.");
        }

        var organization = await dbContext.Organizations.Include(o => o.Users).FirstOrDefaultAsync(o => o.OrganizationId == organizationId);
        if (organization is null)
        {
            logger.LogWarning("Organization with ID {0} not found.", organizationId);
            return TypedResults.NotFound($"Organization with ID {organizationId} not found.");
        }

        var organizationWithUsers = new OrganizationWithUserDto
        {
            OrganizationId = organization.OrganizationId,
            Users = organization.Users.Select(u => new UserDto
            {
                UserId = u.Id,
                Email = u.Email,
                StarCitizenHandle = u.StarCitizenHandle
            }).ToList()
        };

        await dbContext.SaveChangesAsync();
        logger.LogInformation("User with ID {0} received Admin privileges in Organization with ID {1}.", user.Id, organization.OrganizationId);


        var roles = await userManager.GetRolesAsync(user);
        if (roles.Contains(SecurityConstants.OrganizationAdminRole))
        {
            logger.LogInformation("User with ID {0} already has Admin privileges in Organization with ID {1}.", user.Id, organization.OrganizationId);
            return TypedResults.Ok(organizationWithUsers);
        }

        var rolesResult = await userManager.AddToRoleAsync(user, SecurityConstants.OrganizationAdminRole);
        if (!rolesResult.Succeeded)
        {
            logger.LogError("Updating Roles from User with Guid: {0} resulted in errors: {1}", user.Id, rolesResult.Errors);
            return ValidationProblemExtension.CreateValidationProblem(rolesResult);
        }

        user.OrganizationId = organizationId;
        user.IsAdmin = true;
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            logger.LogError("Updating Claims from User with Guid: {0} resulted in errors: {1}", user.Id, updateResult.Errors);
            return ValidationProblemExtension.CreateValidationProblem(updateResult);
        }

        return TypedResults.Ok(organizationWithUsers);
    }

    public static async Task<Results<NotFound<string>, NoContent>> DeleteOrganizationAsync(
        StarLedgerDbContext dbContext,
        ILogger<OrganizationDto> logger,
        [FromRoute] Guid organizationId)
    {
        var organization = await dbContext.Organizations.FindAsync(organizationId);
        if (organization is null)
        {
            logger.LogWarning("Organization with ID {0} not found.", organizationId);
            return TypedResults.NotFound($"Organization with ID {organizationId} not found.");
        }

        dbContext.Organizations.Remove(organization);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Deleted organization with ID {0}.", organizationId);

        return TypedResults.NoContent();
    }

    public static async Task<Results<NotFound<string>, ValidationProblem, NoContent>> DeleteUserFromOrganizationAsync(
        StarLedgerDbContext dbContext,
        UserManager<User> userManager,
        ILogger<OrganizationDto> logger,
        [FromRoute] Guid organizationId,
        [FromRoute] Guid userId)
    {
        var organization = await dbContext.Organizations.FindAsync(organizationId);
        if (organization is null)
        {
            logger.LogWarning("Organization with ID {0} not found.", organizationId);
            return TypedResults.NotFound($"Organization with ID {organizationId} not found.");
        }

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            logger.LogWarning("User with ID {0} not found.", userId);
            return TypedResults.NotFound($"User with ID {userId} not found.");
        }

        user.OrganizationId = null;
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            logger.LogError("Failed to remove User with ID {0} from the organization. Errors: {1}", userId, result.Errors);
            return ValidationProblemExtension.CreateValidationProblem(result);
        }

        logger.LogInformation("User with ID {UserId} has been removed from the organization.", userId);
        return TypedResults.NoContent();
    }

    public static async Task<Results<NotFound<string>, Ok<OrganizationBalanceDto>>> GetOrganizationBalanceAsync(
        StarLedgerDbContext dbContext,
        ILogger<OrganizationBalanceDto> logger,
        [FromRoute] Guid organizationId)
    {
        var organization = await dbContext.Organizations.Include(o => o.Users).FirstOrDefaultAsync(o => o.OrganizationId == organizationId);
        if (organization is null)
        {
            logger.LogWarning("Organization with ID {OrganizationId} not found.", organizationId);
            return TypedResults.NotFound($"Organization with ID {organizationId} not found.");
        }

        long totalBalance = organization.Users.Sum(u => u.Balance);

        logger.LogInformation("Retrieved total balance for organization with ID {OrganizationId}. Total balance: {TotalBalance}", organizationId, totalBalance);

        return TypedResults.Ok(new OrganizationBalanceDto { OrganizationId = organization.OrganizationId, Balance = totalBalance });
    }

    public static async Task<Results<NotFound<string>, NoContent, Ok<List<OrganizationBalanceHistoryDto>>>> GetOrganizationBalanceHistoryAsync(
        StarLedgerDbContext dbContext,
        ILogger<List<OrganizationBalanceHistoryDto>> logger,
        [FromRoute] Guid organizationId,
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate)
    {
        // Apply date values if none are provided
        if (!startDate.HasValue)
        {
            startDate = DateOnly.MinValue;
        }
        else if (!endDate.HasValue)
        {
            endDate = DateOnly.MaxValue;
        }

        var organization = await dbContext.Organizations.Include(o => o.Users).FirstOrDefaultAsync(o => o.OrganizationId == organizationId);
        if (organization is null)
        {
            logger.LogWarning("Organization with ID {OrganizationId} not found.", organizationId);
            return TypedResults.NotFound($"Organization with ID {organizationId} not found.");
        }

        var balanceHistoryQuery = dbContext.UserBalanceHistories.AsQueryable();

        // Filter by organization's users
        balanceHistoryQuery = balanceHistoryQuery.Where(ubh => organization.Users.Select(u => u.Id).Contains(ubh.UserId));

        balanceHistoryQuery = balanceHistoryQuery.Where(ubh => ubh.Timestamp >= startDate.Value && ubh.Timestamp <= endDate!.Value);

        var balanceHistories = await balanceHistoryQuery
            .GroupBy(ubh => ubh.Timestamp)
            .Select(group => new OrganizationBalanceHistoryDto
            {
                OrganizationId = organizationId,
                Timestamp = group.Key,
                Balance = group.Sum(ubh => ubh.Balance)
            })
            .ToListAsync();

        if (balanceHistories.Count < 1)
        {
            logger.LogWarning("No Results found for OrganizationBalanceHistory with Date range from: {0} to {1}", startDate, endDate);
            return TypedResults.NoContent();
        }

        logger.LogInformation("Retrieved balance history for organization with ID {OrganizationId}.", organizationId);

        return TypedResults.Ok(balanceHistories);
    }

    public static async Task<Results<NotFound<string>, NoContent, Ok<List<OrganizationResourceDto>>>> GetOrganizationResourcesAsync(
        StarLedgerDbContext dbContext,
        ILogger<List<OrganizationResourceDto>> logger,
        [FromRoute] Guid organizationId)
    {
        var organization = await dbContext.Organizations.FirstOrDefaultAsync(o => o.OrganizationId == organizationId);
        if (organization is null)
        {
            logger.LogWarning("Organization with ID {OrganizationId} not found.", organizationId);
            return TypedResults.NotFound($"Organization with ID {organizationId} not found.");
        }

        var resources = await dbContext.Users
            .Where(u => u.OrganizationId == organizationId)
            .SelectMany(u => u.UserResources)
            .GroupBy(ur => ur.ResourceId)
            .Select(group => new OrganizationResourceDto
            {
                OrganizationId = organizationId,
                ResourceId = group.Key,
                Quantity = group.Sum(ur => ur.Quantity)
            })
            .ToListAsync();

        if (resources.Count < 1)
        {
            logger.LogWarning("No resources found for organization with ID {OrganizationId}.", organizationId);
            return TypedResults.NoContent();
        }

        logger.LogInformation("Retrieved resources for organization with ID {OrganizationId}.", organizationId);
        return TypedResults.Ok(resources);
    }

    public static async Task<Results<NotFound<string>, NoContent, Ok<List<OrganizationResourceQuantityHistory>>>> GetOrganizationResourceHistoryAsync(
        StarLedgerDbContext dbContext,
        ILogger<List<OrganizationResourceQuantityHistory>> logger,
        [FromRoute] Guid organizationId,
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate)
    {
        // Apply date values if none are provided
        if (!startDate.HasValue)
        {
            startDate = DateOnly.MinValue;
        }
        else if (!endDate.HasValue)
        {
            endDate = DateOnly.MaxValue;
        }

        //Check if organization exists
        var organization = await dbContext.Organizations.FirstOrDefaultAsync(o => o.OrganizationId == organizationId);
        if (organization is null)
        {
            logger.LogWarning("Organization with ID {OrganizationId} not found.", organizationId);
            return TypedResults.NotFound($"Organization with ID {organizationId} not found.");
        }

        var resourceHistoryQuery = dbContext.ResourceQuantityHistories.AsQueryable();

        //Filter by Organization users
        resourceHistoryQuery =
            resourceHistoryQuery.Where(rh => organization.Users.Select(u => u.Id).Contains(rh.UserId));

        resourceHistoryQuery =
            resourceHistoryQuery.Where(rh => rh.Timestamp >= startDate.Value && rh.Timestamp <= endDate!.Value);

        var organizationResourceHistories = await resourceHistoryQuery
            .GroupBy(rh => new { rh.Timestamp, rh.ResourceId })
            .Select(group => new OrganizationResourceQuantityHistory
            {
                OrganizationId = organizationId,
                ResourceId = group.Key.ResourceId,
                Timestamp = group.Key.Timestamp,
                Quantity = group.Sum(rh => rh.Quantity)
            }).ToListAsync();


        if (organizationResourceHistories.Count < 1)
        {
            logger.LogWarning("No resource history found for organization with ID {OrganizationId}.", organizationId);
            return TypedResults.NoContent();
        }

        logger.LogInformation("Retrieved resource history for organization with ID {OrganizationId}.", organizationId);
        return TypedResults.Ok(organizationResourceHistories);
    }

    public static async Task<Results<NotFound<string>, NoContent, Ok<List<OrganizationBalanceByUserDto>>>> GetOrganizationBalanceByUserAsync(
        StarLedgerDbContext dbContext,
        ILogger<List<OrganizationBalanceByUserDto>> logger,
        [FromRoute] Guid organizationId)
    {
        //Check if organization exists
        var organization = await dbContext.Organizations.Include(o => o.Users).FirstOrDefaultAsync(o => o.OrganizationId == organizationId);
        if (organization is null)
        {
            logger.LogWarning("Organization with ID {OrganizationId} not found.", organizationId);
            return TypedResults.NotFound($"Organization with ID {organizationId} not found.");
        }

        var balanceByUser = organization.Users.Select(u => new OrganizationBalanceByUserDto
        {
            OrganizationId = organizationId,
            UserId = u.Id,
            UserName = u.UserName!,
            Balance = u.Balance
        }).ToList();

        if (balanceByUser.Count < 1)
        {
            logger.LogWarning("No Balances by User found for organization with ID: {0}", organizationId);
            return TypedResults.NoContent();
        }
        logger.LogInformation("Retrieved balance by user for organization with ID {0}. Total entries: {1}", organizationId, balanceByUser.Count);

        return TypedResults.Ok(balanceByUser);
    }

    public static async Task<Results<NotFound<string>, NoContent, Ok<List<OrganizationResourceByUserDto>>>> GetOrganizationResourcesByUserAsync(
            StarLedgerDbContext dbContext,
            ILogger<List<OrganizationResourceByUserDto>> logger,
            [FromRoute] Guid organizationId)
    {
        //Check if organization exists
        var organization = await dbContext.Organizations.FirstOrDefaultAsync(o => o.OrganizationId == organizationId);
        if (organization is null)
        {
            logger.LogWarning("Organization with ID {OrganizationId} not found.", organizationId);
            return TypedResults.NotFound($"Organization with ID {organizationId} not found.");
        }

        var resourcesByUser = await dbContext.Users
            .Where(u => u.OrganizationId == organizationId)
            .SelectMany(u => u.UserResources.Select(ur => new { User = u, UserResource = ur }))
            .GroupBy(x => new { x.UserResource.ResourceId, x.User.Id })
            .Select(group => new OrganizationResourceByUserDto
            {
                OrganizationId = organizationId,
                UserId = group.Key.Id,
                UserName = group.FirstOrDefault()!.User.UserName!,
                ResourceId = group.Key.ResourceId,
                Quantity = group.Sum(x => x.UserResource.Quantity)
            })
            .ToListAsync();

        if (resourcesByUser.Count < 1)
        {
            logger.LogWarning("No resources found for organization with ID {OrganizationId}.", organizationId);
            return TypedResults.NoContent();
        }

        logger.LogInformation("Retrieved resources for organization with ID {OrganizationId}.", organizationId);
        return TypedResults.Ok(resourcesByUser);
    }

}