using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using SubManagerLite.Application;
using SubManagerLite.Infrastructure;
using SubManagerLite.Infrastructure.Identity;
using SubManagerLite.Web;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SubManagerLite")));
    options.UseSqlServer(builder.Configuration.GetConnectionString("SubManagerLite.Api")));

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

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapIdentityApi<ApplicationUser>()
    .WithTags("Identity");

// Endpoints
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