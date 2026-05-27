using Gridify;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SubManager.Api.Application.Features.Channels.Interfaces;
using SubManager.Api.Application.Features.Channels.Models;

namespace SubManager.Api.Web;

public static class ChannelEndpoints
{
    public static RouteGroupBuilder MapChannelsApi(this RouteGroupBuilder group)
    {
        group.MapGet("/",
            async (
                [AsParameters] GridifyQuery query,
                IChannelService channelService,
                CancellationToken ct) =>
            {
                var response = await channelService.GetAllAsync(query, ct);
                return TypedResults.Ok(response);
            })
            .WithName("GetChannels")
            .WithSummary("List channels")
            .WithDescription("Supports pagination and optional filtering & sorting.");
            

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
            })
            .WithName("GetChannelById")
            .WithSummary("Get channel by ID")
            .ProducesProblem(StatusCodes.Status404NotFound);
        
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
            })
            .WithName("CreateChannel")
            .WithSummary("Create channel")
            .WithDescription("Creates a new channel from the provided YouTube channel URL.")
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/import",
            async Task<Results<Ok<ImportChannelsResponse>, BadRequest>> (
                [FromForm] ImportChannelsRequest request,
                IChannelService channelService,
                CancellationToken ct) =>
            {
                var response = await channelService.ImportAsync(request, ct);
                return response is not null
                    ? TypedResults.Ok(response)
                    : TypedResults.BadRequest();
                })
            .WithName("ImportChannels")
            .WithSummary("Import channels")
            .WithDescription("Imports channels from a plaintext file. Scans the file for YouTube channel URLs.")
            .ProducesProblem(StatusCodes.Status400BadRequest);
        
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
            })
            .WithName("UpdateChannelCategories")
            .WithSummary("Update channel categories")
            .ProducesProblem(StatusCodes.Status404NotFound);
        
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
            })
            .WithName("UpdateChannelStatus")
            .WithSummary("Update channel status")
            .WithDescription("Sets a channel as active or inactive. Inactive channels will not have their feeds " +
                             "refreshed.")
            .ProducesProblem(StatusCodes.Status404NotFound);
        
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
            })
            .WithName("DeleteChannel")
            .WithSummary("Delete channel")
            .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }
}