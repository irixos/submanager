using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using SubManager.Api.Application;
using SubManager.Api.Infrastructure;
using SubManager.Api.Infrastructure.Identity;
using SubManager.Api.Web;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorClient", policy =>
    {
        policy.WithOrigins("https://localhost:7122")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = ctx =>
    {
        ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier;
    };
});

builder.Services.AddValidation();
builder.Services.AddHttpClient();
builder.Services.AddAuthorization();
builder.Services.AddAntiforgery();
builder.Services.AddIdentityApiEndpoints<ApplicationUser>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
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
app.UseCors("BlazorClient");
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// Endpoints
app.MapIdentityApi<ApplicationUser>()
    .WithTags("Identity");

app.MapGroup("/channels")
    .WithTags("Channels")
    .RequireAuthorization()
    .MapChannelsApi();

app.MapGroup("/categories")
    .WithTags("Categories")
    .RequireAuthorization()
    .MapCategoriesApi();

app.MapGroup("/videos")
    .WithTags("Videos")
    .RequireAuthorization()
    .MapVideosApi();

app.Run();