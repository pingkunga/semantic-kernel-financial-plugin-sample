using Microsoft.AspNetCore.Mvc;
using InvestmentAPI.Services;
using ModelContextProtocol.Server;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[McpServerToolType]
public class CalcInvReturnController : ControllerBase
{
    private readonly ILogger<CalcInvReturnController> _logger;
    private readonly CalcInvReturnService _calcService;

    public CalcInvReturnController(
        ILogger<CalcInvReturnController> logger,
        CalcInvReturnService calcService
    )
    {
        _logger = logger;
        _calcService = calcService;
    }

    [McpServerTool(
        Name = "Calc Simple Return from initialValue to finalValue",
        Title = "Calculate Simple Return"
    )]
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

    [McpServerTool(
        Name = "Calc CAGR from initialValue to finalValue over years",
        Title = "Calculate Compound Annual Growth Rate (CAGR)"
    )]
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
