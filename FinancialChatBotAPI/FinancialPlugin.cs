using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Text.Json;

/// <summary>
/// Financial plugin providing stock market and financial data functions for the AI assistant
/// </summary>
public class FinancialPlugin
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FinancialPlugin> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FinancialPlugin"/> class.
    /// </summary>
    /// <param name="logger">The logger instance for logging information and errors.</param>
    public FinancialPlugin(ILogger<FinancialPlugin> logger)
    {
        _httpClient = new HttpClient();
        _logger = logger;
    }

    /// <summary>
    /// Get the current stock price for a given symbol
    /// </summary>
    /// <param name="symbol">Stock symbol (e.g., AAPL, MSFT, GOOGL)</param>
    /// <returns>Current stock price information</returns>
    [KernelFunction]
    [Description("Get the current stock price for a given stock symbol")]
    public async Task<string> GetStockPriceAsync(
        [Description("The stock symbol to look up (e.g., AAPL, MSFT, GOOGL)")] string symbol
    )
    {
        try
        {
            _logger.LogInformation("🎯 GetStockPriceAsync called with symbol: {Symbol}", symbol);

            // For demo purposes, return mock data
            // In a real implementation, you would call a financial API like Alpha Vantage, Yahoo Finance, etc.
            var mockPrices = new Dictionary<string, decimal>
            {
                ["AAPL"] = 150.25m,
                ["MSFT"] = 280.50m,
                ["GOOGL"] = 2650.75m,
                ["AMZN"] = 3200.00m,
                ["TSLA"] = 850.30m,
                ["NVDA"] = 450.80m,
                ["META"] = 320.45m,
                ["NFLX"] = 380.90m
            };

            var upperSymbol = symbol.ToUpperInvariant();
            if (mockPrices.ContainsKey(upperSymbol))
            {
                var price = mockPrices[upperSymbol];
                var change = Math.Round((decimal)(new Random().NextDouble() * 10 - 5), 2);
                var changePercent = Math.Round(change / price * 100, 2);

                return JsonSerializer.Serialize(
                    new
                    {
                        Symbol = upperSymbol,
                        Price = price,
                        Change = change,
                        ChangePercent = changePercent,
                        Currency = "USD",
                        LastUpdated = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC")
                    }
                );
            }
            else
            {
                return $"Stock symbol '{symbol}' not found. Available symbols: {string.Join(", ", mockPrices.Keys)}";
            }
        }
        catch (Exception ex)
        {
            return $"Error retrieving stock price for {symbol}: {ex.Message}";
        }
    }

    /// <summary>
    /// Get market summary and major indices information
    /// </summary>
    /// <returns>Market summary with major indices</returns>
    [KernelFunction]
    [Description("Get current market summary including major stock indices")]
    public async Task<string> GetMarketSummaryAsync()
    {
        try
        {
            _logger.LogInformation("🎯 GetMarketSummaryAsync called");
            // Mock market data - in real implementation, call financial APIs
            var marketData = new
            {
                LastUpdated = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                Indices = new[]
                {
                    new
                    {
                        Name = "S&P 500",
                        Symbol = "SPX",
                        Value = 4150.48m,
                        Change = 15.25m,
                        ChangePercent = 0.37m
                    },
                    new
                    {
                        Name = "Dow Jones",
                        Symbol = "DJI",
                        Value = 33875.40m,
                        Change = -45.85m,
                        ChangePercent = -0.14m
                    },
                    new
                    {
                        Name = "NASDAQ",
                        Symbol = "IXIC",
                        Value = 12853.98m,
                        Change = 25.60m,
                        ChangePercent = 0.20m
                    }
                },
                MarketStatus = "Open",
                TradingSession = "Regular Hours"
            };

            return JsonSerializer.Serialize(
                marketData,
                new JsonSerializerOptions { WriteIndented = true }
            );
        }
        catch (Exception ex)
        {
            return $"Error retrieving market summary: {ex.Message}";
        }
    }

    /// <summary>
    /// Calculate compound interest for investment planning
    /// </summary>
    /// <param name="principal">Initial investment amount</param>
    /// <param name="rate">Annual interest rate (as decimal, e.g., 0.07 for 7%)</param>
    /// <param name="time">Number of years</param>
    /// <param name="compoundFrequency">Number of times interest compounds per year (default: 12 for monthly)</param>
    /// <returns>Compound interest calculation results</returns>
    [KernelFunction]
    [Description("Calculate compound interest for investment planning")]
    public string CalculateCompoundInterest(
        [Description("Initial investment amount in dollars")] double principal,
        [Description("Annual interest rate as decimal (e.g., 0.07 for 7%)")] double rate,
        [Description("Number of years for the investment")] int time,
        [Description("Compounding frequency per year (default: 12 for monthly)")]
            int compoundFrequency = 12
    )
    {
        try
        {
            _logger.LogInformation(
                "🎯 CalculateCompoundInterest called with Principal: {Principal}, Rate: {Rate}, Time: {Time}, Frequency: {Frequency}",
                principal,
                rate,
                time,
                compoundFrequency
            );

            if (principal <= 0 || rate < 0 || time <= 0 || compoundFrequency <= 0)
            {
                return "Invalid input: All values must be positive, and rate cannot be negative.";
            }

            var amount =
                principal * Math.Pow(1 + (rate / compoundFrequency), compoundFrequency * time);
            var interest = amount - principal;

            var result = new
            {
                InitialInvestment = Math.Round(principal, 2),
                AnnualInterestRate = Math.Round(rate * 100, 2) + "%",
                Years = time,
                CompoundingFrequency = compoundFrequency,
                FinalAmount = Math.Round(amount, 2),
                TotalInterest = Math.Round(interest, 2),
                EffectiveAnnualReturn = Math.Round(((amount / principal) - 1) * 100 / time, 2) + "%"
            };

            return JsonSerializer.Serialize(
                result,
                new JsonSerializerOptions { WriteIndented = true }
            );
        }
        catch (Exception ex)
        {
            return $"Error calculating compound interest: {ex.Message}";
        }
    }

    /// <summary>
    /// Get basic financial ratios and analysis for a stock
    /// </summary>
    /// <param name="symbol">Stock symbol to analyze</param>
    /// <returns>Financial ratios and analysis</returns>
    [KernelFunction]
    [Description("Get financial ratios and basic analysis for a stock symbol")]
    public async Task<string> GetFinancialAnalysisAsync(
        [Description("Stock symbol to analyze (e.g., AAPL, MSFT)")] string symbol
    )
    {
        try
        {
            _logger.LogInformation(
                "🎯 GetFinancialAnalysisAsync called with symbol: {Symbol}",
                symbol
            );
            // Mock financial analysis data
            var mockAnalysis = new Dictionary<string, object>
            {
                ["AAPL"] = new
                {
                    Symbol = "AAPL",
                    CompanyName = "Apple Inc.",
                    PE_Ratio = 28.5,
                    EPS = 5.89,
                    DividendYield = 0.66,
                    MarketCap = "2.8T",
                    BookValue = 3.85,
                    DebtToEquity = 1.73,
                    ROE = 147.4,
                    RevenueGrowth = 8.1,
                    Recommendation = "Buy",
                    TargetPrice = 165.00
                },
                ["MSFT"] = new
                {
                    Symbol = "MSFT",
                    CompanyName = "Microsoft Corporation",
                    PE_Ratio = 32.1,
                    EPS = 8.75,
                    DividendYield = 0.72,
                    MarketCap = "2.1T",
                    BookValue = 13.55,
                    DebtToEquity = 0.47,
                    ROE = 47.1,
                    RevenueGrowth = 12.0,
                    Recommendation = "Buy",
                    TargetPrice = 310.00
                }
            };

            var upperSymbol = symbol.ToUpperInvariant();
            if (mockAnalysis.ContainsKey(upperSymbol))
            {
                return JsonSerializer.Serialize(
                    mockAnalysis[upperSymbol],
                    new JsonSerializerOptions { WriteIndented = true }
                );
            }
            else
            {
                return $"Financial analysis not available for '{symbol}'. Available symbols: {string.Join(", ", mockAnalysis.Keys)}";
            }
        }
        catch (Exception ex)
        {
            return $"Error retrieving financial analysis for {symbol}: {ex.Message}";
        }
    }

    /// <summary>
    /// Convert currency amounts between different currencies
    /// </summary>
    /// <param name="amount">Amount to convert</param>
    /// <param name="fromCurrency">Source currency code (e.g., USD, EUR, GBP)</param>
    /// <param name="toCurrency">Target currency code (e.g., USD, EUR, GBP)</param>
    /// <returns>Currency conversion result</returns>
    [KernelFunction]
    [Description("Convert currency amounts between different currencies")]
    public string ConvertCurrency(
        [Description("Amount to convert")] double amount,
        [Description("Source currency code (e.g., USD, EUR, GBP)")] string fromCurrency,
        [Description("Target currency code (e.g., USD, EUR, GBP)")] string toCurrency
    )
    {
        try
        {
            _logger.LogInformation(
                "🎯 ConvertCurrency called with Amount: {Amount}, From: {FromCurrency}, To: {ToCurrency}",
                amount,
                fromCurrency,
                toCurrency
            );
            // Mock exchange rates - in real implementation, call currency API
            var exchangeRates = new Dictionary<string, Dictionary<string, double>>
            {
                ["USD"] = new()
                {
                    ["EUR"] = 0.85,
                    ["GBP"] = 0.73,
                    ["JPY"] = 110.0,
                    ["CAD"] = 1.25,
                    ["AUD"] = 1.35
                },
                ["EUR"] = new()
                {
                    ["USD"] = 1.18,
                    ["GBP"] = 0.86,
                    ["JPY"] = 129.5,
                    ["CAD"] = 1.47,
                    ["AUD"] = 1.59
                },
                ["GBP"] = new()
                {
                    ["USD"] = 1.37,
                    ["EUR"] = 1.16,
                    ["JPY"] = 150.7,
                    ["CAD"] = 1.71,
                    ["AUD"] = 1.85
                }
            };

            var fromUpper = fromCurrency.ToUpperInvariant();
            var toUpper = toCurrency.ToUpperInvariant();

            if (fromUpper == toUpper)
            {
                return JsonSerializer.Serialize(
                    new
                    {
                        Amount = amount,
                        FromCurrency = fromUpper,
                        ToCurrency = toUpper,
                        ConvertedAmount = amount,
                        ExchangeRate = 1.0,
                        Message = "Same currency conversion"
                    }
                );
            }

            if (
                !exchangeRates.ContainsKey(fromUpper)
                || !exchangeRates[fromUpper].ContainsKey(toUpper)
            )
            {
                return $"Exchange rate not available for {fromCurrency} to {toCurrency}. Available currencies: USD, EUR, GBP, JPY, CAD, AUD";
            }

            var rate = exchangeRates[fromUpper][toUpper];
            var convertedAmount = Math.Round(amount * rate, 2);

            return JsonSerializer.Serialize(
                new
                {
                    Amount = amount,
                    FromCurrency = fromUpper,
                    ToCurrency = toUpper,
                    ConvertedAmount = convertedAmount,
                    ExchangeRate = rate,
                    LastUpdated = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC")
                },
                new JsonSerializerOptions { WriteIndented = true }
            );
        }
        catch (Exception ex)
        {
            return $"Error converting currency: {ex.Message}";
        }
    }
}
