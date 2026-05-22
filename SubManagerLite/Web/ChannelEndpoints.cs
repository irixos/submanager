using Microsoft.AspNetCore.Http.HttpResults;
using SubManagerLite.Application.Entities;
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

        group.MapGet("/{id:int}",
            async Task<Results<Ok<ChannelResponse>, NotFound>>(
                int id,
                IChannelService channelService, 
                CancellationToken ct) =>
            {
                var response = await channelService.GetAsync(id, ct);
                return response is not null
                    ? TypedResults.Ok(response)
                    : TypedResults.NotFound();
            });
        
        group.MapPost("/",
            async Task<Results<Created<ChannelResponse>, Conflict>>(
                CreateChannelRequest request, 
                IChannelService channelService, 
                CancellationToken ct) =>
            {
                var response = await channelService.CreateAsync(request, ct);
                return response is not null
                    ? TypedResults.Created($"/channels/{response.Id}", response)
                    : TypedResults.Conflict();
            });
        
        group.MapPatch("/{id:int}/categories", 
            async Task<Results<NoContent, NotFound>> (
            int id,
            UpdateChannelCategoriesRequest request,
            IChannelService channelService,
            CancellationToken ct) =>
        {
            var response = await channelService.UpdateCategoriesAsync(id, request, ct);
            return response
                ? TypedResults.NoContent()
                : TypedResults.NotFound();
        });
        
        group.MapPatch("/{id:int}/status", 
            async Task<Results<NoContent, NotFound>> (
                int id,
                UpdateChannelStatusRequest request,
                IChannelService channelService,
                CancellationToken ct) =>
            {
                var response = await channelService.UpdateStatusAsync(id, request, ct);
                return response
                    ? TypedResults.NoContent()
                    : TypedResults.NotFound();
            });
        
        group.MapDelete("/{id:int}",
            async Task<Results<NoContent, NotFound>> (
                int id,
                IChannelService channelService,
                CancellationToken ct) =>
            {
                var response = await channelService.DeleteAsync(id, ct);
                return response 
                    ? TypedResults.NoContent() 
                    : TypedResults.NotFound();
            });

        return group;
    }
}