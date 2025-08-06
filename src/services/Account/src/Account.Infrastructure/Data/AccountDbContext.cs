using System.Diagnostics.CodeAnalysis;
using BankSystem.Account.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using AccountEntity = BankSystem.Account.Domain.Entities.Account;

namespace BankSystem.Account.Infrastructure.Data;

[ExcludeFromCodeCoverage]
public class AccountDbContext(DbContextOptions<AccountDbContext> options) : DbContext(options)
{
    public DbSet<AccountEntity> Accounts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AccountDbContext).Assembly);

        // Configure Account entity
        ConfigureAccount(modelBuilder);
    }

    private static void ConfigureAccount(ModelBuilder modelBuilder)
    {
        var accountEntity = modelBuilder.Entity<AccountEntity>();

        // Primary key
        accountEntity.HasKey(a => a.Id);

        // Properties
        accountEntity.Property(a => a.Id).IsRequired().ValueGeneratedNever(); // We generate GUIDs in the domain

        accountEntity
            .Property(a => a.AccountNumber)
            .IsRequired()
            .HasMaxLength(10)
            .IsFixedLength()
            .HasConversion(accountNumber => accountNumber.Value, value => new AccountNumber(value));

        accountEntity.Property(a => a.CustomerId).IsRequired();

        accountEntity.Property(a => a.Type).IsRequired().HasConversion<string>().HasMaxLength(15);

        accountEntity.Property(a => a.Status).IsRequired().HasConversion<string>().HasMaxLength(20);

        // Configure Money value object for Balance
        accountEntity.OwnsOne(
            a => a.Balance,
            balanceBuilder =>
            {
                balanceBuilder
                    .Property(m => m.Amount)
                    .HasColumnName("Balance")
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();

                // Explicitly configure the nested Currency value object
                balanceBuilder.OwnsOne(
                    m => m.Currency,
                    currencyBuilder =>
                    {
                        currencyBuilder
                            .Property(c => c.Code)
                            .HasColumnName("CurrencyCode")
                            .HasMaxLength(3)
                            .IsFixedLength()
                            .IsRequired();

                        // Tell EF to not map other properties of Currency if they are not stored in the DB
                        currencyBuilder.Ignore(c => c.Symbol);
                    }
                );
            }
        );

        // Auditing fields
        accountEntity.Property(a => a.CreatedAt).IsRequired();

        accountEntity.Property(a => a.UpdatedAt).IsRequired(false);

        accountEntity.Property(a => a.CreatedBy).HasMaxLength(30);

        accountEntity.Property(a => a.UpdatedBy).HasMaxLength(30);

        // Indexes for performance
        accountEntity
            .HasIndex(a => a.AccountNumber)
            .IsUnique()
            .HasDatabaseName("IX_Accounts_AccountNumber");

        accountEntity.HasIndex(a => a.CustomerId).HasDatabaseName("IX_Accounts_CustomerId");

        accountEntity.HasIndex(a => a.Status).HasDatabaseName("IX_Accounts_Status");

        accountEntity.HasIndex(a => a.CreatedAt).HasDatabaseName("IX_Accounts_CreatedAt");

        // Table name
        accountEntity.ToTable("Accounts");
    }
}
