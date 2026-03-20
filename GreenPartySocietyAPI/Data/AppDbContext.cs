using GreenPartySocietyAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace GreenPartySocietyAPI.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<AppNotification> Notifications => Set<AppNotification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(b =>
        {
            b.ToTable("Users");
            b.HasKey(u => u.Id);
            b.Property(u => u.Email).IsRequired().HasMaxLength(300);
            b.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
            b.Property(u => u.LastName).IsRequired().HasMaxLength(100);
            b.Property(u => u.Role).IsRequired().HasMaxLength(50).HasDefaultValue("Member");
            b.Property(u => u.Bio).HasMaxLength(1000).HasDefaultValue("");
            b.Property(u => u.SubstackUrl).HasMaxLength(500).HasDefaultValue("");
            b.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<Event>(b =>
        {
            b.ToTable("Events");
            b.HasKey(e => e.Id);

            b.Property(e => e.Title).IsRequired().HasMaxLength(200);
            b.Property(e => e.Description).HasMaxLength(4000);
            b.Property(e => e.Location).HasMaxLength(300);

            b.Property(e => e.StartsAtUtc).IsRequired();
            b.Property(e => e.EndsAtUtc);

            b.Property(e => e.CreatedAtUtc).IsRequired();
            b.Property(e => e.UpdatedAtUtc).IsRequired();

            b.Property(e => e.Source).IsRequired().HasMaxLength(50).HasDefaultValue("manual");
            b.Property(e => e.ExternalId).HasMaxLength(200);

            b.HasIndex(e => e.StartsAtUtc);
            b.HasIndex(e => e.ExternalId);
        });

        modelBuilder.Entity<AppNotification>(b =>
        {
            b.ToTable("Notifications");
            b.HasKey(n => n.Id);
            b.Property(n => n.Type).IsRequired().HasMaxLength(100);
            b.Property(n => n.Title).IsRequired().HasMaxLength(500);
            b.Property(n => n.Message).HasMaxLength(2000);
            b.Property(n => n.CreatedAtUtc).IsRequired();
        });
    }
}
