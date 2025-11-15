using ds.opentelemetry;
using InvestmentAPI.Data;
using InvestmentAPI.Services;
using Microsoft.EntityFrameworkCore;

public class Program
{
    private static void Main(string[] args)
    {
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

        app.UseHttpsRedirection();
        app.UseAuthorization();

        app.MapControllers();
        app.Run();
    }
}
