using Microsoft.EntityFrameworkCore;
using TrainService.Models;

namespace TrainService.Data;

public class TrainDbContext : DbContext
{
    public TrainDbContext(DbContextOptions<TrainDbContext> options) : base(options)
    {
    }

    public DbSet<Train> Trains => Set<Train>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Train>(entity =>
        {
            entity.HasKey(t => t.TrainId);

            entity.Property(t => t.TrainName).IsRequired().HasMaxLength(100);
            entity.Property(t => t.Source).IsRequired().HasMaxLength(100);
            entity.Property(t => t.Destination).IsRequired().HasMaxLength(100);
            entity.Property(t => t.JourneyDate).HasColumnType("date").IsRequired();
            entity.Property(t => t.Fare).HasColumnType("decimal(10,2)").IsRequired();
            entity.Property(t => t.Status).IsRequired().HasMaxLength(20);

            // Optimistic concurrency
            entity.Property(t => t.RowVersion).IsRowVersion();

            // Search index for fast lookup
            entity.HasIndex(t => new { t.Source, t.Destination, t.JourneyDate });
        });
    }
}
