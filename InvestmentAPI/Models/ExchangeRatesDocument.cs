namespace InvestmentAPI.Models;

public class ExchangeRatesDocument
{
    public string Disclaimer { get; set; }
    public string License { get; set; }
    public long Timestamp { get; set; }
    public string Base { get; set; } // "Base" is a reserved word, but allowed as a property name in C#
    public Dictionary<string, decimal> Rates { get; set; }
}
