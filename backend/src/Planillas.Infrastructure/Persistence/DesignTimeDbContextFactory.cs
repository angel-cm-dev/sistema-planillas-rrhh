using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Planillas.Infrastructure.Persistence;

/// <summary>
/// Usada solo por las herramientas de EF Core (dotnet ef migrations add ...). Las migraciones
/// se generan para SQL Server, el motor de producción. No necesita una BD real para generarlas.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=localhost,1433;Database=PlanillasRrhh;User Id=sa;Password=Placeholder_123;TrustServerCertificate=True")
            .Options;
        return new AppDbContext(options);
    }
}
