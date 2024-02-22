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

        if (user is null)
        {
            logger.LogWarning("The user with Guid: {0} was not found", userId);
            return TypedResults.NotFound($"The user with Guid: {userId} was not found");
        }

        return TypedResults.Ok(new FullUserDto { UserId = user.Id, StarCitizenHandle = user.StarCitizenHandle, Email = user.Email, OrganizationId = user.OrganizationId ?? Guid.Empty });
    }

    public static async Task<Results<NotFound<string>, Ok<List<UserDto>>>> GetUsersAsync(
        StarLedgerDbContext dbContext,
        ILogger<UserDto> logger)
    {
        var users = await dbContext.Users
            .Select(u => new UserDto { UserId = u.Id, Email = u.Email, StarCitizenHandle = u.StarCitizenHandle })
            .ToListAsync();

        return TypedResults.Ok(users);
    }

    public static async Task<Results<Ok<Dictionary<string, string>>, NotFound<string>>> GetUserClaimsAsync(
        ILogger<UserDto> logger,
        ClaimsPrincipal claimsPrincipal)
    {
        logger.LogInformation("Getting claims for user {UserId}", claimsPrincipal.Identity);
        var claims = claimsPrincipal.Claims.ToList();
        var claimsDictionary = new Dictionary<string, string>(claims.Select(c => new KeyValuePair<string, string>(c.Type, c.Value)));

        return TypedResults.Ok(claimsDictionary);
    }

    public static async Task<Results<NotFound<string>, Ok<FullUserDto>>> UpdateUserAsync(
        UserManager<User> userManager,
        ILogger<FullUserDto> logger,
        [FromRoute] Guid userId,
        [FromBody] UpdateUserRequest updateUserRequest)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            logger.LogWarning("The user with Guid: {UserId} was not found", userId);
            return TypedResults.NotFound($"The user with Guid: {userId} was not found");
        }


        if (updateUserRequest.StarCitizenHandle is not null)
        {
            await userManager.AddClaimAsync(user, new Claim("StarCitizenHandle", updateUserRequest.StarCitizenHandle));
            user.StarCitizenHandle = updateUserRequest.StarCitizenHandle;
        }

        if (updateUserRequest.Email is not null)
        {
            user.Email = updateUserRequest.Email;
            user.UserName = updateUserRequest.Email;
        }

        await userManager.UpdateAsync(user);

        return TypedResults.Ok(new FullUserDto { UserId = user.Id, StarCitizenHandle = user.StarCitizenHandle, Email = user.Email, OrganizationId = user.OrganizationId ?? Guid.Empty });
    }

    public static async Task<Results<NotFound<string>, ValidationProblem, NoContent>> DeleteUserAsync(
        UserManager<User> userManager,
        ILogger<User> logger,
        [FromRoute] Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            logger.LogWarning("The user with Guid: {UserId} was not found", userId);
            return TypedResults.NotFound($"The user with Guid: {userId} was not found");
        }

        var result = await userManager.DeleteAsync(user);

        if (!result.Succeeded)
        {
            logger.LogError("Updating Claims from User with Guid: {UserId} resulted in errors: {Errors}", user.Id, result.Errors);
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

        if (user is null)
        {
            logger.LogWarning("The user with Guid: {UserId} was not found", userId);
            return TypedResults.NotFound($"The user with Guid: {userId} was not found");
        }

        return TypedResults.Ok(new UserBalanceDto { UserId = user.Id, Balance = user.Balance });
    }

    public static async Task<Results<NotFound<string>, Ok<List<UserBalanceHistoryDto>>>> GetUserBalanceHistoryAsync(
        StarLedgerDbContext dbContext,
        ILogger<UserBalanceHistoryDto> logger,
        [FromRoute] Guid userId,
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
        {
            logger.LogWarning("The user with Guid: {UserId} was not found", userId);
            return TypedResults.NotFound($"The user with Guid: {userId} was not found");
        }

        if (startDate is null)
        {
            startDate = DateOnly.MinValue;
        }
        else if (endDate is null)
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
        if (user is null)
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
        if (balanceHistory is null)
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
        return TypedResults.Created(linkToUserBalance, new UserBalanceDto
        {
            UserId = user.Id,
            Balance = user.Balance
        });
    }
}