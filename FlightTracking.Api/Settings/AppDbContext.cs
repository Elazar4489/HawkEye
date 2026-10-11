using Microsoft.EntityFrameworkCore;
using FlightTracking.Api.Models;

namespace FlightTracking.Api.Settings
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<CustomerPriority> CustomerPriorities { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<CustomerPriority>()
                .HasOne(cp => cp.User)
                .WithMany(u => u.CustomerPriorities)
                .HasForeignKey(cp => cp.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}