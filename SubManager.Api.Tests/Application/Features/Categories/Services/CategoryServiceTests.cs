using Gridify;
using Microsoft.EntityFrameworkCore;
using SubManager.Api.Application.Entities;
using SubManager.Api.Application.Features.Categories.Interfaces;
using SubManager.Api.Application.Features.Categories.Models;
using SubManager.Api.Application.Features.Categories.Services;
using SubManager.Api.Tests.Infrastructure;
using Xunit;

namespace SubManager.Api.Tests.Application.Features.Categories.Services;

public sealed class CategoryServiceTests
{
    [Fact]
    public async Task GetAllAsync_SortedPage_ReturnsProjectedPage()
    {
        using var database = new SqliteTestDatabase();
        database.Context.Categories.AddRange(
            new Category { Name = "Alpha", Color = "#111111" },
            new Category { Name = "Beta", Color = "#222222" });
        await database.Context.SaveChangesAsync();
        var service = new CategoryService(database.Context);

        var result = await service.GetAllAsync(
            new GridifyQuery { Page = 1, PageSize = 1, OrderBy = "Name desc" },
            CancellationToken.None);
        var filtered = await service.GetAllAsync(
            new GridifyQuery { Filter = "Name=Alpha" },
            CancellationToken.None);

        Assert.Equal(2, result.Count);
        var category = Assert.Single(result.Data);
        Assert.Equal("Beta", category.Name);
        Assert.Equal("#222222", category.Color);
        Assert.Equal(1, filtered.Count);
        Assert.Equal("Alpha", Assert.Single(filtered.Data).Name);
    }

    [Fact]
    public async Task GetAsync_FoundAndMissing_ReturnsExpectedProjection()
    {
        using var database = new SqliteTestDatabase();
        var entity = new Category { Name = "Technology", Color = "#123456" };
        database.Context.Add(entity);
        await database.Context.SaveChangesAsync();
        var service = new CategoryService(database.Context);

        var found = await service.GetAsync(entity.Id, CancellationToken.None);
        var missing = await service.GetAsync(entity.Id + 1, CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal(entity.Id, found.Id);
        Assert.Equal("Technology", found.Name);
        Assert.Equal("#123456", found.Color);
        Assert.Null(missing);
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_PersistsAndMapsCategory()
    {
        using var database = new SqliteTestDatabase();
        var service = new CategoryService(database.Context);

        var result = await service.CreateAsync(
            new CreateCategoryRequest { Name = "Science", Color = "#ABCDEF" },
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal("Science", result.Name);
        Assert.Equal("#ABCDEF", result.Color);
        Assert.Equal(1, await database.Context.Categories.CountAsync());
    }

    [Fact]
    public async Task UpdateAsync_PartialUpdates_SetClearAndPreserveFields()
    {
        using var database = new SqliteTestDatabase();
        var entity = new Category { Name = "Original", Color = "#111111" };
        database.Context.Add(entity);
        await database.Context.SaveChangesAsync();
        var service = new CategoryService(database.Context);

        var renamed = await service.UpdateAsync(
            entity.Id,
            new UpdateCategoryRequest { Name = "Renamed" },
            CancellationToken.None);
        Assert.Equal(CategoryUpdateResult.Updated, renamed);
        Assert.Equal("Renamed", entity.Name);
        Assert.Equal("#111111", entity.Color);

        var recolored = await service.UpdateAsync(
            entity.Id,
            new UpdateCategoryRequest { Color = "#222222" },
            CancellationToken.None);
        Assert.Equal(CategoryUpdateResult.Updated, recolored);
        Assert.Equal("#222222", entity.Color);

        var cleared = await service.UpdateAsync(
            entity.Id,
            new UpdateCategoryRequest { ClearColor = true },
            CancellationToken.None);
        Assert.Equal(CategoryUpdateResult.Updated, cleared);
        Assert.Null(entity.Color);
    }

    [Fact]
    public async Task MissingAndDeletePaths_ReturnExpectedResults()
    {
        using var database = new SqliteTestDatabase();
        var entity = new Category { Name = "Delete me" };
        database.Context.Add(entity);
        await database.Context.SaveChangesAsync();
        var service = new CategoryService(database.Context);

        var missingUpdate = await service.UpdateAsync(
            entity.Id + 1,
            new UpdateCategoryRequest { Name = "Nope" },
            CancellationToken.None);
        var missingDelete = await service.DeleteAsync(entity.Id + 1, CancellationToken.None);
        var deleted = await service.DeleteAsync(entity.Id, CancellationToken.None);

        Assert.Equal(CategoryUpdateResult.NotFound, missingUpdate);
        Assert.False(missingDelete);
        Assert.True(deleted);
        Assert.Empty(await database.Context.Categories.ToListAsync());
    }
}
