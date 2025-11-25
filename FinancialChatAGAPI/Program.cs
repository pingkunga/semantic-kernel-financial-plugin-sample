using GenerativeAI.Microsoft;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.DevUI;
using Microsoft.Agents.AI.Hosting.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Server;
using OllamaSharp;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using Azure.AI.OpenAI;
using Azure.Identity;

public class Program
{
    
    private static void Main(string[] args)
    {
        // Get API key from environment variables (recommended).
        string? aiEngineType = Environment.GetEnvironmentVariable("AI_ENGINE_TYPE");
        string? endpoint = Environment.GetEnvironmentVariable("AI_ENDPOINT");
        string? modelName = Environment.GetEnvironmentVariable("AI_MODEL_NAME");
        string? apiKey = Environment.GetEnvironmentVariable("AI_API_KEY");

        string? mcpServer = Environment.GetEnvironmentVariable("MCP_SERVER");

        //===================================================================

        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddOpenApi();

        builder.Services.AddSingleton<IChatClient>(
            provider => CreateChatClient(aiEngineType, endpoint, modelName, apiKey)
        );

        builder.AddAIAgent("Financial Assistant", (sp, key) =>
        {
            IClientTransport clientTransport = CreateClientTransport("http", new string[]
            {
                mcpServer ?? "http://localhost:5205"
            });

            var mcpClient = McpClient.CreateAsync(clientTransport!).GetAwaiter().GetResult();

            var tools = mcpClient.ListToolsAsync().GetAwaiter().GetResult();
            foreach (var tool in tools)
            {
                Console.WriteLine($"Connected to server with tools: {tool.Name}");
            }


            var chatClient = sp.GetRequiredService<IChatClient>();

            return new ChatClientAgent(
                chatClient,
                name: key,
                instructions:
                   """
                     You are a helpful financial assistant.
                     Use the available financial functions to help users with stock prices, market analysis, and financial calculations.
                     Always call the appropriate functions when users ask for specific financial data.
                    """,
                tools: [
                    .. tools.Cast<AITool>()
                ]
            );
        });

        // Register services for OpenAI responses and conversations (also required for DevUI)
        builder.AddOpenAIResponses();
        builder.AddOpenAIConversations();
        

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();


            //Needed for DevUI to function 
            app.MapOpenAIResponses();
            app.MapOpenAIConversations();
            app.MapDevUI();
        }

        app.MapGet("/", () => "Hello World!");

        app.Run();
    }

    public const string AIEngine_AZURE = "Azure";
    public const string AIEngine_OPENAI = "OpenAI";
    public const string AIEngine_OLLAMA = "Ollama";
    public const string AIEngine_GEMINI = "Gemini";

    public static IChatClient CreateChatClient(
        String? pType,
        String? pEndpoint,
        String? pModelName,
        String? pKey
    )
    {
        //validate parameters
        if (string.IsNullOrEmpty(pType))
            throw new ArgumentNullException(nameof(pType), "AI engine type is not configured.");
        if (
            string.IsNullOrEmpty(pEndpoint)
            && (
                AIEngine_AZURE.Equals(pType, StringComparison.OrdinalIgnoreCase)
                || AIEngine_OLLAMA.Equals(pType, StringComparison.OrdinalIgnoreCase)
            )
        )
            throw new ArgumentNullException(nameof(pEndpoint), "AI endpoint is not configured.");
        if (
            string.IsNullOrEmpty(pModelName)
            && (
                AIEngine_OLLAMA.Equals(pType, StringComparison.OrdinalIgnoreCase)
                || AIEngine_GEMINI.Equals(pType, StringComparison.OrdinalIgnoreCase)
            )
        )
            throw new ArgumentNullException(nameof(pModelName), "AI model name is not configured.");
        if (
            string.IsNullOrEmpty(pKey)
            && (
                AIEngine_OPENAI.Equals(pType, StringComparison.OrdinalIgnoreCase)
                || AIEngine_GEMINI.Equals(pType, StringComparison.OrdinalIgnoreCase)
            )
        )
            throw new ArgumentNullException(nameof(pKey), "AI API key is not configured.");

        if (AIEngine_AZURE.Equals(pType, StringComparison.OrdinalIgnoreCase))
        {
            // var endpoint = "https://my-openai.openai.azure.com/";
            // var modelId = "my-gpt-4o-mini"; // this is the deployment name
            // var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions() {  
            //                     TenantId = "Example-e89b-12d3-a456-426614174000" });

            // Ref: https://learn.microsoft.com/en-us/dotnet/ai/how-to/content-filtering
            // IChatClient client =
            //     new AzureOpenAIClient(
            //         new Uri("YOUR_MODEL_ENDPOINT"),
            //         new DefaultAzureCredential()).GetChatClient("YOUR_MODEL_DEPLOYMENT_NAME").AsIChatClient();
            var credential = new ApiKeyCredential(pKey!);
            IChatClient client =new AzureOpenAIClient(new Uri(pEndpoint!),credential)
                                        .GetChatClient(pModelName)
                                        .AsIChatClient();
            
            return client;

            // Generic install Microsoft.Extensions.AI.AzureAIInference nuget
            // https://devblogs.microsoft.com/dotnet/introducing-microsoft-extensions-ai-preview/#chat
            // IChatClient client =new ChatCompletionsClient(endpoint: new Uri("https://models.inference.ai.azure.com"), 
            //                             new AzureKeyCredential(Environment.GetEnvironmentVariable("GH_TOKEN")))
            //                             .AsChatClient("Phi-3.5-MoE-instruct");
        }
        else if (AIEngine_OPENAI.Equals(pType, StringComparison.OrdinalIgnoreCase))
        {
            return new ChatClient(
                    pModelName ?? "gpt-4o-mini",
                    new ApiKeyCredential(pKey!), 
                    new OpenAIClientOptions { Endpoint = new Uri(pEndpoint!) })
                .AsIChatClient();
        }
        else if (AIEngine_OLLAMA.Equals(pType, StringComparison.OrdinalIgnoreCase))
        {
            return new OllamaApiClient(new Uri(pEndpoint!), pModelName!);
        }
        else if (AIEngine_GEMINI.Equals(pType, StringComparison.OrdinalIgnoreCase))
        {
            return new GenerativeAIChatClient(pKey!, pModelName!);
        }

        throw new NotSupportedException($"The AI engine type '{pType}' is not supported.");
    }

    public static IClientTransport CreateClientTransport(String pCommandType, String[] pArguments)
    {
        IClientTransport clientTransport;
        var command = pCommandType.ToLowerInvariant();
        if (command == "http")
        {
            // make sure AspNetCoreMcpServer is running
            clientTransport = new HttpClientTransport(new() { Endpoint = new Uri(pArguments[0]) });
        }
        else
        {
            clientTransport = new StdioClientTransport(
                new()
                {
                    Name = "Demo Server",
                    Command = command,
                    Arguments = pArguments,
                }
            );
        }
        return clientTransport;
    }
}
