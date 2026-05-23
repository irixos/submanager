using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using SubManagerLite.Application;
using SubManagerLite.Infrastructure;
using SubManagerLite.Web;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SubManagerLite")));

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = ctx =>
    {
        ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier;
    };
});

builder.Services.AddValidation();
builder.Services.AddHttpClient();
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseHttpsRedirection();

// Endpoints
app.MapGroup("/channels")
    .WithTags("Channels")
    .MapChannelsApi();

app.MapGroup("/categories")
    .WithTags("Categories")
    .MapCategoriesApi();

app.MapGroup("/videos")
    .WithTags("Videos")
    .MapVideosApi();

app.Run();