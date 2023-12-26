using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarLedger.Api.Dtos;
using StarLedger.Api.Dtos.UserResource;
using StarLedger.Api.Models;

namespace StarLedger.Api.EndpointHandlers;

public static class UserResourceHandler
{
     public static async Task<Results<NotFound<string>, Ok<List<UserResourceDto>>>> GetUserResourcesAsync(
        StarLedgerDbContext dbContext,
        ILogger<List<UserResourceDto>> logger,
        [FromRoute] Guid userId)
    {
        logger.LogInformation($"Attempting to get UserResourceDtos for UserID[{userId}]");
        var userResources = await dbContext.UserResources.Where(ur => ur.UserId == userId)
            .Select(ur => new UserResourceDto
            {
                UserId = ur.UserId,
                ResourceId = ur.ResourceId,
                Quantity = ur.Quantity
            }).ToListAsync();

        if (userResources.Count < 1)
        {
            logger.LogWarning($"UserResources with UserID[{userId}] not found");
            return TypedResults.NotFound($"UserResources with UserID {userId} not found");
        }

        logger.LogInformation($"Found [{userResources.Count}] UserResourceDtos for UserID[{userId}]");
        return TypedResults.Ok(userResources);
    }
    
    public static async Task<Results<NotFound<string>, Ok<UserResourceDto>>> GetUserResourceAsync(
        StarLedgerDbContext dbContext,
        ILogger<UserResourceDto> logger,
        [FromRoute] Guid userId, 
        [FromRoute] int resourceId)
    {
        logger.LogInformation($"Attempting to get UserResourceDto for UserID[{userId}] and ResourceID[{resourceId}]");
        var userResource =
            await dbContext.UserResources.FirstOrDefaultAsync(ur => ur.UserId == userId && ur.ResourceId == resourceId);

        if (userResource == null)
        {
            logger.LogWarning($"UserResource with UserID[{userId}] and ResourceID[{resourceId}] not found");
            return TypedResults.NotFound($"UserResource with UserID {userId} not found");
        }

        logger.LogInformation($"Found UserResourceDto for UserID[{userId}] and ResourceID[{resourceId}]");
        return TypedResults.Ok(new UserResourceDto
        {
            UserId = userResource.UserId,
            ResourceId = userResource.ResourceId,
            Quantity = userResource.Quantity
        });
    }
    
    public static async Task<Results<NotFound<string>, UnprocessableEntity<string>, Created<UserResourceDto>>> UpdateUserResourceAsync(
            StarLedgerDbContext dbContext,
            ILogger<UpdateUserResourceRequestDto> logger,
            LinkGenerator linkGenerator,
            HttpContext httpContext,
            [FromRoute] Guid userId,
            [FromBody] UpdateUserResourceRequestDto request)
    {
        var userResource = await dbContext.UserResources
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.ResourceId == request.ResourceId);

        if (userResource == null)
        {
            logger.LogWarning("UserResource with UserID {UserId} and ResourceID {ResourceId} not found.", userId,
                request.ResourceId);
            return TypedResults.NotFound(
                $"UserResource with UserID {userId} and ResourceID {request.ResourceId} not found.");
        }

        // Adjust quantity based on the update type
        switch (request.UpdateType)
        {
            case UpdateType.Add:
                logger.LogInformation(
                    "Adding {Quantity} to UserResource with UserID {UserId} and ResourceID {ResourceId}.",
                    request.Quantity, userId, request.ResourceId);
                userResource.Quantity += request.Quantity;
                break;
            case UpdateType.Subtract:
                if (userResource.Quantity < request.Quantity)
                {
                    logger.LogWarning(
                        "Cannot subtract {Quantity} from UserResource with UserID {UserId} and ResourceID {ResourceId}. Current quantity of UserResource is less than the requested subtract quantity.",
                        request.Quantity, userId, request.ResourceId);
                    return TypedResults.UnprocessableEntity(
                        $"Cannot subtract {request.Quantity} from UserResource. Current quantity of UserResource is less than the requested subtract quantity.");
                }

                logger.LogInformation(
                    "Subtracting {Quantity} from UserResource with UserID {UserId} and ResourceID {ResourceId}.",
                    request.Quantity, userId, request.ResourceId);
                userResource.Quantity -= request.Quantity;
                break;
            case UpdateType.Update:
                logger.LogInformation(
                    "Updating UserResource with UserID {UserId} and ResourceID {ResourceId} quantity to {Quantity}.",
                    userId, request.ResourceId, request.Quantity);
                userResource.Quantity = request.Quantity;
                break;
        }

        dbContext.UserResources.Update(userResource);

        // Find or create a new resource quantity history record for the day
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var resourceQuantityHistory = await dbContext.ResourceQuantityHistories
            .FirstOrDefaultAsync(rqh => rqh.UserId == userId && 
                                        rqh.ResourceId == request.ResourceId && 
                                        rqh.Timestamp == today);

        if (resourceQuantityHistory == null)
        {
            resourceQuantityHistory = new ResourceQuantityHistory
            {
                UserId = userId,
                ResourceId = request.ResourceId,
                Timestamp = today,
                Quantity = userResource.Quantity
            };
            dbContext.ResourceQuantityHistories.Add(resourceQuantityHistory);
        }
        else
        {
            resourceQuantityHistory.Quantity = userResource.Quantity;
            dbContext.ResourceQuantityHistories.Update(resourceQuantityHistory);
        }

        await dbContext.SaveChangesAsync();

        logger.LogInformation(
            "Updated resource quantity and history for user {UserId} and resource {ResourceId}. New quantity: {Quantity}",
            userId, request.ResourceId, userResource.Quantity);

        var linkToUserResource = linkGenerator.GetUriByName(
            httpContext,
            "GetUserResourceById",
            new { userId = userResource.UserId, resourceId = userResource.ResourceId });
        return TypedResults.Created(linkToUserResource, new UserResourceDto
        {
            UserId = userResource.UserId,
            ResourceId = userResource.ResourceId,
            Quantity = userResource.Quantity
        });
    }

    public static async Task<Results<NotFound<string>, Ok<List<ResourceQuantityHistoryDto>>>> GetUserResourceQuantityHistoryAsync(
        StarLedgerDbContext dbContext,
        ILogger<ResourceQuantityHistoryDto> logger,
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
        
        var resourceQuantityHistory = await dbContext.ResourceQuantityHistories
            .Where(rqh => rqh.UserId == userId && rqh.Timestamp >= startDate && rqh.Timestamp <= endDate)
            .OrderBy(rqh => rqh.Timestamp) // Or use OrderByDescending for descending order
            .Select(rqh => new ResourceQuantityHistoryDto
            {
                UserId = rqh.UserId,
                ResourceId = rqh.ResourceId,
                Timestamp = rqh.Timestamp,
                Quantity = rqh.Quantity
            })
            .ToListAsync();

        return TypedResults.Ok(resourceQuantityHistory);
    }
}