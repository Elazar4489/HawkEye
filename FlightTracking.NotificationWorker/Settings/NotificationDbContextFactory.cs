using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FlightTracking.NotificationWorker.Settings
{
    // Lets "dotnet ef migrations add" build the model without a running MySQL.
    public class NotificationDbContextFactory : IDesignTimeDbContextFactory<NotificationDbContext>
    {
        public NotificationDbContext CreateDbContext(string[] args)
        {
            var connectionString =
                Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                ?? "Server=localhost;Port=3306;Database=flight_tracking_db;Uid=root;Pwd=root;";

            var options = new DbContextOptionsBuilder<NotificationDbContext>()
                .UseMySql(
                    connectionString,
                    new MySqlServerVersion(new Version(8, 0, 36)),
                    mySqlOptions => mySqlOptions.UseNetTopologySuite())
                .Options;

            return new NotificationDbContext(options);
        }
    }
}
