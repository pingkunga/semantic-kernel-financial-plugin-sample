using Microsoft.AspNetCore.Mvc;
using InvestmentAPI.Services;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CalcInvReturnController : ControllerBase
{
    private readonly ILogger<CalcInvReturnController> _logger;
    private readonly CalcInvReturnService _calcService;

    public CalcInvReturnController(
        ILogger<CalcInvReturnController> logger,
        CalcInvReturnService calcService)
    {
        _logger = logger;
        _calcService = calcService;
    }

    [HttpGet("simple-return")]
    public IActionResult SimpleReturn(double initialValue, double finalValue)
    {
        try
        {
            double result = _calcService.CalculateSimpleReturn(initialValue, finalValue);
            return Ok(new { SimpleReturn = result });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("cagr")]
    public IActionResult CAGR(double initialValue, double finalValue, int years)
    {
        try
        {
            double result = _calcService.CalculateCAGR(initialValue, finalValue, years);
            return Ok(new { CAGR = result });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
