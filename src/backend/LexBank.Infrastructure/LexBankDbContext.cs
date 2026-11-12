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
        });
    }
}
