using GreenPartySocietyAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace GreenPartySocietyAPI.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Event>(b =>
        {
            b.ToTable("Events");

            b.HasKey(e => e.Id);

            b.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(200);

            b.Property(e => e.Description)
                .HasMaxLength(4000);

            b.Property(e => e.Location)
                .HasMaxLength(300);

            b.Property(e => e.StartsAt)
                .IsRequired();

            b.Property(e => e.CreatedAt)
                .IsRequired();

            b.Property(e => e.UpdatedAt)
                .IsRequired();

            b.HasIndex(e => e.StartsAt);
        });
    }
}