using FlightMatching.Models;
using Microsoft.EntityFrameworkCore;

namespace FlightMatching.Settings;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<CustomerPriority> CustomerPriorities
        => Set<CustomerPriority>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<CustomerPriority>()
            .HasOne(cp => cp.User)
            .WithMany(user => user.CustomerPriorities)
            .HasForeignKey(cp => cp.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}