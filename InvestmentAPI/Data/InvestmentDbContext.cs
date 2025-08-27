using Microsoft.EntityFrameworkCore;
using InvestmentAPI.Models;

namespace InvestmentAPI.Data;

public class InvestmentDbContext : DbContext
{
    public InvestmentDbContext(DbContextOptions<InvestmentDbContext> options)
        : base(options) { }

    public DbSet<ExchangeRateEntry> ExchangeRates { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ExchangeRateEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd(); // Ensure GUID is generated automatically
            entity.Property(e => e.MTMDate).HasColumnType("date").IsRequired();
            entity.Property(e => e.BaseCurrency).HasMaxLength(3).IsRequired();
            entity.Property(e => e.Currency).HasMaxLength(3).IsRequired();
            entity.Property(e => e.Rate).HasColumnType("decimal(18,6)");
            entity.Property(e => e.DataSource).HasMaxLength(100);

            // Create index for faster queries
            entity.HasIndex(
                e =>
                    new
                    {
                        e.MTMDate,
                        e.BaseCurrency,
                        e.Currency
                    }
            );
        });
    }
}
