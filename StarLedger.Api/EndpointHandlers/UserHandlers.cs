using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarLedger.Api.Dtos;
using StarLedger.Api.Dtos.User;
using StarLedger.Api.Extensions;
using StarLedger.Api.Models;

namespace StarLedger.Api.EndpointHandlers;

public static class UserHandlers
{
    public static async Task<Results<NotFound<string>, Ok<FullUserDto>>> GetUserAsync(
        StarLedgerDbContext dbContext,
        ILogger<UserDto> logger,
        [FromRoute] Guid userId)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user == null)
        {
            logger.LogWarning("The user with Guid: {0} was not found", userId);
            return TypedResults.NotFound($"The user with Guid: {userId} was not found");
        }

        return TypedResults.Ok(new FullUserDto{ UserId = user.Id, UserName = user.UserName, Email = user.Email, OrganizationId = user.OrganizationId ?? Guid.Empty});
    }

    public static async Task<Results<NotFound<string>, Ok<List<UserDto>>>> GetUsersAsync(
        StarLedgerDbContext dbContext,
        ILogger<UserDto> logger)
    {
        var users = await dbContext.Users
            .Select(u => new UserDto {UserId = u.Id, UserName = u.UserName})
            .ToListAsync();
        
        return TypedResults.Ok(users);
    }
    
    public static async Task<Ok<Dictionary<string, string>>> GetUserInformationAsync(
        ILogger<UserDto> logger,
        ClaimsPrincipal claimsPrincipal,
        UserManager<User> userManager)
    {
        var user = await userManager.GetUserAsync(claimsPrincipal);

        var claims = await userManager.GetClaimsAsync(user);
        var roles = await userManager.GetRolesAsync(user);

        var claimsDictionary = new Dictionary<string, string>(claims.Select(c => new KeyValuePair<string, string>(c.Type, c.Value)));
        claimsDictionary.Add("Roles", string.Join(", ", roles));

        return TypedResults.Ok(claimsDictionary);
    }
    
    public static async Task<Results<NotFound<string>, Ok<FullUserDto>>> UpdateUserAsync(
        StarLedgerDbContext dbContext,
        ILogger<FullUserDto> logger,
        [FromRoute] Guid userId,
        [FromBody] UpdateUserRequest updateUserRequest)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user == null)
        {
            logger.LogWarning("The user with Guid: {0} was not found", userId);
            return TypedResults.NotFound($"The user with Guid: {userId} was not found");
        }


        if (updateUserRequest.UserName != null)
        {
            user.UserName = updateUserRequest.UserName;
        }

        if (updateUserRequest.Email != null)
        {
            user.Email = updateUserRequest.Email;
        }

        await dbContext.SaveChangesAsync();
        
        return TypedResults.Ok(new FullUserDto{ UserId = user.Id, UserName = user.UserName, Email = user.Email, OrganizationId = user.OrganizationId ?? Guid.Empty});
    }

    public static async Task<Results<NotFound<string>, ValidationProblem, NoContent>> DeleteUserAsync(
        UserManager<User> userManager,
        ILogger<User> logger,
        [FromRoute] Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        
        if (user == null)
        {
            logger.LogWarning("The user with Guid: {0} was not found", userId);
            return TypedResults.NotFound($"The user with Guid: {userId} was not found");
        }

        var result = await userManager.DeleteAsync(user);

        if (!result.Succeeded)
        {
            logger.LogError("Updating Claims from User with Guid: {0} resulted in errors: {1}", user.Id, result.Errors);
            return ValidationProblemExtension.CreateValidationProblem(result);
        }
        
        logger.LogInformation("User with ID {UserId} has been deleted.", userId);
        return TypedResults.NoContent();

    }
    
    public static async Task<Results<NotFound<string>, Ok<UserBalanceDto>>> GetUserBalanceAsync(
        StarLedgerDbContext dbContext,
        ILogger<UserBalanceDto> logger,
        [FromRoute] Guid userId)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user == null)
        {
            logger.LogWarning("The user with Guid: {0} was not found", userId);
            return TypedResults.NotFound($"The user with Guid: {userId} was not found");
        }

        return TypedResults.Ok(new UserBalanceDto {UserId = user.Id, Balance = user.Balance});
    }
    
    public static async Task<Results<NotFound<string>, Ok<List<UserBalanceHistoryDto>>>> GetUserBalanceHistoryAsync(
        StarLedgerDbContext dbContext,
        ILogger<UserBalanceHistoryDto> logger,
        [FromRoute] Guid userId,
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            logger.LogWarning("The user with Guid: {0} was not found", userId);
            return TypedResults.NotFound($"The user with Guid: {userId} was not found");
        }

        if (startDate == null)
        {
            startDate = DateOnly.MinValue;
        }
        else if (endDate == null)
        {
            endDate = DateOnly.MaxValue;
        }
        
        var balanceHistory = await dbContext.UserBalanceHistories
            .Where(ubh => ubh.UserId == userId && ubh.Timestamp >= startDate && ubh.Timestamp <= endDate)
            .Select(ubh => new UserBalanceHistoryDto 
            {
                Id = ubh.Id,
                UserId = ubh.UserId,
                Timestamp = ubh.Timestamp,
                Balance = ubh.Balance
            })
            .OrderBy(u => u.Timestamp) // Order by ascending
            //.OrderByDescending(u => u.Timestamp) Order by descending
            .ToListAsync();

        return TypedResults.Ok(balanceHistory);
    }
    
    public static async Task<Results<NotFound<string>, UnprocessableEntity<string>, Created<UserBalanceDto>>> UpdateUserBalanceAsync(
        StarLedgerDbContext dbContext,
        LinkGenerator linkGenerator,
        HttpContext httpContext,
        ILogger<UpdateUserBalanceRequestDto> logger,
        [FromRoute] Guid userId,
        [FromBody] UpdateUserBalanceRequestDto request)
    {
        var user = await dbContext.Users.FindAsync(userId);
        if (user == null)
        {
            logger.LogWarning("User with Guid {UserId} not found.", userId);
            return TypedResults.NotFound($"User with Guid {userId} was not found");
        }

        // Adjust balance based on the update type
        switch (request.UpdateType)
        {
            case UpdateType.Add:
                user.Balance += request.UpdateAmount;
                logger.LogInformation("User balance has been increased. New balance of {userId} is {newBalance}", userId, user.Balance);
                break;
            case UpdateType.Subtract:
                if (user.Balance < request.UpdateAmount)
                {
                    logger.LogWarning("Attempted to subtract amount resulting in a negative balance for user {userId}.", userId);
                    return TypedResults.UnprocessableEntity(
                        $"Cant update User Balance with Guid {userId}, because it would result in a negative balance");
                }
                user.Balance -= request.UpdateAmount;
                logger.LogInformation("User balance has been decreased. New balance of {userId} is {newBalance}", userId, user.Balance);
                break;
            case UpdateType.Update:
                user.Balance = request.UpdateAmount;
                logger.LogInformation("User balance has been updated. New balance of {userId} is {newBalance}", userId, user.Balance);
                break;
        }

        dbContext.Users.Update(user);

        // Create and save a new balance history record
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var balanceHistory = await dbContext.UserBalanceHistories
            .FirstOrDefaultAsync(ubh => ubh.UserId == userId &&
                                        ubh.Timestamp == today);
        if (balanceHistory == null)
        {
            balanceHistory = new UserBalanceHistory
            {
                UserId = userId,
                Balance = user.Balance,
                Timestamp = today // Assuming UTC timestamp
            };
            dbContext.UserBalanceHistories.Add(balanceHistory);
        }
        else
        {
            balanceHistory.Balance = user.Balance;
            dbContext.UserBalanceHistories.Update(balanceHistory);
        }
        await dbContext.SaveChangesAsync();

        var linkToUserBalance = linkGenerator.GetUriByName(
            httpContext,
            "GetUserBalance",
            new { userId = user.Id });

        logger.LogInformation("Updated balance and balance history for user {UserId}. New balance: {NewBalance}", userId, user.Balance);
        return TypedResults.Created(linkToUserBalance ,new UserBalanceDto
        {
            UserId = user.Id,
            Balance = user.Balance
        });
    }
}