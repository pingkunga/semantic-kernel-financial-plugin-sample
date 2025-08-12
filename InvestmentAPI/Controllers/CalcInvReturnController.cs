using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CalcInvReturnController : ControllerBase
{
    private readonly ILogger<CalcInvReturnController> _logger;

    public CalcInvReturnController(ILogger<CalcInvReturnController> logger)
    {
        _logger = logger;
    }

    [HttpGet("simple-return")]
    public IActionResult SimpleReturn(double initialValue, double finalValue)
    {
        _logger.LogInformation(
            "Calculating simple return for initial value: {InitialValue}, final value: {FinalValue}",
            initialValue,
            finalValue
        );

        if (initialValue <= 0)
            return BadRequest("Initial value must be greater than zero.");

        double result = ((finalValue - initialValue) / initialValue) * 100.0;
        return Ok(new { SimpleReturn = result });
    }

    [HttpGet("cagr")]
    public IActionResult CAGR(double initialValue, double finalValue, int years)
    {
        _logger.LogInformation(
            "Calculating compound annual growth rate (CAGR) for initial value: {InitialValue}, final value: {FinalValue}, years: {Years}",
            initialValue,
            finalValue,
            years
        );

        if (initialValue <= 0 || years <= 0)
            return BadRequest("Initial value and years must be greater than zero.");
        double result = (Math.Pow(finalValue / initialValue, 1.0 / years) - 1) * 100.0;
        return Ok(new { CAGR = result });
    }
}
