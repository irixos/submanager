using Microsoft.AspNetCore.Http.HttpResults;
using SubManagerLite.Application.Features.Videos.Interfaces;
using SubManagerLite.Application.Features.Videos.Models;

namespace SubManagerLite.Web;

public static class VideoEndpoints
{
   public static RouteGroupBuilder MapVideosApi(this RouteGroupBuilder group)
    {
        group.MapGet("/",
            async (IVideoService videoService, CancellationToken ct) =>
            {
                var response = await videoService.GetAllAsync(ct);
                return TypedResults.Ok(response);
            });

        group.MapGet("/{id:int}",
            async Task<Results<Ok<VideoResponse>, NotFound<string>>>(
                int id,
                IVideoService videoService, 
                CancellationToken ct) =>
            {
                var response = await videoService.GetAsync(id, ct);
                return response is not null
                    ? TypedResults.Ok(response)
                    : TypedResults.NotFound("Video not found");
            });
        
        group.MapPost("/refresh",
            async Task<Results<Ok<List<VideoResponse>>, Conflict<string>>>(
                IVideoRefreshService videoRefreshService, 
                CancellationToken ct) =>
            {
                var response = await videoRefreshService.RefreshAllAsync(ct);
                return !response.IsAlreadyRunning
                    ? TypedResults.Ok(response.Response)
                    : TypedResults.Conflict("Refresh already in progress");
            });
        
        group.MapPatch("/{id:int}/watched-date", 
            async Task<Results<NoContent, NotFound<string>>> (
            int id,
            UpdateVideoWatchedDateRequest request,
            IVideoService videoService,
            CancellationToken ct) =>
        {
            var response = await videoService.UpdateWatchedDateAsync(id, request, ct);
            return response
                ? TypedResults.NoContent()
                : TypedResults.NotFound("Video not found");
        });

        return group;
    } 
}