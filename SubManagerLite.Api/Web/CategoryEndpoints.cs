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
            })
            .WithName("GetCategories")
            .WithSummary("List categories")
            .WithDescription("Supports pagination and optional sorting & filtering.");
        
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
            })
            .WithName("GetCategoryById")
            .WithSummary("Get category by ID")
            .ProducesProblem(StatusCodes.Status404NotFound);
        
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
            })
            .WithName("CreateCategory")
            .WithSummary("Create category")
            .ProducesProblem(StatusCodes.Status409Conflict);
        
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
        })
            .WithName("UpdateCategory")
            .WithSummary("Update category")
            .ProducesProblem(StatusCodes.Status404NotFound);
        
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
            })
            .WithName("DeleteCategory")
            .WithSummary("Delete category")
            .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }
}