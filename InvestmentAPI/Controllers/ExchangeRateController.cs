using InvestmentAPI.Data;
using InvestmentAPI.Models;
using InvestmentAPI.Services;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ExchangeRateController : ControllerBase
{
    private readonly ExchangeRateService _exchangeRateService;

    public ExchangeRateController(ExchangeRateService exchangeRateService)
    {
        _exchangeRateService = exchangeRateService;
    }

    [HttpGet("historical")]
    public async Task<IActionResult> GetHistoricalRates(string date)
    {
        var rates = await _exchangeRateService.GetHistoricalRatesAsync(date);
        return Ok(rates);
    }

    [HttpGet("latest")]
    public async Task<IActionResult> GetLatestRates(string baseCurrency)
    {
        var rates = await _exchangeRateService.GetLatestRatesAsync(baseCurrency);
        return Ok(rates);
    }

    [HttpGet("specificrate")]
    public async Task<IActionResult> GetSpecificRate(
        string date,
        string baseCurrency,
        string targetCurrency,
        bool isMatchDay
    )
    {
        var rate = await _exchangeRateService.GetSpecificRateAsync(
            date,
            baseCurrency,
            targetCurrency,
            isMatchDay
        );
        if (rate != null)
            return Ok(rate);
        else
            return NotFound("Rate not found");
    }
}
