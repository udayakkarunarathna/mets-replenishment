using Microsoft.EntityFrameworkCore;
using METS.Api.Models;

namespace METS.Api.Data;

public class MetsDbContext(DbContextOptions<MetsDbContext> options) : DbContext(options)
{
    public DbSet<ReplenishmentRequest> ReplenishmentRequests => Set<ReplenishmentRequest>();
    public DbSet<RequestLineItem> RequestLineItems => Set<RequestLineItem>();
    public DbSet<StockLocation> StockLocations => Set<StockLocation>();
    public DbSet<AppUser> Users => Set<AppUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ReplenishmentRequest configuration
        modelBuilder.Entity<ReplenishmentRequest>(e =>
        {
            e.HasOne(r => r.StockLocation)
                .WithMany(l => l.Requests)
                .HasForeignKey(r => r.StockLocationId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(r => r.CreatedBy)
                .WithMany(u => u.CreatedRequests)
                .HasForeignKey(r => r.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(r => r.ReviewedBy)
                .WithMany(u => u.ReviewedRequests)
                .HasForeignKey(r => r.ReviewedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            e.Property(r => r.Priority)
                .HasConversion<string>();

            e.Property(r => r.Status)
                .HasConversion<string>();

            e.Property(r => r.ValidationStatus)
                .HasConversion<string>();
        });

        modelBuilder.Entity<AppUser>(e =>
        {
            e.Property(u => u.Role)
                .HasConversion<string>();
        });
    }
}
