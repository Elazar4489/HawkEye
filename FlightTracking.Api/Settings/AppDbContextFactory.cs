using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FlightTracking.Api.Settings
{
    // Lets "dotnet ef migrations add" build the model without a running MySQL
    // (ServerVersion.AutoDetect in Program.cs needs a live connection).
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var connectionString =
                Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                ?? "Server=localhost;Port=3306;Database=flight_tracking_db;Uid=root;Pwd=root;";

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseMySql(
                    connectionString,
                    new MySqlServerVersion(new Version(8, 0, 36)),
                    mySqlOptions => mySqlOptions.UseNetTopologySuite())
                .Options;

            return new AppDbContext(options);
        }
    }
}
