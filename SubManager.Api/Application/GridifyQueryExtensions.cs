using Gridify;

namespace SubManager.Api.Application;

public static class GridifyQueryExtensions
{
    public static GridifyQuery ClampPageSize(this GridifyQuery query)
    {
        const int maxPageSize = 100;
        
        query.PageSize = query.PageSize <= 0
            ? GridifyGlobalConfiguration.DefaultPageSize
            : Math.Min(query.PageSize, maxPageSize);

        return query;
    }
}