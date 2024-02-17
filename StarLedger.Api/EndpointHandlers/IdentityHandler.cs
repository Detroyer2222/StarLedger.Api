using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarLedger.Api.Dtos.Identity;
using StarLedger.Api.Extensions;
using StarLedger.Api.Models;

namespace StarLedger.Api.EndpointHandlers;

[EndpointGroupName("Identity")]
public static class IdentityHandler
{
    public static async Task<Results<NotFound<string>, ValidationProblem, Ok<UserClaimsDto>>> UpdateUserClaimsAsync(
        UserManager<User> userManager,
        StarLedgerDbContext dbContext,
        ILogger<UserClaimsDto> logger,
        [FromRoute]Guid userId,
        [FromBody]UserClaimsDto claimsDto)
    {
        logger.LogInformation("Getting user from database");
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user == null)
        {
            logger.LogWarning("User with Guid: {0} was not found", userId);
            return TypedResults.NotFound($"User with Guid {userId} was not found");
        }

        foreach (var claims in claimsDto.Claims)
        {
            var existingClaim = await userManager.GetClaimsAsync(user);
            if (existingClaim.Any(c => c.Type == claims.Key))
            {
                logger.LogInformation("Claim Key{0} Value{1} already exists for User with Guid: {2}", claims.Key, claims.Value, userId);
                continue;
            }
            
            var result = await userManager.AddClaimAsync(user, new Claim(claims.Key, claims.Value));
            if (!result.Succeeded)
            {
                logger.LogError("Updating Claim Key{0} Value{1} from User with Guid: {2} resulted in errors: {3}", claims.Key, claims.Value, userId, result.Errors);
                return ValidationProblemExtension.CreateValidationProblem(result);
            }
        }
        
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            logger.LogError("Updating Claims from User with Guid: {0} resulted in errors: {1}",userId, updateResult.Errors);
            return ValidationProblemExtension.CreateValidationProblem(updateResult);
        }
        
        var newClaims = await userManager.GetClaimsAsync(user);
        logger.LogInformation("Successfully update claims from User with Guid: {0} with Claims: {1}", user.Id, claimsDto.Claims);
        return TypedResults.Ok(new UserClaimsDto
        {
            Claims = new Dictionary<string, string>(newClaims.Select(c => new KeyValuePair<string, string>(c.Type, c.Value)))
        });
    }
}