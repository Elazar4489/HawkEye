using Microsoft.EntityFrameworkCore;
using FlightTracking.NotificationWorker.Models;

namespace FlightTracking.NotificationWorker.Settings
{
    public class NotificationDbContext : DbContext
    {
        public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<CustomerPriority> CustomerPriorities { get; set; }

        public DbSet<NotificationHistoryItem> NotificationHistories { get; set; }
        public DbSet<UserConnection> UserConnections { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users", tb => tb.ExcludeFromMigrations());
            });

            modelBuilder.Entity<CustomerPriority>(entity =>
            {
                entity.ToTable("CustomerPriorities", tb => tb.ExcludeFromMigrations());
            });
            modelBuilder.Entity<UserConnection>(entity =>
            {
                entity.ToTable("UserConnections", tb => tb.ExcludeFromMigrations());
            });
            modelBuilder.Entity<CustomerPriority>()
                .HasOne(cp => cp.User)
                .WithMany(u => u.CustomerPriorities)
                .HasForeignKey(cp => cp.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<NotificationHistoryItem>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<UserConnection>()
                .HasOne<User>() 
                .WithMany()
                .HasForeignKey(uc => uc.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}