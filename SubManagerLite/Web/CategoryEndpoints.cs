using Gridify;
using Microsoft.AspNetCore.Http.HttpResults;
using SubManagerLite.Application.Features.Categories.Interfaces;
using SubManagerLite.Application.Features.Categories.Models;

namespace SubManagerLite.Web;

public static class CategoryEndpoints
{
    public static RouteGroupBuilder MapCategoriesApi(this RouteGroupBuilder group)
    {
        group.MapGet("/",
            async (
                [AsParameters] GridifyQuery query, 
                ICategoryService categoryService, 
                CancellationToken ct) =>
            {
                var response = await categoryService.GetAllAsync(query, ct);
                return TypedResults.Ok(response);
            });
        
        group.MapGet("/{id:int}",
            async Task<Results<Ok<CategoryResponse>, NotFound>>(
                int id,
                ICategoryService categoryService,
                CancellationToken ct) =>
            {
                var response = await categoryService.GetAsync(id, ct);
                return response is not null
                    ? TypedResults.Ok(response)
                    : TypedResults.NotFound();
            });
        
        group.MapPost("/",
            async Task<Results<Created<CategoryResponse>, Conflict>>(
                CreateCategoryRequest request, 
                ICategoryService categoryService, 
                CancellationToken ct) =>
            {
                var response = await categoryService.CreateAsync(request, ct);
                return response is not null
                    ? TypedResults.Created($"/categories/{response.Id}", response)
                    : TypedResults.Conflict();
            });
        
        group.MapPut("/{id:int}", 
            async Task<Results<NoContent, NotFound>> (
            int id,
            UpdateCategoryRequest request,
            ICategoryService categoryService,
            CancellationToken ct) =>
        {
            var response = await categoryService.UpdateAsync(id, request, ct);
            return response
                ? TypedResults.NoContent()
                : TypedResults.NotFound();
        });
        
        group.MapDelete("/{id:int}",
            async Task<Results<NoContent, NotFound>> (
                int id,
                ICategoryService categoryService,
                CancellationToken ct) =>
            {
                var response = await categoryService.DeleteAsync(id, ct);
                return response 
                    ? TypedResults.NoContent() 
                    : TypedResults.NotFound();
            });

        return group;
    }
}