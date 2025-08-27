using System;

namespace InvestmentAPI.Models;

public class ExchangeRateEntry
{
    public Guid Id { get; set; } = Guid.NewGuid(); // Generate UUID by default
    public string DataSource { get; set; }
    public long Timestamp { get; set; }
    public DateTime MTMDate { get; set; }
    public string BaseCurrency { get; set; }
    public string Currency { get; set; }
    public decimal Rate { get; set; }
}