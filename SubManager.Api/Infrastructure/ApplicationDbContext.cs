using System.Reflection;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SubManager.Api.Application.Entities;
using SubManager.Api.Infrastructure.Identity;

namespace SubManager.Api.Infrastructure;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Channel> Channels => Set<Channel>();
    public DbSet<Video> Videos => Set<Video>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Call base class implementation to configure Identity entities
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
