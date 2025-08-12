using Microsoft.AspNetCore.SignalR;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Text.Json;

namespace FinancialChatBotAPI.Hubs;

/// <summary>
/// SignalR Hub for real-time chat communication
/// </summary>
public class ChatHub : Hub
{
    private readonly Kernel _kernel;
    private readonly ILogger<ChatHub> _logger;
    private readonly PromptExecutionSettings _executionSettings;
    private readonly IConfiguration _config;

    private const string AI_ROLE = "AI";

    // key ConnectionId
    // value ChatHistory
    private static readonly Dictionary<string, ChatHistory> _userHistories = new();

    public ChatHub(Kernel kernel, ILogger<ChatHub> logger, IConfiguration config)
    {
        _kernel = kernel;
        _logger = logger;
        _config = config;

        // Enable automatic function calling
        if (_config["AIBackEnd"] == "OpenAI")
        {
            _executionSettings = new OpenAIPromptExecutionSettings()
            {
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
            };
        }
        else if (_config["AIBackEnd"] == "Ollama")
        {
            _executionSettings = new OllamaPromptExecutionSettings()
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            };
        }
        else
        {
            _executionSettings = new PromptExecutionSettings();
        }
    }

    /// <summary>
    /// Handle incoming chat messages from clients
    /// </summary>
    /// <param name="message">User's chat message</param>
    public async Task SendMessage(string message)
    {
        try
        {
            _logger.LogInformation("🚀 SignalR Chat message received: {Message}", message);

            // Notify other clients that user sent a message
            await Clients.Caller.SendAsync("ReceiveMessage", Context.ConnectionId, message, "user");

            // Show typing indicator
            await Clients.Caller.SendAsync("UserTyping", Context.ConnectionId, AI_ROLE, true);

            var chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();

            _logger.LogInformation("📡 Sending request to AI via SignalR...");

            // Create or retrieve chat history for the user
            var chatHistory = new ChatHistory();
            if (_userHistories.TryGetValue(Context.ConnectionId, out var existingHistory))
            {
                chatHistory = existingHistory;
            }
            else
            {
                // Add new chat history for the user
                chatHistory.AddSystemMessage(
                    "You are a helpful financial assistant. "
                        + "Use the available financial functions to help users with stock prices, market analysis, and financial calculations. "
                        + "Always call the appropriate functions when users ask for specific financial data."
                );

                _userHistories[Context.ConnectionId] = chatHistory;
            }

            // Add user message to the history
            chatHistory.AddUserMessage(message);

            var result = await chatCompletionService.GetChatMessageContentAsync(
                chatHistory,
                executionSettings: _executionSettings,
                kernel: _kernel
            );

            // Add AI response to the history
            chatHistory.AddAssistantMessage(result.Content ?? "No response generated.");

            // Hide typing indicator
            await Clients.Caller.SendAsync("UserTyping", Context.ConnectionId, AI_ROLE, false);

            // Send AI response
            await Clients.Caller.SendAsync(
                "ReceiveMessage",
                "AI",
                result.Content ?? "No response generated.",
                "bot"
            );

            _logger.LogInformation(
                "✅ SignalR AI response sent: {Response}",
                result.Content?.Substring(0, Math.Min(100, result.Content?.Length ?? 0))
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error in SignalR SendMessage: {Message}", ex.Message);

            // Hide typing indicator
            await Clients.Caller.SendAsync("UserTyping", Context.ConnectionId, AI_ROLE, false);

            // Send error message
            await Clients.Caller.SendAsync(
                "ReceiveMessage",
                "System",
                $"❌ Error: {ex.Message}",
                "error"
            );

            //Clear user history on error
            ClearUserChatHistory(_userHistories, Context.ConnectionId);
        }
    }

    /// <summary>
    /// Handle user joining the chat
    /// </summary>
    public async Task JoinChat(string userName)
    {
        _logger.LogInformation(
            "👋 User {UserName} joined chat with connection {ConnectionId}",
            userName,
            Context.ConnectionId
        );

        await Clients.All.SendAsync("UserJoined", userName, Context.ConnectionId);

        // Send welcome message to the new user
        await Clients.Caller.SendAsync(
            "ReceiveMessage",
            "System",
            "👋 Welcome to Financial ChatBot! Ask me about stocks, market data, or financial calculations.",
            "system"
        );
    }

    /// <summary>
    /// Handle user leaving the chat
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("👋 User disconnected: {ConnectionId}", Context.ConnectionId);
        await Clients.All.SendAsync("UserLeft", Context.ConnectionId);

        // Clear chat history for the user
        ClearUserChatHistory(_userHistories, Context.ConnectionId);

        await base.OnDisconnectedAsync(exception);
    }

    private void ClearUserChatHistory(IDictionary<string, ChatHistory> pUserHistories, String pConnectionId)
    {
        if (pUserHistories.ContainsKey(pConnectionId))
        {
            _logger.LogInformation(
                "🗑️ Clearing chat history for user {ConnectionId} on disconnect",
                pConnectionId
            );
            pUserHistories.Remove(pConnectionId);
        }
    }

    /// <summary>
    /// Handle typing indicators
    /// </summary>
    public async Task SendTyping(bool isTyping)
    {
        await Clients.Others.SendAsync("UserTyping", Context.ConnectionId, AI_ROLE, isTyping);
    }
}
