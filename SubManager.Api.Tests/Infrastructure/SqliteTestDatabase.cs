using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubManager.Api.Application.Entities;
using SubManager.Api.Infrastructure;

namespace SubManager.Api.Tests.Infrastructure;

internal sealed class SqliteTestDatabase : IDisposable
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");

    public SqliteTestDatabase(params IInterceptor[] interceptors)
    {
        connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .ReplaceService<IModelCustomizer, SqliteTestModelCustomizer>()
            .AddInterceptors(interceptors)
            .Options;

        Context = new ApplicationDbContext(options);
        Context.Database.EnsureCreated();
    }

    public ApplicationDbContext Context { get; }

    public void Dispose()
    {
        Context.Dispose();
        connection.Dispose();
    }
}

internal sealed class SqliteTestModelCustomizer(ModelCustomizerDependencies dependencies)
    : ModelCustomizer(dependencies)
{
    public override void Customize(ModelBuilder modelBuilder, DbContext context)
    {
        base.Customize(modelBuilder, context);
        ConfigureGeneratedDate(modelBuilder.Entity<Channel>().Property(channel => channel.AddedDate));
        ConfigureGeneratedDate(modelBuilder.Entity<Video>().Property(video => video.AddedDate));
        ConfigureGeneratedDate(modelBuilder.Entity<Video>().Property(video => video.MetadataLastRefreshedAt));
        ConfigureDateTimeOffset(modelBuilder.Entity<Video>().Property(video => video.PublishedDate));
    }

    private static void ConfigureGeneratedDate(
        PropertyBuilder<DateTimeOffset> property) =>
        property
            .HasDefaultValueSql(null)
            .HasDefaultValue(DateTimeOffset.UnixEpoch);

    private static void ConfigureDateTimeOffset(
        PropertyBuilder<DateTimeOffset> property) => property.HasConversion<long>();
}
