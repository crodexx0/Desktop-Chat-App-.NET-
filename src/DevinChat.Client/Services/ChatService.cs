using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using DevinChat.Client.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace DevinChat.Client.Services;

/// <summary>Talks to the DevinChat server: REST for auth/contacts/history, SignalR for live messages.</summary>
public class ChatService : IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private HubConnection? _hub;
    private string? _token;

    public ChatService(string baseAddress)
    {
        BaseAddress = baseAddress.TrimEnd('/');
        _http = new HttpClient { BaseAddress = new Uri(BaseAddress + "/") };
    }

    public string BaseAddress { get; }

    public UserDto? CurrentUser { get; private set; }

    public event Action<MessageDto>? MessageReceived;

    public event Action<Exception?>? ConnectionClosed;

    public async Task<UserDto> RegisterAsync(string userName, string displayName, string password)
    {
        var response = await _http.PostAsJsonAsync("api/auth/register", new { userName, displayName, password }, JsonOptions);
        return await CompleteAuthAsync(response);
    }

    public async Task<UserDto> LoginAsync(string userName, string password)
    {
        var response = await _http.PostAsJsonAsync("api/auth/login", new { userName, password }, JsonOptions);
        return await CompleteAuthAsync(response);
    }

    public async Task<IReadOnlyList<UserDto>> GetContactsAsync() =>
        await _http.GetFromJsonAsync<List<UserDto>>("api/contacts", JsonOptions) ?? [];

    public async Task<UserDto> AddContactAsync(string userName)
    {
        var response = await _http.PostAsJsonAsync("api/contacts", new { userName }, JsonOptions);
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<UserDto>(JsonOptions))!;
    }

    public async Task<IReadOnlyList<MessageDto>> GetMessagesAsync(int otherUserId) =>
        await _http.GetFromJsonAsync<List<MessageDto>>($"api/messages/{otherUserId}", JsonOptions) ?? [];

    public async Task ConnectAsync()
    {
        if (_token is null)
        {
            throw new InvalidOperationException("Sign in before connecting.");
        }

        _hub = new HubConnectionBuilder()
            .WithUrl($"{BaseAddress}/hubs/chat", options => options.AccessTokenProvider = () => Task.FromResult<string?>(_token))
            .WithAutomaticReconnect()
            .Build();

        _hub.On<MessageDto>("ReceiveMessage", message => MessageReceived?.Invoke(message));
        _hub.Closed += error =>
        {
            ConnectionClosed?.Invoke(error);
            return Task.CompletedTask;
        };

        await _hub.StartAsync();
    }

    public async Task<MessageDto> SendMessageAsync(int recipientId, string text)
    {
        if (_hub is null || _hub.State != HubConnectionState.Connected)
        {
            throw new InvalidOperationException("Not connected to the chat server.");
        }

        return await _hub.InvokeAsync<MessageDto>("SendMessage", recipientId, text);
    }

    public async Task SignOutAsync()
    {
        if (_hub is not null)
        {
            await _hub.DisposeAsync();
            _hub = null;
        }

        _token = null;
        CurrentUser = null;
        _http.DefaultRequestHeaders.Authorization = null;
    }

    public async ValueTask DisposeAsync()
    {
        await SignOutAsync();
        _http.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task<UserDto> CompleteAuthAsync(HttpResponseMessage response)
    {
        await EnsureSuccessAsync(response);
        var auth = (await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions))!;
        _token = auth.Token;
        CurrentUser = auth.User;
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        return auth.User;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        string? message = null;
        try
        {
            message = (await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions))?.Error;
        }
        catch (JsonException)
        {
            // Fall back to the status code below.
        }

        throw new ChatServiceException(message ?? $"Request failed ({(int)response.StatusCode}).");
    }
}

public class ChatServiceException(string message) : Exception(message);
