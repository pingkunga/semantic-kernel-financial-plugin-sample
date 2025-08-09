using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json; // This should be here
using System.Text.Json.Serialization; // Add this as well

/// <summary>
/// Financial ChatBot API Controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ChatController : ControllerBase
{
    private readonly Kernel _kernel;
    private readonly ILogger<ChatController> _logger;
    private readonly PromptExecutionSettings _executionSettings;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatController"/> class.
    /// </summary>
    /// <param name="logger">The logger instance for logging information and errors.</param>
    /// <param name="kernel">The Semantic Kernel instance for AI operations.</param>
    public ChatController(ILogger<ChatController> logger
                        , Kernel kernel)
    {
        _logger = logger;
        _kernel = kernel;
        
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();

        /*
        var builder = Kernel.CreateBuilder();

        // Configure AI model (Azure OpenAI or Ollama)
        if (config["AIBackEnd"] == "OpenAI")
        {
            builder.AddAzureOpenAIChatCompletion(
                deploymentName: "gpt-35-turbo",
                endpoint: config["AzureOpenAI:Endpoint"]!,
                apiKey: config["AzureOpenAI:ApiKey"]!
            );
        }
        else if (config["AIBackEnd"] == "Ollama")
        {
            builder.AddOllamaChatCompletion(
                modelId: config["Ollama:ModelId"]!,
                endpoint: new Uri(config["Ollama:Endpoint"]!)
            );
        }
        // Alternatively, if FinancialPlugin has a parameterless constructor
        //builder.Plugins.AddFromType<FinancialPlugin>();
        
        // Build the kernel
        _kernel = builder.Build();
        */

        // Enable automatic function calling
        if (config["AIBackEnd"] == "OpenAI")
        {
            _executionSettings = new OpenAIPromptExecutionSettings()
            {
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
            };
        }
        else if (config["AIBackEnd"] == "Ollama")
        {
            // For Ollama, use the base PromptExecutionSettings or OllamaPromptExecutionSettings
            _executionSettings = new OllamaPromptExecutionSettings()
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            };
        }
        else
        {
            // Default fallback
            _executionSettings = new PromptExecutionSettings();
        }
        _logger.LogInformation("ChatController initialized with AI backend: {AIBackEnd}", config["AIBackEnd"]);
    }

    /// <summary>
    /// Chat with the financial assistant AI
    /// </summary>
    /// <param name="request">The chat request containing the user's query</param>
    /// <returns>AI response with financial information</returns>
    /// <response code="200">Returns the AI's response to the financial query</response>
    /// <response code="400">If the request is invalid</response>
    [HttpPost]
    [ProducesResponseType(typeof(ChatResponse), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            _logger.LogInformation("🚀 Chat request received: {Query}", request.Query);

            var chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();

            _logger.LogInformation("📡 Sending request to AI with function calling enabled...");
            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage(
                "You are a helpful financial assistant. "
                    + "Use the available financial functions to help users with stock prices, market analysis, and financial calculations. "
                    + "Always call the appropriate functions when users ask for specific financial data."
            );
            chatHistory.AddUserMessage(request.Query);

            var result = await chatCompletionService.GetChatMessageContentAsync(
                chatHistory,
                executionSettings: _executionSettings,
                kernel: _kernel
            );

            _logger.LogInformation(
                "✅ AI response received: {Response}",
                result.Content?.Substring(0, Math.Min(100, result.Content?.Length ?? 0))
            );

            return Ok(new ChatResponse { Response = result.Content ?? "No response generated." });
        }
        catch (Exception ex)
        {
            _logger.LogError("❌ Error in Chat method: {Error}", ex.Message);
            return StatusCode(
                500,
                new
                {
                    Error = "An error occurred while processing your request.",
                    Details = ex.Message
                }
            );
        }
    }

    /// <summary>
    /// Health check endpoint to verify the API is running
    /// </summary>
    /// <returns>API status information</returns>
    /// <response code="200">Returns the API health status</response>
    [HttpGet("health")]
    [ProducesResponseType(typeof(HealthResponse), 200)]
    public IActionResult Health()
    {
        return Ok(
            new HealthResponse
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Version = "1.0.0"
            }
        );
    }

    /// <summary>
    /// Get available functions for debugging
    /// </summary>
    /// <returns>List of available kernel functions</returns>
    [HttpGet("functions")]
    [ProducesResponseType(typeof(object), 200)]
    public IActionResult GetAvailableFunctions()
    {
        try
        {
            var plugins = _kernel.Plugins;
            var functionsList = new List<object>();

            foreach (var plugin in plugins)
            {
                foreach (var function in plugin)
                {
                    functionsList.Add(
                        new
                        {
                            PluginName = plugin.Name,
                            FunctionName = function.Name,
                            Description = function.Description,
                            Parameters = function.Metadata.Parameters.Select(
                                p =>
                                    new
                                    {
                                        Name = p.Name,
                                        Description = p.Description,
                                        Type = p.ParameterType?.Name
                                    }
                            )
                        }
                    );
                }
            }

            return Ok(new { TotalFunctions = functionsList.Count, Functions = functionsList });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Test function calling directly
    /// </summary>
    /// <param name="functionName">Name of the function to test</param>
    /// <param name="parameters">Parameters as JSON string</param>
    /// <returns>Function execution result</returns>
    [HttpPost("test-function")]
    public async Task<IActionResult> TestFunction(
        [FromQuery] string functionName,
        [FromBody] Dictionary<string, object> parameters
    )
    {
        try
        {
            _logger.LogInformation(
                "🧪 Testing function: {FunctionName} with parameters: {Parameters}",
                functionName,
                JsonSerializer.Serialize(parameters)
            );

            var function = _kernel.Plugins.GetFunction("FinancialPlugin", functionName);
            var arguments = new KernelArguments();

            foreach (var param in parameters)
            {
                arguments[param.Key] = param.Value;
            }

            var result = await function.InvokeAsync(_kernel, arguments);

            _logger.LogInformation(
                "✅ Function test completed: {FunctionName} with result: {Result}",
                functionName,
                result.GetValue<object>()?.ToString()
            );

            return Ok(
                new
                {
                    FunctionName = functionName,
                    Parameters = parameters,
                    Result = result.GetValue<object>()?.ToString()
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError("❌ Error in TestFunction: {Error}", ex.Message);
            return StatusCode(500, new { Error = ex.Message });
        }
    }
}

/// <summary>
/// Request model for chat interactions
/// </summary>
public class ChatRequest
{
    /// <summary>
    /// The user's query or question about financial topics
    /// </summary>
    /// <example>What is the current stock price of AAPL?</example>
    [Required]
    [StringLength(
        1000,
        MinimumLength = 1,
        ErrorMessage = "Query must be between 1 and 1000 characters"
    )]
    public required string Query { get; set; }
}

/// <summary>
/// Response model for chat interactions
/// </summary>
public class ChatResponse
{
    /// <summary>
    /// The AI assistant's response to the user's query
    /// </summary>
    public required string Response { get; set; }
}

/// <summary>
/// Response model for health check
/// </summary>
public class HealthResponse
{
    /// <summary>
    /// The current status of the API
    /// </summary>
    public required string Status { get; set; }

    /// <summary>
    /// The timestamp when the health check was performed
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// The version of the API
    /// </summary>
    public required string Version { get; set; }
}
