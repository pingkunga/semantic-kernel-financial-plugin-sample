namespace FinancialDomain.Shared.DTOs;

public class ExchangeRateDTO
{
    public Guid Id { get; set; }
    public string DataSource { get; set; }
    public long Timestamp { get; set; }
    public DateTime MTMDate { get; set; }
    public string BaseCurrency { get; set; }
    public string Currency { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
}