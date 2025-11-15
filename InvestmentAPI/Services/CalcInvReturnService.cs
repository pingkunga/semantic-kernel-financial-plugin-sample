using InvestmentAPI.Models;

namespace InvestmentAPI.Services;

public class CalcInvReturnService
{
    private readonly ILogger<CalcInvReturnService> _logger;

    public CalcInvReturnService(ILogger<CalcInvReturnService> logger)
    {
        _logger = logger;
    }

    public double CalculateSimpleReturn(double initialValue, double finalValue)
    {
        _logger.LogInformation(
            "Calculating simple return for initial value: {InitialValue}, final value: {FinalValue}",
            initialValue,
            finalValue
        );

        if (initialValue <= 0)
            throw new ArgumentException("Initial value must be greater than zero.");

        return ((finalValue - initialValue) / initialValue) * 100.0;
    }

    public double CalculateCAGR(double initialValue, double finalValue, int years)
    {
        _logger.LogInformation(
            "Calculating compound annual growth rate (CAGR) for initial value: {InitialValue}, final value: {FinalValue}, years: {Years}",
            initialValue,
            finalValue,
            years
        );

        if (initialValue <= 0 || years <= 0)
            throw new ArgumentException("Initial value and years must be greater than zero.");

        return (Math.Pow(finalValue / initialValue, 1.0 / years) - 1) * 100.0;
    }
}