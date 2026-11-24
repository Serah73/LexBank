using LexBank.Domain;
using Microsoft.EntityFrameworkCore;

namespace LexBank.Infrastructure;

public class LexBankDbContext(DbContextOptions<LexBankDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Transaction>(transaction =>
        {
            transaction.HasKey(t => t.Id);
            transaction.Property(t => t.Amount).HasPrecision(18, 2);
            transaction.Property(t => t.Balance).HasPrecision(18, 2);
            transaction.Property(t => t.Bank).HasConversion<string>().HasMaxLength(20);
            transaction.Property(t => t.Category).HasConversion<string>().HasMaxLength(30);
            transaction.Property<int>("UserId");
            transaction.HasOne<User>().WithMany().HasForeignKey("UserId").IsRequired();
            transaction.HasOne(t => t.Rule).WithMany().HasForeignKey("RuleId");
        });

        modelBuilder.Entity<Rule>(rule =>
        {
            rule.Property<int>("Id");
            rule.HasKey("Id");
            rule.Property(r => r.Pattern).HasMaxLength(Rule.MaxPatternLength);
            rule.Property(r => r.Category).HasConversion<string>().HasMaxLength(30);
            rule.Property(r => r.Sign).HasConversion<string>().HasMaxLength(10);
            rule.HasData(InitialRules.All.Select(r => new { r.Id, r.Rule.Pattern, r.Rule.Category, r.Rule.Sign, r.Rule.Priority }));
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
