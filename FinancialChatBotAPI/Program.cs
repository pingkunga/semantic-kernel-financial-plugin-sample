using ds.opentelemetry;
using FinancialChatBotAPI.Hubs;
using Microsoft.SemanticKernel;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllers();

        // Register FinancialPlugin for DI
        builder.Services.AddTransient<FinancialPlugin>();

        // Build the kernel and register it as a singleton
        builder.Services.AddSingleton(provider =>
        {
            var config = builder.Configuration;

            var kernelBuilder = Kernel.CreateBuilder();

            // Configure AI model
            if (config["AIBackEnd"] == "OpenAI")
            {
                kernelBuilder.AddAzureOpenAIChatCompletion(
                    deploymentName: "gpt-35-turbo",
                    endpoint: config["AzureOpenAI:Endpoint"]!,
                    apiKey: config["AzureOpenAI:ApiKey"],
                    httpClient: new() { Timeout = TimeSpan.FromMinutes(5) }
                );
            }
            else if (config["AIBackEnd"] == "Ollama")
            {
                kernelBuilder.AddOllamaChatCompletion(
                    modelId: config["Ollama:ModelId"]!,
                    httpClient: new()
                    {
                        BaseAddress = new Uri(config["Ollama:Endpoint"]!),
                        Timeout = TimeSpan.FromMinutes(5)
                    }
                );
            }

            // Add plugin from DI
            var financialPlugin = provider.GetRequiredService<FinancialPlugin>();
            kernelBuilder.Plugins.AddFromObject(financialPlugin);

            return kernelBuilder.Build();
        });

        // Add SignalR
        builder.Services.AddSignalR();

        // Add CORS for Blazor WebAssembly
        builder.Services.AddCors(options =>
        {
            options.AddPolicy(
                "AllowBlazorWasm",
                policy =>
                {
                    if (builder.Environment.IsDevelopment())
                    {
                        // In development: Allow any origin
                        policy
                            .SetIsOriginAllowed(origin => true) // Allow any origin
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials(); // Important for SignalR
                    }
                    else
                    {
                        // In production: Restrict to specific origins
                        policy
                            .WithOrigins("https://yourdomain.com", "https://www.yourdomain.com")
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials();
                    }
                }
            );
        });

        // Configure Swagger/OpenAPI
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc(
                "v1",
                new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "Financial ChatBot API",
                    Version = "v1",
                    Description =
                        @"A Financial ChatBot API powered by Semantic Kernel that provides stock prices and market analysis using AI                    
                            
                             ## Features
                            - REST API endpoints for traditional HTTP calls
                            - SignalR Hub for real-time chat communication
                            - Function calling with financial data
            
                            ## SignalR Hub
                            - Connect to `/chathub` for real-time chat functionality.
            
                            ## AI Backends
                            - OpenAI: Uses Azure OpenAI for chat completions.
                            - Ollama: Uses Ollama for chat completions.
                            
                            ##Sample Prompts
                            - What's the current price of Apple stock?
                            - ราคาของหุ้น Apple ตอนนี้
                            -------------------------
                            - Show me the market summary
                            - ขอดูภาพรวมตลาดหุ้น
                            - สรุปภาวะตลาดหน่อย
                            -------------------------
                            - If I invest $15,000 at 7% annual interest for 10 years, what will it be worth?
                            - ถ้าฉันลงทุน 15,000 ดอลลาร์ ด้วยอัตราดอกเบี้ย 7% ต่อปี เป็นเวลา 10 ปี มันจะมีค่าเท่าไหร่?
                            -------------------------
                            - Analyze Microsoft stock for me
                            - วิเคราะห์หุ้น Microsoft ให้ฉันหน่อย
                            -------------------------
                            - Convert 1000 USD to EUR
                            - แลกเงิน 1,000 ดอลลาร์อเมริกัน เป็นสกุลยูโร
                        ",
                    Contact = new Microsoft.OpenApi.Models.OpenApiContact
                    {
                        Name = "Financial ChatBot Support",
                        Email = "dev@debuggingsoft.com"
                    }
                }
            );

            // Enable XML documentation
            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath);
            }
        });

        builder.AddObservability();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Financial ChatBot API V1");
                c.RoutePrefix = string.Empty; // Set Swagger UI at the app's root
            });
        }

        // Use CORS
        app.UseCors("AllowBlazorWasm");

        app.UseHttpsRedirection();
        app.MapControllers();

        // Map SignalR Hub
        app.MapHub<ChatHub>("/chathub");

        app.Run();
    }
}
