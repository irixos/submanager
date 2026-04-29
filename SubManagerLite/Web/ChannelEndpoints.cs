using Microsoft.AspNetCore.Http.HttpResults;
using SubManagerLite.Application.Features.Channels.Interfaces;
using SubManagerLite.Application.Features.Channels.Models;

namespace SubManagerLite.Web;

public static class ChannelEndpoints
{
    public static RouteGroupBuilder MapChannelsApi(this RouteGroupBuilder group)
    {
        group.MapGet("/",
            async (IChannelService channelService, CancellationToken ct) =>
            {
                var response = await channelService.GetAllAsync(ct);
                return TypedResults.Ok(response);
            });
        
        group.MapPost("/",
            async Task<Results<Created<ChannelResponse>, Conflict<string>>>(
                CreateChannelRequest request, 
                IChannelService channelService, 
                CancellationToken ct) =>
            {
                var response = await channelService.CreateAsync(request, ct);
                return response is not null
                    ? TypedResults.Created($"/channels/{response.Id}", response)
                    : TypedResults.Conflict("Channel already exists");
            });
        
        group.MapPut("/{id:int}/categories", 
            async Task<Results<NoContent, NotFound<string>>> (
            int id,
            UpdateChannelCategoriesRequest request,
            IChannelService channelService,
            CancellationToken ct) =>
        {
            var response = await channelService.UpdateCategoriesAsync(id, request, ct);
            return response
                ? TypedResults.NoContent()
                : TypedResults.NotFound("Channel not found");
        });
        
        group.MapPut("/{id:int}/status", 
            async Task<Results<NoContent, NotFound<string>>> (
                int id,
                UpdateChannelStatusRequest request,
                IChannelService channelService,
                CancellationToken ct) =>
            {
                var response = await channelService.UpdateStatusAsync(id, request, ct);
                return response
                    ? TypedResults.NoContent()
                    : TypedResults.NotFound("Channel not found");
            });
        
        group.MapDelete("/{id:int}",
            async Task<Results<NoContent, NotFound<string>>> (
                int id,
                IChannelService channelService,
                CancellationToken ct) =>
            {
                var response = await channelService.DeleteAsync(id, ct);
                return response 
                    ? TypedResults.NoContent() 
                    : TypedResults.NotFound("Channel not found");
            });

        return group;
    }
}