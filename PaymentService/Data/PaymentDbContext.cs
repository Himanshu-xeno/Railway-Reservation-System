using Microsoft.EntityFrameworkCore;
using PaymentService.Models;

namespace PaymentService.Data;

public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options)
    {
    }

    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(p => p.PaymentId);
            entity.HasIndex(p => p.TransactionId).IsUnique();
            entity.HasIndex(p => p.ReservationId);
            entity.Property(p => p.Amount).HasColumnType("decimal(10,2)").IsRequired();
            entity.Property(p => p.PaymentType).IsRequired().HasMaxLength(20);
            entity.Property(p => p.PaymentStatus).IsRequired().HasMaxLength(20);
            entity.Property(p => p.TransactionId).IsRequired().HasMaxLength(50);
        });
    }
}
