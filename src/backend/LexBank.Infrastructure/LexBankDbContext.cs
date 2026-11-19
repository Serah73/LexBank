using LexBank.Domain;
using Microsoft.EntityFrameworkCore;

namespace LexBank.Infrastructure;

public class LexBankDbContext(DbContextOptions<LexBankDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Transaction>(transaction =>
        {
            transaction.Property<int>("Id");
            transaction.HasKey("Id");
            transaction.Property(t => t.Amount).HasPrecision(18, 2);
            transaction.Property(t => t.Balance).HasPrecision(18, 2);
            transaction.Property(t => t.Bank).HasConversion<string>().HasMaxLength(20);
            transaction.Property(t => t.Category).HasConversion<string>().HasMaxLength(30);
            transaction.Property<int>("UserId");
            transaction.HasOne<User>().WithMany().HasForeignKey("UserId").IsRequired();
        });

        modelBuilder.Entity<Rule>(rule =>
        {
            rule.Property<int>("Id");
            rule.HasKey("Id");
            rule.Property(r => r.Pattern).HasMaxLength(100);
            rule.Property(r => r.Category).HasConversion<string>().HasMaxLength(30);
            rule.Property(r => r.Sign).HasConversion<string>().HasMaxLength(10);
            rule.HasData(InitialRules.All.Select((r, i) => new { Id = i + 1, r.Pattern, r.Category, r.Sign, r.Priority }));
        });

        modelBuilder.Entity<User>(user =>
        {
            user.ToTable("Users");
            user.Property<int>("Id");
            user.HasKey("Id");
            user.Property(u => u.Email).HasMaxLength(256);
            user.HasIndex(u => u.Email).IsUnique();
        });
    }
}
