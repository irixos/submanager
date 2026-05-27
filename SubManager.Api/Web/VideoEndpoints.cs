using Gridify;
using Microsoft.AspNetCore.Http.HttpResults;
using SubManager.Api.Application.Features.Videos.Interfaces;
using SubManager.Api.Application.Features.Videos.Models;

namespace SubManager.Api.Web;

public static class VideoEndpoints
{
   public static RouteGroupBuilder MapVideosApi(this RouteGroupBuilder group)
    {
        group.MapGet("/",
            async (
                [AsParameters] GridifyQuery query, 
                IVideoService videoService, 
                CancellationToken ct) =>
            {
                var response = await videoService.GetAllAsync(query, ct);
                return TypedResults.Ok(response);
            })
            .WithName("GetVideos")
            .WithSummary("List videos")
            .WithDescription("Supports pagination and optional filtering & sorting.");

        group.MapGet("/{id:int}",
            async Task<Results<Ok<VideoResponse>, NotFound>>(
                int id,
                IVideoService videoService, 
                CancellationToken ct) =>
            {
                var response = await videoService.GetAsync(id, ct);
                return response is not null
                    ? TypedResults.Ok(response)
                    : TypedResults.NotFound();
            })
            .WithName("GetVideoById")
            .WithSummary("Get video by ID")
            .ProducesProblem(StatusCodes.Status404NotFound);
        
        group.MapPost("/refresh",
            async Task<Results<Ok<IReadOnlyList<VideoResponse>>, Conflict>>(
                IVideoRefreshService videoRefreshService, 
                CancellationToken ct) =>
            {
                var response = await videoRefreshService.RefreshAllAsync(ct);
                return !response.IsAlreadyRunning
                    ? TypedResults.Ok(response.Response)
                    : TypedResults.Conflict();
            })
            .WithName("RefreshVideos")
            .WithSummary("Refresh feeds for active channels")
            .WithDescription("Refreshes the video feeds for all active channels. Returns a list of videos with " +
                             "refreshed metadata, along with any new videos, with a 15 video feed window. Duration " +
                             "metadata ingest for new videos is queued separately as a background job. Only one " +
                             "active refresh operation can be run at a time.")
            .ProducesProblem(StatusCodes.Status409Conflict);
        
        group.MapPatch("/{id:int}/watched-date", 
            async Task<Results<NoContent, NotFound>> (
            int id,
            UpdateVideoWatchedDateRequest request,
            IVideoService videoService,
            CancellationToken ct) =>
        {
            var response = await videoService.UpdateWatchedDateAsync(id, request, ct);
            return response
                ? TypedResults.NoContent()
                : TypedResults.NotFound();
        })
            .WithName("UpdateVideoWatchedDate")
            .WithSummary("Update video watched date")
            .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    } 
}
