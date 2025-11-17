using ds.opentelemetry;
using InvestmentAPI.Data;
using InvestmentAPI.Services;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;

public class Program
{
    private static async Task Main(string[] args)
    {
        // Check for --stdio flag
        bool isStdioMode = args.Contains("--stdio");

        if (isStdioMode)
        {
            // Stdio Mode: Run as console app for MCP Inspector
            await RunStdioMode();
            return;
        }

        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        builder.Services.AddControllers();

        builder.AddObservability();

        // Add PostgreSQL DbContext
        builder.Services.AddDbContext<InvestmentDbContext>(
            options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
        );

        builder.Services
            .AddMcpServer()
            .WithHttpTransport()
            .WithPromptsFromAssembly()
            .WithResourcesFromAssembly()
            .WithToolsFromAssembly();

        // Register services
        builder.Services.AddHttpClient<ExchangeRateService>();
        builder.Services.AddScoped<ExchangeRateService>();
        builder.Services.AddScoped<CalcInvReturnService>();

        var app = builder.Build();

        // Automatically create or migrate the database
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<InvestmentDbContext>();
            // Use EnsureCreated or Migrate
            dbContext.Database.Migrate(); // Applies migrations
            // dbContext.Database.EnsureCreated(); // Creates the database without migrations
        }

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }
        app.MapMcp();

        app.UseHttpsRedirection();
        app.UseAuthorization();

        app.MapControllers();
        app.Run();
    }

    private static async Task RunStdioMode()
    {
        var builder = Host.CreateApplicationBuilder(); // Use CreateApplicationBuilder for config
        builder.Configuration.AddJsonFile("appsettings.json", optional: true); // Load config
        // Add PostgreSQL DbContext
        builder.Services.AddDbContext<InvestmentDbContext>(
            options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
        );

        builder.Services
            .AddMcpServer()
            .WithStdioServerTransport()
            .WithPromptsFromAssembly()
            .WithResourcesFromAssembly()
            .WithToolsFromAssembly();

        // Register services
        builder.Services.AddHttpClient<ExchangeRateService>();
        builder.Services.AddScoped<ExchangeRateService>();
        builder.Services.AddScoped<CalcInvReturnService>();

        await builder.Build().RunAsync();

        //Apply migrations
        using (var scope = builder.Services.BuildServiceProvider().CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<InvestmentDbContext>();
            dbContext.Database.Migrate(); // Applies migrations
        }
    }
}
