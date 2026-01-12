using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LifecycleDashboard.Data;

/// <summary>
/// Design-time factory for creating LifecycleDbContext.
/// Used by EF Core tools for migrations.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<LifecycleDbContext>
{
    public LifecycleDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<LifecycleDbContext>();

        // Use the development connection string for migrations
        // This matches the connection string in appsettings.Development.json
        optionsBuilder.UseSqlServer(
            "Server=localhost,1433;Database=LifecycleDashboard;User Id=sa;Password=LifecycleDev123!;TrustServerCertificate=True;Encrypt=False");

        return new LifecycleDbContext(optionsBuilder.Options);
    }
}
