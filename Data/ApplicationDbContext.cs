using RentalHub.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace RentalHub.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Property> Properties { get; set; }
    public DbSet<PropertyImage> PropertyImages { get; set; }
    public DbSet<Reservation> Reservations { get; set; }
    public DbSet<Wishlist> Wishlists { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<KycVerification> KycVerifications { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Property>()
            .HasMany(p => p.Images)
            .WithOne(i => i.Property)
            .HasForeignKey(i => i.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Property>()
            .HasMany(p => p.Reservations)
            .WithOne(r => r.Property)
            .HasForeignKey(r => r.PropertyId);

        builder.Entity<ApplicationUser>()
            .HasMany<Reservation>()
            .WithOne(r => r.User)
            .HasForeignKey(r => r.UserId);

        builder.Entity<KycVerification>()
            .HasOne(k => k.User)
            .WithOne()
            .HasForeignKey<KycVerification>(k => k.UserId);
    }
}
