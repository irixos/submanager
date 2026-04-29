using Microsoft.AspNetCore.Http.HttpResults;
using SubManagerLite.Application.Features.Categories.Interfaces;
using SubManagerLite.Application.Features.Categories.Models;

namespace SubManagerLite.Web;

public static class CategoryEndpoints
{
    public static RouteGroupBuilder MapCategoriesApi(this RouteGroupBuilder group)
    {
        group.MapGet("/",
            async (ICategoryService categoryService, CancellationToken ct) =>
            {
                var response = await categoryService.GetAllAsync(ct);
                return TypedResults.Ok(response);
            });
        
        group.MapGet("/{id:int}",
            async Task<Results<Ok<CategoryResponse>, NotFound<string>>>(
                int id,
                ICategoryService categoryService,
                CancellationToken ct) =>
            {
                var response = await categoryService.GetAsync(id, ct);
                return response is not null
                    ? TypedResults.Ok(response)
                    : TypedResults.NotFound("Category not found");
            });
        
        group.MapPost("/",
            async Task<Results<Created<CategoryResponse>, Conflict<string>>>(
                CreateCategoryRequest request, 
                ICategoryService categoryService, 
                CancellationToken ct) =>
            {
                var response = await categoryService.CreateAsync(request, ct);
                return response is not null
                    ? TypedResults.Created($"/categories/{response.Id}", response)
                    : TypedResults.Conflict("Category already exists");
            });
        
        group.MapPut("/{id:int}", 
            async Task<Results<NoContent, NotFound<string>>> (
            int id,
            UpdateCategoryRequest request,
            ICategoryService categoryService,
            CancellationToken ct) =>
        {
            var response = await categoryService.UpdateAsync(id, request, ct);
            return response
                ? TypedResults.NoContent()
                : TypedResults.NotFound("Category not found");
        });
        
        group.MapDelete("/{id:int}",
            async Task<Results<NoContent, NotFound<string>>> (
                int id,
                ICategoryService categoryService,
                CancellationToken ct) =>
            {
                var response = await categoryService.DeleteAsync(id, ct);
                return response 
                    ? TypedResults.NoContent() 
                    : TypedResults.NotFound("Category not found");
            });

        return group;
    }
}