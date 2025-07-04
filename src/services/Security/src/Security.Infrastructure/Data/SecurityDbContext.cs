using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Security.Domain.Entities;

namespace Security.Infrastructure.Data;

/// <summary>
/// Database context for the Security microservice using Entity Framework Identity
/// </summary>
public class SecurityDbContext : IdentityDbContext<ApplicationUser>
{
    public SecurityDbContext(DbContextOptions<SecurityDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Refresh tokens for secure token management
    /// </summary>
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        ConfigureApplicationUser(builder);
        ConfigureRefreshToken(builder);
    }

    private static void ConfigureApplicationUser(ModelBuilder builder)
    {
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.ClientId)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            entity.Property(e => e.FailedLoginAttempts)
                .IsRequired()
                .HasDefaultValue(0);

            // Index for performance
            entity.HasIndex(e => e.ClientId)
                .IsUnique();

            entity.HasIndex(e => e.IsActive);
        });
    }

    private static void ConfigureRefreshToken(ModelBuilder builder)
    {
        builder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");

            ConfigureRefreshTokenProperties(entity);
            ConfigureRefreshTokenRelationships(entity);
            ConfigureRefreshTokenIndexes(entity);
        });
    }

    private static void ConfigureRefreshTokenProperties(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<RefreshToken> entity)
    {
        entity.HasKey(e => e.Token);

        entity.Property(e => e.Token)
            .IsRequired()
            .HasMaxLength(256);

        entity.Property(e => e.JwtId)
            .IsRequired()
            .HasMaxLength(128);

        entity.Property(e => e.UserId)
            .IsRequired()
            .HasMaxLength(450); // Same as Identity User ID

        entity.Property(e => e.ExpiryDate)
            .IsRequired();

        entity.Property(e => e.IsRevoked)
            .IsRequired()
            .HasDefaultValue(false);

        entity.Property(e => e.CreatedByIp)
            .HasMaxLength(45); // IPv6 max length

        entity.Property(e => e.RevokedByIp)
            .HasMaxLength(45);

        entity.Property(e => e.RevocationReason)
            .HasMaxLength(500);

        entity.Property(e => e.ReplacedByToken)
            .HasMaxLength(256);

        entity.Property(e => e.DeviceInfo)
            .HasMaxLength(1000);

        entity.Property(e => e.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        entity.Property(e => e.CreatedBy)
            .HasMaxLength(450);

        entity.Property(e => e.UpdatedBy)
            .HasMaxLength(450);
    }

    private static void ConfigureRefreshTokenRelationships(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<RefreshToken> entity)
    {
        entity.HasOne(e => e.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureRefreshTokenIndexes(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<RefreshToken> entity)
    {
        entity.HasIndex(e => e.UserId);
        entity.HasIndex(e => e.JwtId);
        entity.HasIndex(e => e.ExpiryDate);
        entity.HasIndex(e => e.IsRevoked);
        entity.HasIndex(e => new { e.UserId, e.IsRevoked });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Update audit fields
        var entries = ChangeTracker.Entries()
            .Where(e => e is { Entity: RefreshToken, State: EntityState.Added or EntityState.Modified });

        foreach (var entry in entries)
        {
            var entity = (RefreshToken)entry.Entity;
            
            if (entry.State == EntityState.Modified)
            {
                entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}