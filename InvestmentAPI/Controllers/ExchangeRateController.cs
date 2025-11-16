using InvestmentAPI.Data;
using InvestmentAPI.Models;
using InvestmentAPI.Services;
using Microsoft.AspNetCore.Mvc;
using FinancialDomain.Shared.DTOs;
using ModelContextProtocol.Server;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[McpServerToolType]
public class ExchangeRateController : ControllerBase
{
    private readonly ExchangeRateService _exchangeRateService;

    public ExchangeRateController(ExchangeRateService exchangeRateService)
    {
        _exchangeRateService = exchangeRateService;
    }

    [McpServerTool(
        Name = "Get Historical Exchange Rates for a given date",
        Title = "Fetch Historical Exchange Rates"
    )]
    [HttpGet("historical")]
    public async Task<IActionResult> GetHistoricalRates(string date)
    {
        var rates = await _exchangeRateService.GetHistoricalRatesAsync(date);
        return Ok(rates);
    }

    [McpServerTool(
        Name = "Get Latest Exchange Rates for a base currency",
        Title = "Fetch Latest Exchange Rates"
    )]
    [HttpGet("latest")]
    public async Task<IActionResult> GetLatestRates(string baseCurrency)
    {
        var rates = await _exchangeRateService.GetLatestRatesAsync(baseCurrency);
        return Ok(rates);
    }

    [McpServerTool(
        Name = "Get Specific Exchange Rate for a date, base currency, and target currency",
        Title = "Fetch Specific Exchange Rate"
    )]
    [HttpGet("specificrate")]
    public async Task<IActionResult> GetSpecificRate(
        string date,
        string baseCurrency,
        string targetCurrency,
        bool isMatchDay
    )
    {
        try
        {
            ExchangeRateEntry? rate = await _exchangeRateService.GetSpecificRateAsync(
                date,
                baseCurrency,
                targetCurrency,
                isMatchDay
            );
            if (rate != null)
            {
                //map exchange rate entry to DTO
                ExchangeRateDTO rateDto =
                    new()
                    {
                        Id = rate.Id,
                        DataSource = rate.DataSource,
                        Timestamp = rate.Timestamp,
                        MTMDate = rate.MTMDate,
                        BaseCurrency = rate.BaseCurrency,
                        Currency = rate.Currency,
                        Rate = rate.Rate,
                        Amount = 1
                    };
                return Ok(rateDto);
            }
            else
            {
                return NotFound("Rate not found");
            }
        }
        catch (HttpRequestException ex)
        {
            // Optionally log ex.Message
            return StatusCode(502, new { error = "External API error", message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [McpServerTool(
        Name = "Get Maximum Date for Exchange Rate Data for a Currency Pair",
        Title = "Fetch Max Date for Currency Pair"
    )]
    [HttpGet("max-date")]
    public async Task<IActionResult> GetMaxDateForCurrencyPair(
        [FromQuery] string baseCurrency,
        [FromQuery] string targetCurrency
    )
    {
        try
        {
            DateTime? maxDate = await _exchangeRateService.GetMaxDateForCurrencyPairAsync(
                baseCurrency,
                targetCurrency
            );
            if (maxDate.HasValue)
            {
                return Ok(new { maxDate = maxDate.Value.ToString("yyyy-MM-dd") });
            }
            else
            {
                return NotFound("No exchange rate data found for the specified currency pair");
            }
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [McpServerTool(
        Name = "Get All Currency Pairs with Their Maximum Dates",
        Title = "Fetch All Currency Pairs Max Dates"
    )]
    [HttpGet("all-pairs-max-dates")]
    public async Task<IActionResult> GetAllCurrencyPairsWithMaxDate()
    {
        try
        {
            var pairs = await _exchangeRateService.GetAllCurrencyPairsWithMaxDateAsync();
            var result = pairs.Select(p => $"{p.BaseCurrency}-{p.TargetCurrency} {p.MaxDate:yyyy-MM-dd}");
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
}
