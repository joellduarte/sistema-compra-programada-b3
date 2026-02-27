using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CompraProgramada.Infrastructure.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<CompraProgramadaDbContext>
{
    public CompraProgramadaDbContext CreateDbContext(string[] args)
    {
        var connectionString = "Server=localhost;Port=3307;Database=compra_programada;User=compra_user;Password=compra_pass;";

        var optionsBuilder = new DbContextOptionsBuilder<CompraProgramadaDbContext>();
        optionsBuilder.UseMySql(
            connectionString,
            new MySqlServerVersion(new Version(8, 0, 0)),
            mysqlOptions => mysqlOptions.MigrationsAssembly(
                typeof(CompraProgramadaDbContext).Assembly.FullName));

        return new CompraProgramadaDbContext(optionsBuilder.Options);
    }
}
