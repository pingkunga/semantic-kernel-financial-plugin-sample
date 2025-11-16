using FinancialDomain.Shared.DTOs;
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
        /*
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
        */
        try
        {
            _logger.LogInformation("🎯 GetStockPriceAsync called with symbol: {Symbol}", symbol);

            var upperSymbol = symbol.ToUpperInvariant();

            // Fetch historical data for the last day
            var historicalData = await YahooFinanceApi.Yahoo.GetHistoricalAsync(
                upperSymbol,
                DateTime.Now.AddDays(-1),
                DateTime.Now
            );

            if (historicalData == null || !historicalData.Any())
            {
                return $"No data available for symbol '{symbol}'. Please check the symbol or try again later.";
            }

            // Use the latest data point
            var latest = historicalData.Last();
            var previous = historicalData.Count > 1 ? historicalData[historicalData.Count - 2] : latest; // Previous close if available

            var price = latest.Close;
            var change = price - previous.Close;
            var changePercent = Math.Round((change / previous.Close) * 100, 2);

            return JsonSerializer.Serialize(
                new
                {
                    Symbol = upperSymbol,
                    Price = price,
                    Change = change,
                    ChangePercent = changePercent,
                    Currency = "USD",
                    DataDate = latest.DateTime.ToString("yyyy-MM-dd"), 
                    LastUpdated = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                    Note = $"Data is based on the latest available close price {latest.DateTime.ToString("yyyy-MM-dd")} from Yahoo Finance."
                },
                new JsonSerializerOptions { WriteIndented = true }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving stock price for {Symbol}", symbol);
            return $"Error retrieving stock price for {symbol}: {ex.Message}";

            // Fallback to mock data
            //return await GetMockStockPriceAsync(symbol);
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
    /// Get market outlook for Thailand's SET Index, specified securities, and VIX volatility index
    /// </summary>
    [KernelFunction]
    [Description(
        "Get market outlook for Thailand's SET Index, specified securities, and VIX volatility index"
    )]
    public async Task<string> GetThaiMarketOutlookAsync(
        [Description(
            "Comma-separated list of Thai security symbols (e.g., SCB.BK,PTT.BK,KBANK.BK). If empty, defaults to common securities."
        )]
            string securitySymbols = ""
    )
    {
        try
        {
            _logger.LogInformation(
                "🎯 GetMarketOutlookAsync called with securities: {Securities}",
                securitySymbols
            );

            // Fetch Thailand's SET Index
            var setIndex = await YahooFinanceApi.Yahoo.GetHistoricalAsync(
                "^SET.BK",
                DateTime.Now.AddDays(-1),
                DateTime.Now
            );
            //wait a moment to avoid rate limit
            await Task.Delay(500);

            // Fetch VIX (Volatility Index)
            var vixIndex = await YahooFinanceApi.Yahoo.GetHistoricalAsync(
                "^VIX",
                DateTime.Now.AddDays(-1),
                DateTime.Now
            );
            await Task.Delay(500);

            // Parse security symbols or use defaults
            var securities = string.IsNullOrWhiteSpace(securitySymbols)
                ? new[] { "SCB.BK", "PTT.BK", "KBANK.BK" } // Default securities
                : securitySymbols
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .ToArray();

            var securityData = new List<object>();
            foreach (var symbol in securities)
            {
                try
                {
                    var data = await YahooFinanceApi.Yahoo.GetHistoricalAsync(
                        symbol,
                        DateTime.Now.AddDays(-1),
                        DateTime.Now
                    );
                    await Task.Delay(500);

                    var last = data.Last();
                    var first = data.First();
                    securityData.Add(
                        new
                        {
                            Symbol = symbol,
                            CurrentValue = last.Close,
                            Change = last.Close - first.Close,
                            ChangePercent = Math.Round(
                                ((last.Close - first.Close) / first.Close) * 100,
                                2
                            )
                        }
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        "Failed to fetch data for {Symbol}: {Message}",
                        symbol,
                        ex.Message
                    );
                    securityData.Add(new { Symbol = symbol, Error = "Data unavailable" });
                }
            }

            var outlook = new
            {
                LastUpdated = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                SETIndex = new
                {
                    Name = "SET Index (Thailand)",
                    Symbol = "^SET.BK",
                    CurrentValue = setIndex.Last().Close,
                    Change = setIndex.Last().Close - setIndex.First().Close,
                    ChangePercent = Math.Round(
                        ((setIndex.Last().Close - setIndex.First().Close) / setIndex.First().Close)
                            * 100,
                        2
                    )
                },
                VIXIndex = new
                {
                    Name = "VIX (Volatility Index)",
                    Symbol = "^VIX",
                    CurrentValue = vixIndex.Last().Close,
                    Change = vixIndex.Last().Close - vixIndex.First().Close,
                    ChangePercent = Math.Round(
                        ((vixIndex.Last().Close - vixIndex.First().Close) / vixIndex.First().Close)
                            * 100,
                        2
                    )
                },
                Securities = securityData,
                MarketOutlook = "Thailand's SET Index reflects local economic trends, with VIX indicating market volatility levels. Specified securities show sector-specific performance."
            };

            return JsonSerializer.Serialize(
                outlook,
                new JsonSerializerOptions { WriteIndented = true }
            );
        }
        catch (Exception ex)
        {
            return $"Error retrieving Thailand market outlook: {ex.Message}";
        }
    }

    #region > InvestmentAPI Integration
    [KernelFunction]
    [Description("Convert currency amounts on today's date using latest exchange rates")]
    public async Task<string> ConvertCurrencyOnTodayDate(
        [Description("Amount to convert")] decimal amount,
        [Description("Source currency code (e.g., USD, EUR, GBP)")] string fromCurrency,
        [Description("Target currency code (e.g., USD, EUR, GBP)")] string toCurrency
    )
    {
        String result = await ConvertCurrencyOnSpecificDate(
            amount,
            fromCurrency,
            toCurrency,
            DateTime.UtcNow.ToString("yyyy-MM-dd")
        );

        if (result.Contains("Invalid date format") || result.Contains("Error"))
        {
            _logger.LogWarning(
                "🎯 ConvertCurrencyOnSpecificDate failed, fallback to ConvertCurrency"
            );
            return await ConvertCurrency(amount, fromCurrency, toCurrency);
        }

        return result;
    }

    /// <summary>
    /// Convert currency amounts on a specific date
    /// </summary>
    /// param name="amount">Amount to convert</param>
    /// <param name="fromCurrency">Source currency code (e.g., USD, EUR, GBP)</param>
    /// <param name="toCurrency">Target currency code (e.g., USD, EUR, GBP)</param>
    /// <param name="date">Date for the exchange rate (YYYY-MM-DD)</param>
    /// <returns></returns>
    [KernelFunction]
    [Description("Convert currency amounts on a specific date using historical exchange rates")]
    public async Task<string> ConvertCurrencyOnSpecificDate(
        [Description("Amount to convert")] decimal amount,
        [Description("Source currency code (e.g., USD, EUR, GBP)")] string fromCurrency,
        [Description("Target currency code (e.g., USD, EUR, GBP)")] string toCurrency,
        [Description("Date for the exchange rate (YYYY-MM-DD)")] string date
    )
    {
        try
        {
            _logger.LogInformation(
                "🎯 ConvertCurrencyOnSpecificDate called with Amount: {Amount}, From: {FromCurrency}, To: {ToCurrency}, Date: {Date}",
                amount,
                fromCurrency,
                toCurrency,
                date
            );

            //Validate date format
            if (!DateTime.TryParse(date, out DateTime parsedDate))
            {
                return "Invalid date format. Please use YYYY-MM-DD.";
            }

            String baseUrl = GetInvestmentAPIBaseUrl();
            var url =
                $"{baseUrl}/api/exchangerate/specificrate?date={date}&baseCurrency={fromCurrency}&targetCurrency={toCurrency}&isMatchDay=true";

            _logger.LogInformation("🎯 Calling InvestmentAPI at {Url}", url);
            var response = await _httpClient.GetAsync(url);

            // Handle non-success status codes
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return $"Error from InvestmentAPI: {response.StatusCode} - {errorContent}";
            }
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true // Optional: Ignore case differences
            };

            var rate = JsonSerializer.Deserialize<ExchangeRateDTO>(result, options);
            if (rate == null)
            {
                return "Not Found";
            }

            return JsonSerializer.Serialize(
                new
                {
                    Amount = amount,
                    FromCurrency = fromCurrency.ToUpperInvariant(),
                    ToCurrency = toCurrency.ToUpperInvariant(),
                    ConvertedAmount = Math.Round(amount * rate.Rate, 2),
                    ExchangeRate = rate.Rate,
                    RateDate = rate.MTMDate
                },
                new JsonSerializerOptions { WriteIndented = true }
            );
        }
        catch (Exception ex)
        {
            return $"Error converting currency: {ex.Message}";
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
    public async Task<string> ConvertCurrency(
        [Description("Amount to convert")] decimal amount,
        [Description("Source currency code (e.g., USD, EUR, GBP, BTC)")] string fromCurrency,
        [Description("Target currency code (e.g., USD, EUR, GBP, BTC)")] string toCurrency
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

            //Call API
            //GET {{InvestmentAPI_HostAddress}}/api/exchangerate/specificrate?date=2025-08-23&baseCurrency=BTC&targetCurrency=THB&isMatchDay=true

            String baseUrl = GetInvestmentAPIBaseUrl();
            var url =
                $"{baseUrl}/api/exchangerate/specificrate?date={DateTime.Now.ToString("yyyy-MM-dd")}&baseCurrency={fromCurrency}&targetCurrency={toCurrency}&isMatchDay=false";

            _logger.LogInformation("🎯 Calling InvestmentAPI at {Url}", url);
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true // Optional: Ignore case differences
            };

            var rate = JsonSerializer.Deserialize<ExchangeRateDTO>(result, options);
            if (rate == null)
            {
                return "Not Found";
            }

            return JsonSerializer.Serialize(
                new
                {
                    Amount = amount,
                    FromCurrency = fromCurrency.ToUpperInvariant(),
                    ToCurrency = toCurrency.ToUpperInvariant(),
                    ConvertedAmount = Math.Round(amount * rate.Rate, 2),
                    ExchangeRate = rate.Rate,
                    LastUpdated = rate.MTMDate
                },
                new JsonSerializerOptions { WriteIndented = true }
            );
        }
        catch (Exception ex)
        {
            return $"Error converting currency: {ex.Message}";
        }
    }

    //Call REST API
    //Automatically generated methods for other financial functions can be added here
    /// <summary>
    /// Calls the InvestmentAPI to calculate simple investment return.
    /// </summary>
    [KernelFunction]
    [Description("Calculate simple investment return using the InvestmentAPI")]
    public async Task<string> GetSimpleInvestmentReturnAsync(
        [Description("Initial investment value")] double initialValue,
        [Description("Final investment value")] double finalValue
    )
    {
        try
        {
            _logger.LogInformation(
                "🎯 GetSimpleInvestmentReturnAsync called with InitialValue: {InitialValue}, FinalValue: {FinalValue}",
                initialValue,
                finalValue
            );

            String baseUrl = GetInvestmentAPIBaseUrl();
            var url =
                $"{baseUrl}/api/CalcInvReturn/simple-return?initialValue={initialValue}&finalValue={finalValue}";
            _logger.LogInformation("🎯 Calling InvestmentAPI at {Url}", url);
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling InvestmentAPI");
            return $"Error calling InvestmentAPI: {ex.Message}";
        }
    }

    //CAGR
    /// <summary>
    /// Calculate Compound Annual Growth Rate (CAGR) using the InvestmentAPI.
    /// </summary>
    /// <param name="initialValue">Initial investment value</param>
    /// <param name="finalValue">Final investment value</param>
    /// <param name="years">Number of years</param>
    /// <returns>The CAGR calculation result as a string</returns>
    [KernelFunction]
    [Description("Calculate Compound Annual Growth Rate (CAGR) using the InvestmentAPI")]
    public async Task<string> GetCAGRAsync(
        [Description("Initial investment value")] double initialValue,
        [Description("Final investment value")] double finalValue,
        [Description("Number of years")] int years
    )
    {
        try
        {
            _logger.LogInformation(
                "🎯 GetCAGRAsync called with InitialValue: {InitialValue}, FinalValue: {FinalValue}, Years: {Years}",
                initialValue,
                finalValue,
                years
            );

            String baseUrl = GetInvestmentAPIBaseUrl();
            var url =
                $"{baseUrl}/api/CalcInvReturn/cagr?initialValue={initialValue}&finalValue={finalValue}&years={years}";
            _logger.LogInformation("🎯 Calling InvestmentAPI at {Url}", url);
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling InvestmentAPI");
            return $"Error calling InvestmentAPI: {ex.Message}";
        }
    }

    /// <summary>
    /// Gets the base URL for the InvestmentAPI from the configuration.
    /// </summary>
    /// <returns>The base URL string for the InvestmentAPI, or an error message if not configured.</returns>
    public string GetInvestmentAPIBaseUrl()
    {
        var _configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        var config = _configuration.GetSection("InvestmentAPI");
        if (config == null || string.IsNullOrEmpty(config["BaseUrl"]))
        {
            _logger.LogError("InvestmentAPI configuration is missing or invalid.");
            return "InvestmentAPI configuration is missing or invalid.";
        }
        return config["BaseUrl"].TrimEnd('/');
    }
    #endregion > InvestmentAPI Integration


    #region > LottoAPI Integration

    /// <summary>
    /// Gets the prize 2 digit history from the LottoAPI with optional month and day filters
    /// </summary>
    /// <param name="month">Optional month filter (1-12)</param>
    /// <param name="day">Optional day filter (1-31)</param>
    /// <returns>Prize 2 digit history as a JSON string</returns>
    [KernelFunction]
    [Description("Get prize 2 digit history from the LottoAPI with month and day filters")]
    public async Task<string> GetPrize2DigitHistoryAsync(
        [Description("month filter (1-12)")] int month,
        [Description("day filter (1-31)")] int day
    )
    {
        //http://localhost:5182/api/lotto/prize2digithistory?month=12&day=16
        try
        {
            _logger.LogInformation(
                "🎯 GetPrize2DigitHistoryAsync called with Month: {Month}, Day: {Day}",
                month,
                day
            );

            String baseUrl = GetLottoAPIBaseUrl();

            var query = new List<string>();
            query.Add($"month={month}");
            query.Add($"day={day}");
            // if (month.HasValue)
            //  query.Add($"month={month.Value}");
            //if (day.HasValue)
            //  query.Add($"day={day.Value}");
            var queryString = query.Count > 0 ? "?" + string.Join("&", query) : "";

            var url = $"{baseUrl}/api/lotto/prize2digithistory{queryString}";
            _logger.LogInformation("🎯 Calling LottoAPI at {Url}", url);

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling LottoAPI");
            return $"Error calling LottoAPI: {ex.Message}";
        }
    }

    /// <summary>
    /// Gets the prize 3 digit history from the LottoAPI with optional month and day filters
    /// </summary>
    /// <param name="month">Optional month filter (1-12)</param>
    /// <param name="day">Optional day filter (1-31)</param>
    /// <returns>Prize 3 digit history as a JSON string</returns>
    [KernelFunction]
    [Description("Get prize 3 digit history from the LottoAPI with month and day filters")]
    public async Task<string> GetPrize3DigitHistoryAsync(
        [Description("month filter (1-12)")] int month,
        [Description("day filter (1-31)")] int day
    )
    {
        //http://localhost:5182/api/lotto/prize3digithistory?month=12&day=16
        try
        {
            _logger.LogInformation(
                "🎯 GetPrize3DigitHistoryAsync called with Month: {Month}, Day: {Day}",
                month,
                day
            );

            String baseUrl = GetLottoAPIBaseUrl();

            var query = new List<string>();
            query.Add($"month={month}");
            query.Add($"day={day}");
            // if (month.HasValue)
            //  query.Add($"month={month.Value}");
            //if (day.HasValue)
            //  query.Add($"day={day.Value}");
            var queryString = query.Count > 0 ? "?" + string.Join("&", query) : "";

            var url = $"{baseUrl}/api/lotto/prize3digithistory{queryString}";
            _logger.LogInformation("🎯 Calling LottoAPI at {Url}", url);

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling LottoAPI");
            return $"Error calling LottoAPI: {ex.Message}";
        }
    }

    /// <summary>
    /// Gets the base URL for the LottoAPI from the configuration.
    /// </summary>
    /// <returns>The base URL string for the LottoAPI, or an error message if not configured.</returns>

    public string GetLottoAPIBaseUrl()
    {
        var _configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        var config = _configuration.GetSection("LottoAPI");
        if (config == null || string.IsNullOrEmpty(config["BaseUrl"]))
        {
            _logger.LogError("LottoAPI configuration is missing or invalid.");
            return "LottoAPI configuration is missing or invalid.";
        }
        return config["BaseUrl"].TrimEnd('/');
    }
    #endregion > LottoAPI Integration
}
