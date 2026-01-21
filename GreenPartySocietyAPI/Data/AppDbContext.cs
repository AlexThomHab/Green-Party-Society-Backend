using GreenPartySocietyAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace GreenPartySocietyAPI.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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

            b.HasIndex(e => e.StartsAtUtc);
        });
    }
}