using Microsoft.EntityFrameworkCore;
using ReservationService.Models;

namespace ReservationService.Data;

public class ReservationDbContext : DbContext
{
    public ReservationDbContext(DbContextOptions<ReservationDbContext> options) : base(options)
    {
    }

    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<Passenger> Passengers => Set<Passenger>();
    public DbSet<Cancellation> Cancellations => Set<Cancellation>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Reservations configuration
        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.HasKey(r => r.ReservationId);
            entity.HasIndex(r => r.PNR).IsUnique();
            entity.HasIndex(r => r.UserId);
            entity.Property(r => r.PNR).IsRequired().HasMaxLength(20);
            entity.Property(r => r.JourneyDate).HasColumnType("date").IsRequired();
            entity.Property(r => r.TotalFare).HasColumnType("decimal(10,2)").IsRequired();
            entity.Property(r => r.BookingStatus).IsRequired().HasMaxLength(20);

            entity.HasMany(r => r.Passengers)
                  .WithOne(p => p.Reservation)
                  .HasForeignKey(p => p.ReservationId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.Cancellation)
                  .WithOne(c => c.Reservation)
                  .HasForeignKey<Cancellation>(c => c.ReservationId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Passengers configuration
        modelBuilder.Entity<Passenger>(entity =>
        {
            entity.HasKey(p => p.PassengerId);
            entity.Property(p => p.PassengerName).IsRequired().HasMaxLength(100);
            entity.Property(p => p.Gender).IsRequired().HasMaxLength(10);
        });

        // Cancellations configuration
        modelBuilder.Entity<Cancellation>(entity =>
        {
            entity.HasKey(c => c.CancellationId);
            entity.HasIndex(c => c.ReservationId).IsUnique();
            entity.Property(c => c.RefundAmount).HasColumnType("decimal(10,2)").IsRequired();
        });

        // IdempotencyRecords configuration
        modelBuilder.Entity<IdempotencyRecord>(entity =>
        {
            entity.HasKey(i => i.Id);
            entity.HasIndex(i => new { i.UserId, i.IdempotencyKey }).IsUnique();
            entity.Property(i => i.IdempotencyKey).IsRequired().HasMaxLength(100);
            entity.Property(i => i.PayloadHash).IsRequired().HasMaxLength(64);
            entity.Property(i => i.Status).IsRequired().HasMaxLength(20);
        });
    }
}
