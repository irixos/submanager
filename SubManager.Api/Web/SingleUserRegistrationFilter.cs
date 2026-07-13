using System.Data;
using Microsoft.EntityFrameworkCore;
using SubManager.Api.Infrastructure;

namespace SubManager.Api.Web;

public sealed class SingleUserRegistrationFilter(ApplicationDbContext db) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var request = context.HttpContext.Request;
        var path = request.Path.Value;
        var isRegistration = path is not null &&
            (path.Equals("/register", StringComparison.OrdinalIgnoreCase) ||
             path.Equals("/register/", StringComparison.OrdinalIgnoreCase));

        if (!HttpMethods.IsPost(request.Method) || !isRegistration)
            return await next(context);

        var cancellationToken = context.HttpContext.RequestAborted;
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        if (await db.Users.AnyAsync(cancellationToken))
            return TypedResults.Conflict();

        var result = await next(context);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }
}
