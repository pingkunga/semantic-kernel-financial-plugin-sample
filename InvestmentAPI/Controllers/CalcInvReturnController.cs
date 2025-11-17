using Microsoft.AspNetCore.Mvc;
using InvestmentAPI.Services;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[McpServerToolType]
[McpServerResourceType]
[McpServerPromptType]
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

    [McpServerTool(Name = "calc_simple_return", Title = "Calculate Simple Return")]
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

    [McpServerTool(Name = "calc_cagr", Title = "Calculate Compound Annual Growth Rate (CAGR)")]
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

    [McpServerResource(
        UriTemplate = "api://calcinvreturn/formulas/{type}",
        Name = "investment_calculation_formula",
        MimeType = "text/plain"
    )]
    [Description("Returns the formula for simple return or CAGR as plain text")]
    [HttpGet("formulas/{type}")]
    public string GetCalculationFormula(string type)
    {
        if (type.ToLower() == "simple-return")
        {
            return "Simple Return Formula: (Final Value - Initial Value) / Initial Value";
        }
        else if (type.ToLower() == "cagr")
        {
            return "CAGR Formula: (Final Value / Initial Value)^(1 / Years) - 1";
        }
        else
        {
            return "Invalid type. Available: simple-return, cagr";
        }
    }

    [McpServerResource(
        UriTemplate = "api://calcinvreturn/examples/{type}",
        Name = "investment_calculation_example",
        MimeType = "application/json"
    )]
    [Description("Returns an example calculation for simple return or CAGR as JSON")]
    [HttpGet("examples/{type}")]
    public string GetCalculationExample(string type)
    {
        if (type.ToLower() == "simple-return")
        {
            var example = new
            {
                Type = "SimpleReturn",
                InitialValue = 1000,
                FinalValue = 1200,
                Calculation = "(1200 - 1000) / 1000 = 0.2",
                Result = 0.2
            };
            return JsonSerializer.Serialize(example);
        }
        else if (type.ToLower() == "cagr")
        {
            var example = new
            {
                Type = "CAGR",
                InitialValue = 1000,
                FinalValue = 1464.1,
                Years = 5,
                Calculation = "(1464.1 / 1000)^(1 / 5) - 1 ≈ 0.08",
                Result = 0.08
            };
            return JsonSerializer.Serialize(example);
        }
        else
        {
            return JsonSerializer.Serialize(
                new { Error = "Invalid type. Available: simple-return, cagr" }
            );
        }
    }

    [
        McpServerPrompt(Name = "simple_return_prompt"),
        Description("A prompt for calculating simple return")
    ]
    public static string SimpleReturnPrompt() =>
        "Calculate the simple return for an investment from initial value to final value.";

    [McpServerPrompt(Name = "cagr_prompt"), Description("A prompt for calculating CAGR")]
    public static string CAGRPrompt() =>
        "Calculate the Compound Annual Growth Rate (CAGR) for an investment over a number of years.";

    [
        McpServerPrompt(Name = "investment_advice_prompt"),
        Description("Generates investment advice based on CAGR")
    ]
    public string GenerateInvestmentAdvicePrompt(double cagr, int years)
    {
        return $"Based on a CAGR of {cagr:F2}% over {years} years, consider diversifying your portfolio and consulting a financial advisor for long-term growth.";
    }

    [
        McpServerPrompt(Name = "risk_assessment_prompt"),
        Description("Provides a risk assessment prompt for investments")
    ]
    public string RiskAssessmentPrompt(string investmentType)
    {
        return $"Assess the risk for {investmentType} investments: Consider market volatility, historical returns, and your risk tolerance before investing.";
    }
}
