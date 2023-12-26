using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarLedger.Api.Dtos.Resource;
using StarLedger.Api.Models;

namespace StarLedger.Api.EndpointHandlers;

public static class ResourceHandler
{
    public static async Task<Results<NoContent, Ok<List<ResourceDto>>>> GetResourcesAsync(
        StarLedgerDbContext dbContext,
        ILogger<ResourceDto> logger)
    {
        logger.LogInformation("Getting all Resources");
        var resources = await dbContext.Resources.Select(r => new ResourceDto()
        {
            ResourceId = r.ResourceId,
            Name = r.Name,
            Code = r.Code,
            Type = r.Type,
            PriceBuy = r.PriceBuy,
            PriceSell = r.PriceSell,
            LastUpdated = r.LastUpdated
        }).ToListAsync();

        if (resources.Count < 1)
        {
            logger.LogWarning("No resources found");
            return TypedResults.NoContent();
        }

        return TypedResults.Ok(resources);
    }

    public static async Task<Results<NotFound<string>, Ok<ResourceDto>>> GetResourceAsync(
        StarLedgerDbContext dbContext,
        ILogger<ResourceDto> logger,
        [FromRoute] int resourceId)
    {
        logger.LogInformation("Getting Resource with ID: {0}", resourceId);
        var resource = await dbContext.Resources.FirstOrDefaultAsync(r => r.ResourceId == resourceId);

        if (resource == null)
        {
            logger.LogWarning("No resource with ID: {0} found", resourceId);
            return TypedResults.NotFound($"Resource with ID: {resourceId} not found");
        }

        return TypedResults.Ok(new ResourceDto
        {
            ResourceId = resource.ResourceId,
            Name = resource.Name,
            Code = resource.Code,
            Type = resource.Type,
            PriceBuy = resource.PriceBuy,
            PriceSell = resource.PriceSell,
            LastUpdated = resource.LastUpdated
        });
    }

    public static async Task<Ok<List<ResourceDto>>> UpdateResourcesAsync(
        StarLedgerDbContext dbContext,
        ILogger<ResourceDto> logger,
        [FromBody] List<UpdateResourceRequest> resourcesToUpdate)
    {
        logger.LogInformation("Updating Resources");

        foreach (var resourceToUpdate in resourcesToUpdate)
        {
            var existingResource = await dbContext.Resources
                .FirstOrDefaultAsync(r => r.Code == resourceToUpdate.Code);

            if (existingResource != null)
            {
                logger.LogInformation("Resources with Code:{0} already exists", existingResource.Code);

                existingResource.Name = resourceToUpdate.Name;
                existingResource.Code = resourceToUpdate.Code;
                existingResource.Type = resourceToUpdate.Type;
                existingResource.PriceBuy = resourceToUpdate.PriceBuy;
                existingResource.PriceSell = resourceToUpdate.PriceSell;
                existingResource.LastUpdated = DateTime.UtcNow;
            }
            else
            {
                logger.LogInformation("Resources with Code:{0} is being created", resourceToUpdate.Code);
                // Add new resource
                var newResource = new Resource
                {
                    Name = resourceToUpdate.Name,
                    Code = resourceToUpdate.Code,
                    Type = resourceToUpdate.Type,
                    PriceBuy = resourceToUpdate.PriceBuy,
                    PriceSell = resourceToUpdate.PriceSell,
                    LastUpdated = DateTime.UtcNow
                };

                dbContext.Resources.Add(newResource);
            }
        }
        
        await dbContext.SaveChangesAsync();
        var updatedResources = await dbContext.Resources
            .Select(r => new ResourceDto()
            {
                ResourceId = r.ResourceId,
                Name = r.Name,
                Code = r.Code,
                Type = r.Type,
                PriceBuy = r.PriceBuy,
                PriceSell = r.PriceSell,
                LastUpdated = r.LastUpdated
            })
            .ToListAsync();
        return TypedResults.Ok(updatedResources);
    }
}