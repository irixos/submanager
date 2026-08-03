using System.Threading.RateLimiting;
using Gridify;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using SubManager.Api.Application;
using SubManager.Api.Infrastructure;
using SubManager.Api.Infrastructure.Identity;
using SubManager.Api.Web;

var builder = WebApplication.CreateBuilder(args);

GridifyGlobalConfiguration.EnableEntityFrameworkCompatibilityLayer();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var path = context.Request.Path.Value?.TrimEnd('/').ToLowerInvariant();
        var isPasswordRecovery = HttpMethods.IsPost(context.Request.Method) &&
            path is "/forgotpassword" or "/resetpassword";

        if (!isPasswordRecovery)
            return RateLimitPartition.GetNoLimiter("unlimited");

        var address = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            $"password-recovery:{path}:{address}",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(15),
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            });
    });
});
builder.Services.AddAntiforgery(options => options.HeaderName = "X-XSRF-TOKEN");
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
app.UseRateLimiter();
app.UseAuthentication();
app.UseValidUserAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// Endpoints
var identityApi = app.MapGroup(string.Empty)
    .AddEndpointFilter<SingleUserRegistrationFilter>();

identityApi.MapIdentityApi<ApplicationUser>()
    .WithTags("Identity");

app.MapGet("/antiforgery/token", (
        HttpContext context,
        IAntiforgery antiforgery) =>
        TypedResults.Json(antiforgery.GetAndStoreTokens(context).RequestToken))
    .ExcludeFromDescription();

app.MapGroup("/identity")
    .WithTags("Identity")
    .MapIdentityEndpoints();

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

app.MapStaticAssets();
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program;
