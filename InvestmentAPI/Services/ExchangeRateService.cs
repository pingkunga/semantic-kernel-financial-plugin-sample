using InvestmentAPI.Data;
using InvestmentAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace InvestmentAPI.Services;

public class ExchangeRateService
{
    private readonly HttpClient _httpClient;
    private readonly InvestmentDbContext _dbContext;

    private string _appId;

    public ExchangeRateService(
        HttpClient httpClient,
        InvestmentDbContext dbContext,
        IConfiguration configuration
    )
    {
        _httpClient = httpClient;
        _dbContext = dbContext;

        // Read BaseUri and AppId from appsettings.json
        var baseUri = configuration["ExternalExchangeRateAPI:BaseUri"];
        _appId = configuration["ExternalExchangeRateAPI:AppId"];

        if (string.IsNullOrWhiteSpace(baseUri))
        {
            throw new ArgumentException("BaseUri is not configured in appsettings.json");
        }

        if (string.IsNullOrWhiteSpace(_appId))
        {
            throw new ArgumentException("AppId is not configured in appsettings.json");
        }

        _httpClient.BaseAddress = new Uri(baseUri);
    }

    //curl https://openexchangerates.org/api/historical/2025-08-24.json?app_id=e5bbe49b83024e1eb997d470470a1cd2
    public async Task<ExchangeRatesDocument?> GetHistoricalRatesAsync(string date)
    {
        var response = await _httpClient.GetAsync($"/historical/{date}.json?app_id={_appId}");
        // response.EnsureSuccessStatusCode();
        // var ratesDocument = await response.Content.ReadFromJsonAsync<ExchangeRatesDocument>();
        // return ratesDocument;
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            // Optionally log the errorContent here
            throw new HttpRequestException($"API Error: {response.StatusCode} - {errorContent}");
        }

