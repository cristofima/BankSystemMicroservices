# Entity Framework Core Guidelines

## Overview

This document provides comprehensive guidelines for working with Entity Framework Core in the Bank System Microservices project, following Clean Architecture principles and .NET best practices.

## Package Configuration

### Microsoft.EntityFrameworkCore.Design Package Placement

The `Microsoft.EntityFrameworkCore.Design` package should be placed **only in the API/Startup project** to avoid conflicts and maintain clear separation of concerns.

**✅ Correct Configuration:**

```xml
<!-- Security.Api.csproj -->
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="9.0.6">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
```

**❌ Avoid:**

- Placing the Design package in Infrastructure project
- Duplicating the package in multiple projects
- Using the package without PrivateAssets

## Migration Commands

All migration commands should be run from the **service root directory** (e.g., `src/services/Security/src/`).

### Basic Migration Operations

```powershell
# Create a new migration
dotnet ef migrations add MigrationName --project Security.Infrastructure --startup-project Security.Api

# Apply migrations to database
dotnet ef database update --project Security.Infrastructure --startup-project Security.Api

# List all migrations
dotnet ef migrations list --project Security.Infrastructure --startup-project Security.Api

# Remove last migration (only if not applied to database)
dotnet ef migrations remove --project Security.Infrastructure --startup-project Security.Api
```

### Advanced Migration Operations

```powershell
# Generate SQL script for all migrations
dotnet ef migrations script --project Security.Infrastructure --startup-project Security.Api

# Generate SQL script for specific migration range
dotnet ef migrations script --from 20250101000000_InitialCreate --to 20250102000000_AddUserRoles --project Security.Infrastructure --startup-project Security.Api

# Apply migrations for specific environment
dotnet ef database update --project Security.Infrastructure --startup-project Security.Api --environment Production

# Rollback to specific migration
dotnet ef database update 20250101000000_InitialCreate --project Security.Infrastructure --startup-project Security.Api

# Drop database and recreate
dotnet ef database drop --project Security.Infrastructure --startup-project Security.Api --force
dotnet ef database update --project Security.Infrastructure --startup-project Security.Api
```

### Environment-Specific Commands

```powershell
# Development environment
dotnet ef database update --project Security.Infrastructure --startup-project Security.Api --environment Development

# Staging environment
dotnet ef database update --project Security.Infrastructure --startup-project Security.Api --environment Staging

# Production environment (use scripts instead)
dotnet ef migrations script --project Security.Infrastructure --startup-project Security.Api --environment Production
```

## DbContext Configuration

### Configuration Best Practices

```csharp
// SecurityDbContext.cs (Infrastructure project)
public class SecurityDbContext : IdentityDbContext<ApplicationUser>
{
    public SecurityDbContext(DbContextOptions<SecurityDbContext> options)
        : base(options)
    {
    }

    public DbSet<RefreshToken> RefreshTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Apply all configurations from assembly
        builder.ApplyConfigurationsFromAssembly(typeof(SecurityDbContext).Assembly);

        // Configure custom entities
        ConfigureRefreshTokens(builder);
        ConfigureApplicationUser(builder);
    }

    private static void ConfigureRefreshTokens(ModelBuilder builder)
    {
        builder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(rt => rt.Token);
            entity.Property(rt => rt.Token).HasMaxLength(256);
            entity.Property(rt => rt.JwtId).HasMaxLength(128).IsRequired();

            // Indexes for performance
            entity.HasIndex(rt => rt.ExpiryDate);
            entity.HasIndex(rt => rt.IsRevoked);
            entity.HasIndex(rt => rt.JwtId);
            entity.HasIndex(rt => new { rt.UserId, rt.IsRevoked });

            // Relationships
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
```

### Registration in API Project

```csharp
// Program.cs (API project)
builder.Services.AddDbContext<SecurityDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.CommandTimeout(30);
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
    });

    // Development-only settings
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});
```

## Migration Best Practices

### Naming Conventions

Use descriptive, action-based names for migrations:

```powershell
# ✅ Good migration names
dotnet ef migrations add AddUserRoleSupport
dotnet ef migrations add UpdateTransactionIndexes
dotnet ef migrations add RemoveObsoleteColumns
dotnet ef migrations add AddAccountStatusEnum

# ❌ Poor migration names
dotnet ef migrations add Update1
dotnet ef migrations add FixBug
dotnet ef migrations add Changes
```

### Migration Content Guidelines

1. **Keep migrations focused**: One logical change per migration
2. **Review generated code**: Always check the migration files before applying
3. **Add data seeding**: Include necessary seed data in migrations when appropriate
4. **Handle breaking changes**: Plan for backward compatibility when possible

### Example Migration with Data Seeding

