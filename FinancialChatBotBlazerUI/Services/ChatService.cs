using Microsoft.AspNetCore.SignalR.Client;
using System.Collections.ObjectModel;

namespace FinancialChatBotBlazerUI.Services;

public class ChatService : IAsyncDisposable
{
    private HubConnection? _hubConnection;
    private readonly ObservableCollection<ChatMessage> _messages = new();
    private readonly string _hubUrl;
    private bool _disposed = false;

    public ChatService()
    {
        _hubUrl = "https://aiappapi.pingkunga.dev/chathub"; // Your API URL
    }

    private void InitializeConnection()
    {
        if (_hubConnection != null && !_disposed)
            return;

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(_hubUrl)
            .WithAutomaticReconnect() // Enable automatic reconnection
            .Build();

        // Subscribe to SignalR events
        _hubConnection.On<string, string, string>("ReceiveMessage", (user, message, type) =>
        {
            if (_disposed) return;
            
            var chatMessage = new ChatMessage
            {
                User = user,
                Content = message,
                MessageType = type,
                Timestamp = DateTime.Now,
                IsUser = type == "user"
            };

            _messages.Add(chatMessage);
            OnMessageReceived?.Invoke(chatMessage);
        });

        _hubConnection.On<string, bool>("UserTyping", (connectionId, isTyping) =>
        {
            if (_disposed) return;
            OnUserTyping?.Invoke(connectionId, isTyping);
        });

        _hubConnection.On<string, string>("UserJoined", (userName, connectionId) =>
        {
            if (_disposed) return;
            OnUserJoined?.Invoke(userName, connectionId);
        });

        _hubConnection.On<string>("UserLeft", (connectionId) =>
        {
            if (_disposed) return;
            OnUserLeft?.Invoke(connectionId);
        });

        // Handle reconnection events
        _hubConnection.Reconnecting += (exception) =>
        {
            if (_disposed) return Task.CompletedTask;
            OnConnectionStateChanged?.Invoke("Reconnecting");
            return Task.CompletedTask;
        };

        _hubConnection.Reconnected += (connectionId) =>
        {
            if (_disposed) return Task.CompletedTask;
            OnConnectionStateChanged?.Invoke("Connected");
            return Task.CompletedTask;
        };

        _hubConnection.Closed += (exception) =>
        {
            if (_disposed) return Task.CompletedTask;
            OnConnectionStateChanged?.Invoke("Disconnected");
            return Task.CompletedTask;
        };
    }

    public IReadOnlyCollection<ChatMessage> Messages => _messages.AsReadOnly();
    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;
    public string? ConnectionId => _hubConnection?.ConnectionId;

    // Events
    public event Action<ChatMessage>? OnMessageReceived;
    public event Action<string, bool>? OnUserTyping;
    public event Action<string, string>? OnUserJoined;
    public event Action<string>? OnUserLeft;
    public event Action<string>? OnConnectionStateChanged;

    public async Task StartAsync()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ChatService));

        try
        {
            InitializeConnection();
            
            if (_hubConnection?.State == HubConnectionState.Disconnected)
            {
                await _hubConnection.StartAsync();
                await _hubConnection.SendAsync("JoinChat", "User");
                OnConnectionStateChanged?.Invoke("Connected");
            }
        }
        catch (Exception ex)
        {
            OnConnectionStateChanged?.Invoke("Error");
            throw new InvalidOperationException($"Failed to connect to chat hub: {ex.Message}", ex);
        }
    }

    public async Task SendMessageAsync(string message)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ChatService));

        if (_hubConnection?.State != HubConnectionState.Connected)
            throw new InvalidOperationException("Not connected to chat hub");

        await _hubConnection.SendAsync("SendMessage", message);
    }

    public async Task SendTypingAsync(bool isTyping)
    {
        if (_disposed || _hubConnection?.State != HubConnectionState.Connected)
            return;

        try
        {
            await _hubConnection.SendAsync("SendTyping", isTyping);
        }
        catch
        {
            // Ignore typing errors
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (_hubConnection != null)
        {
            try
            {
                await _hubConnection.DisposeAsync();
            }
            catch
            {
                // Ignore disposal errors
            }
        }
    }
}

public class ChatMessage
{
    public string User { get; set; } = "";
    public string Content { get; set; } = "";
    public string MessageType { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public bool IsUser { get; set; }
}