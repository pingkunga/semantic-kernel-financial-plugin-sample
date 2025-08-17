using ds.opentelemetry;
using MongoDB.Driver;

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

        // Register MongoClient and IMongoDatabase
        builder.Services.AddSingleton<IMongoClient>(sp =>
            new MongoClient(builder.Configuration["MongoDb:ConnectionString"])
        );
        builder.Services.AddScoped<IMongoDatabase>(sp =>
        {
            var client = sp.GetRequiredService<IMongoClient>();
            return client.GetDatabase(builder.Configuration["MongoDb:Database"]);
        });

        builder.Services.AddScoped<LottoHistoryService>();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();

        app.MapControllers();
        app.Run();
    }
}