```csharp
public partial class AddDefaultRoles : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Create roles table changes
        migrationBuilder.CreateTable(
            name: "ApplicationRoles",
            columns: table => new
            {
                Id = table.Column<string>(nullable: false),
                Name = table.Column<string>(maxLength: 256, nullable: true),
                // ... other columns
            });

        // Seed default roles
        migrationBuilder.InsertData(
            table: "AspNetRoles",
            columns: new[] { "Id", "Name", "NormalizedName" },
            values: new object[,]
            {
                { "1", "Administrator", "ADMINISTRATOR" },
                { "2", "Manager", "MANAGER" },
                { "3", "User", "USER" }
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("ApplicationRoles");
    }
}
```

## Production Deployment

### Script Generation for Production

Instead of running migrations directly in production, generate SQL scripts:

```powershell
# Generate script for all pending migrations
dotnet ef migrations script --project Security.Infrastructure --startup-project Security.Api --output migration-script.sql

# Generate script from specific migration
dotnet ef migrations script 20250101000000_LastAppliedMigration --project Security.Infrastructure --startup-project Security.Api --output update-script.sql

# Generate idempotent script (safe to run multiple times)
dotnet ef migrations script --idempotent --project Security.Infrastructure --startup-project Security.Api --output idempotent-script.sql
```

### Production Checklist

1. **Test in staging**: Apply migrations to staging environment first
2. **Backup database**: Always backup production database before applying migrations
3. **Review SQL script**: Manually review generated SQL for any issues
4. **Plan downtime**: Consider if migrations require downtime
5. **Rollback plan**: Have a rollback strategy ready
6. **Monitor performance**: Check performance impact of new indexes/changes

## Troubleshooting

### Common Issues and Solutions

#### Issue: "Your target project doesn't match your migrations assembly"

**Solution**: Always specify both projects in the command:

```powershell
dotnet ef migrations add MigrationName --project Security.Infrastructure --startup-project Security.Api
```

#### Issue: "No DbContext was found"

**Solution**: Ensure DbContext is properly registered in the startup project's `Program.cs`

#### Issue: "Build failed"

**Solution**: Build the solution first:

```powershell
dotnet build
dotnet ef migrations add MigrationName --project Security.Infrastructure --startup-project Security.Api
```

#### Issue: "Connection string not found"

**Solution**: Verify connection string configuration in `appsettings.json` and environment variables

### Debugging Migration Issues

```powershell
# Verbose output for troubleshooting
dotnet ef migrations add MigrationName --project Security.Infrastructure --startup-project Security.Api --verbose

# Check EF Core version compatibility
dotnet list package --include-transitive | findstr EntityFramework
```

## Performance Considerations

### Index Strategy

Always consider indexes when designing entities:

```csharp
// In entity configuration
entity.HasIndex(e => e.Email).IsUnique();
entity.HasIndex(e => e.CreatedAt);
entity.HasIndex(e => new { e.UserId, e.IsActive }); // Composite index
```

### Query Optimization

```csharp
// Use AsNoTracking for read-only queries
var users = await _context.Users
    .AsNoTracking()
    .Where(u => u.IsActive)
    .ToListAsync();

// Select only needed properties
var userSummaries = await _context.Users
    .Select(u => new UserSummaryDto
    {
        Id = u.Id,
        Name = u.FirstName + " " + u.LastName
    })
    .ToListAsync();
```

## Security Considerations

### Sensitive Data Logging

```csharp
// Disable in production
options.EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
```

### Connection String Security

```json
// Use Azure Key Vault for production
"ConnectionStrings": {
  "DefaultConnection": "@Microsoft.KeyVault(SecretUri=https://your-keyvault.vault.azure.net/secrets/connection-string/)"
}
```

## Integration with Clean Architecture

### Repository Pattern Implementation

```csharp
// IUserRepository.cs (Application layer)
public interface IUserRepository
{
    Task<ApplicationUser?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task AddAsync(ApplicationUser user, CancellationToken cancellationToken = default);
    Task UpdateAsync(ApplicationUser user, CancellationToken cancellationToken = default);
}

// UserRepository.cs (Infrastructure layer)
public class UserRepository : IUserRepository
{
    private readonly SecurityDbContext _context;

    public UserRepository(SecurityDbContext context)
    {
        _context = context;
    }

    public async Task<ApplicationUser?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }
}
```

## Testing with Entity Framework

### Integration Testing Setup

```csharp
[TestFixture]
public class SecurityDbContextTests
{
    private SecurityDbContext _context;
    private DbContextOptions<SecurityDbContext> _options;

    [SetUp]
    public void Setup()
    {
        _options = new DbContextOptionsBuilder<SecurityDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new SecurityDbContext(_options);
        _context.Database.EnsureCreated();
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Test]
    public async Task CanCreateAndRetrieveUser()
    {
        // Arrange
        var user = new ApplicationUser
        {
            UserName = "testuser@example.com",
            Email = "testuser@example.com"
        };

        // Act
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Assert
        var retrievedUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == "testuser@example.com");

        Assert.That(retrievedUser, Is.Not.Null);
        Assert.That(retrievedUser.UserName, Is.EqualTo("testuser@example.com"));
    }
}
```

---

_This documentation should be updated as Entity Framework practices evolve and new patterns emerge._