        var ratesDocument = await response.Content.ReadFromJsonAsync<ExchangeRatesDocument>();
        return ratesDocument;
    }

    public async Task<ExchangeRatesDocument?> GetLatestRatesAsync(string baseCurrency)
    {
        var response = await _httpClient.GetAsync($"/latest?base={baseCurrency}");
        response.EnsureSuccessStatusCode();
        var ratesDocument = await response.Content.ReadFromJsonAsync<ExchangeRatesDocument>();
        return ratesDocument;
    }

    public async Task<ExchangeRateEntry?> GetSpecificRateAsync(
        string pMTMDate,
        string pBaseCurrency,
        string pTargetCurrency,
        bool pMatchDay
    )
    {
        #region validate
        if (!DateTime.TryParse(pMTMDate, out var mtmDate))
        {
            throw new ArgumentException("Invalid date format", nameof(pMTMDate));
        }

        if (mtmDate == DateTime.MinValue)
        {
            throw new ArgumentException("Invalid MTM date", nameof(pMTMDate));
        }

        if (string.IsNullOrWhiteSpace(pBaseCurrency))
        {
            throw new ArgumentException("Base currency must be provided", nameof(pBaseCurrency));
        }

        if (string.IsNullOrWhiteSpace(pTargetCurrency))
        {
            throw new ArgumentException(
                "Target currency must be provided",
                nameof(pTargetCurrency)
            );
        }
        #endregion validate

        pBaseCurrency = pBaseCurrency.ToUpperInvariant();
        pTargetCurrency = pTargetCurrency.ToUpperInvariant();

        ExchangeRateEntry? exchangeRateEntry = await FindSpecificRateFromDB(
            mtmDate,
            pBaseCurrency,
            pTargetCurrency,
            pMatchDay
        );
        if (exchangeRateEntry != null)
        {
            return exchangeRateEntry;
        }

        // If not found in DB, call API to get rates
        ExchangeRatesDocument ratesDocument = await GetHistoricalRatesAsync(pMTMDate);
        if (ratesDocument == null)
        {
            return null;
        }
        // Dump Results
        // Convert all rates to entries and add to DB for future use
        IList<ExchangeRateEntry> entries = ToEntries(ratesDocument, "OpenExchangeRates").ToList();
        _dbContext.ExchangeRates.AddRange(entries);
        await _dbContext.SaveChangesAsync(); // Persist changes to the database

        //Find from DB Again
        return await FindSpecificRateFromDB(mtmDate, pBaseCurrency, pTargetCurrency, pMatchDay);
    }

    public async Task<ExchangeRateEntry?> FindSpecificRateFromDB(
        DateTime pMTMDate,
        string pBaseCurrency,
        string pTargetCurrency,
        bool pMatchDay
    )
    {
        //Case Same Currency
        if (pBaseCurrency == pTargetCurrency)
        {
            return new ExchangeRateEntry
            {
                DataSource = "OpenExchangeRates",
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                MTMDate = pMTMDate,
                BaseCurrency = pBaseCurrency,
                Currency = pTargetCurrency,
                Rate = 1
            };
        }

        ExchangeRateEntry? cachedRate = GetSpecificRateFromDBAsync(
            pMTMDate,
            pBaseCurrency,
            pTargetCurrency,
            pMatchDay
        ).Result;

        if (cachedRate != null)
        {
            if (cachedRate.BaseCurrency == pBaseCurrency && cachedRate.Currency == pTargetCurrency)
            {
                return cachedRate;
            }
            else
            {
                return CalculateReciprocalRate(cachedRate);
            }
        }
        //ถ้าไม่พบใน DB ให้หา Cross Rate แทน เช่น USD -> THB ให้หา USD -> JPY และ JPY -> THB แทน
        //แต่ Data อิงกับ USD ถ้ามาเป็น THB > EUR
        // ---> USD > THB และ USD > EUR
        // ---> EUR/THB
        ExchangeRateEntry? cachedRateBase = GetSpecificRateFromDBAsync(
            pMTMDate,
            "USD",
            pBaseCurrency,
            pMatchDay
        ).Result;

        ExchangeRateEntry? cachedRateTarget = GetSpecificRateFromDBAsync(
            pMTMDate,
            "USD",
            pTargetCurrency,
            pMatchDay
        ).Result;

        if (cachedRateBase != null && cachedRateTarget != null)
        {
            // Found both base and target rates
            ExchangeRateEntry crossRate = CalculateCrossRate(cachedRateBase, cachedRateTarget);

            //dump to db for future used
            _dbContext.ExchangeRates.Add(crossRate);
            await _dbContext.SaveChangesAsync();

            return crossRate;
        }

        return null;
    }

    // private bool IsReciprocalRate(ExchangeRatesDocument entry, string pBaseCurrency)
    // {
    //     if (entry == null)
    //         throw new ArgumentNullException(nameof(entry));
    //     return entry.Base != pBaseCurrency;
    // }

    public ExchangeRateEntry CalculateReciprocalRate(ExchangeRateEntry entry)
    {
        if (entry == null)
            throw new ArgumentNullException(nameof(entry));

        return new ExchangeRateEntry
        {
            DataSource = entry.DataSource,
            Timestamp = entry.Timestamp,
            MTMDate = entry.MTMDate,
            BaseCurrency = entry.Currency,
            Currency = entry.BaseCurrency,
            Rate = 1 / entry.Rate
        };
    }

    public ExchangeRateEntry CalculateCrossRate(
        ExchangeRateEntry baseEntry,
        ExchangeRateEntry targetEntry
    )
    {
        if (baseEntry == null)
            throw new ArgumentNullException(nameof(baseEntry));
        if (targetEntry == null)
            throw new ArgumentNullException(nameof(targetEntry));

        return new ExchangeRateEntry
        {
            DataSource = "OpenExchangeRates",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            MTMDate = baseEntry.MTMDate,
            BaseCurrency = baseEntry.BaseCurrency,
            Currency = targetEntry.Currency,
            Rate = targetEntry.Rate / baseEntry.Rate
        };
    }

    public async Task<ExchangeRateEntry?> GetSpecificRateFromDBAsync(
        DateTime pDate,
        string pBaseCurrency,
        string pTargetCurrency,
        bool pMatchDay
    )
    {
        if (pMatchDay)
        {
            return await _dbContext.ExchangeRates
                .Where(
                    r =>
                        r.MTMDate == pDate
                        && (
                            (r.BaseCurrency == pBaseCurrency && r.Currency == pTargetCurrency)
                            || (r.BaseCurrency == pTargetCurrency && r.Currency == pBaseCurrency)
                        )
                )
                .FirstOrDefaultAsync();
        }
        else
        {
            return await _dbContext.ExchangeRates
                .Where(
                    r =>
                        r.MTMDate <= pDate
                        && (
                            (r.BaseCurrency == pBaseCurrency && r.Currency == pTargetCurrency)
                            || (r.BaseCurrency == pTargetCurrency && r.Currency == pBaseCurrency)
                        )
                )
                .OrderByDescending(r => r.MTMDate)
                .FirstOrDefaultAsync();
        }
    }

    public static IEnumerable<ExchangeRateEntry> ToEntries(
        ExchangeRatesDocument doc,
        string pSource
    )
    {
        foreach (var kvp in doc.Rates)
        {
            yield return new ExchangeRateEntry
            {
                DataSource = pSource,
                Timestamp = doc.Timestamp,
                MTMDate = DateTimeOffset.FromUnixTimeSeconds(doc.Timestamp).UtcDateTime.Date,
                BaseCurrency = doc.Base,
                Currency = kvp.Key,
                Rate = kvp.Value
            };
        }
    }

    public async Task<DateTime?> GetMaxDateForCurrencyPairAsync(
        string baseCurrency,
        string targetCurrency
    )
    {
        if (string.IsNullOrWhiteSpace(baseCurrency))
        {
            throw new ArgumentException("Base currency must be provided", nameof(baseCurrency));
        }

        if (string.IsNullOrWhiteSpace(targetCurrency))
        {
            throw new ArgumentException("Target currency must be provided", nameof(targetCurrency));
        }

        baseCurrency = baseCurrency.ToUpperInvariant();
        targetCurrency = targetCurrency.ToUpperInvariant();

        // If same currency, return current date as max date
        if (baseCurrency == targetCurrency)
        {
            return DateTime.UtcNow.Date;
        }

        var maxDate = await _dbContext.ExchangeRates
            .Where(
                r =>
                    (r.BaseCurrency == baseCurrency && r.Currency == targetCurrency)
                    || (r.BaseCurrency == targetCurrency && r.Currency == baseCurrency)
            )
            .MaxAsync(r => (DateTime?)r.MTMDate);

        return maxDate;
    }

    public async Task<IEnumerable<CurrencyPairMaxDate>> GetAllCurrencyPairsWithMaxDateAsync()
    {
        var pairs = await _dbContext.ExchangeRates
            .GroupBy(
                r =>
                    new
                    {
                        Base = r.BaseCurrency.CompareTo(r.Currency) < 0
                            ? r.BaseCurrency
                            : r.Currency,
                        Target = r.BaseCurrency.CompareTo(r.Currency) < 0
                            ? r.Currency
                            : r.BaseCurrency
                    }
            )
            .Select(
                g =>
                    new CurrencyPairMaxDate
                    {
                        BaseCurrency = g.Key.Base,
                        TargetCurrency = g.Key.Target,
                        MaxDate = g.Max(r => r.MTMDate)
                    }
            )
            .ToListAsync();

        return pairs;
    }

    public class CurrencyPairMaxDate
    {
        public required string BaseCurrency { get; set; }
        public required string TargetCurrency { get; set; }
        public required DateTime MaxDate { get; set; }
    }
}
