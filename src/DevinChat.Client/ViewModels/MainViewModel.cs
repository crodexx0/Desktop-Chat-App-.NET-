using System;
using CommunityToolkit.Mvvm.ComponentModel;
using DevinChat.Client.Services;

namespace DevinChat.Client.ViewModels;

/// <summary>Shell view model that swaps between the login page and the chat page.</summary>
public partial class MainViewModel : ViewModelBase
{
    public const string DefaultServerUrl = "http://localhost:5099";

    [ObservableProperty]
    private ViewModelBase _currentPage;

    public MainViewModel()
    {
        var serverUrl = Environment.GetEnvironmentVariable("DEVINCHAT_SERVER") ?? DefaultServerUrl;
        _currentPage = new LoginViewModel(serverUrl, OnSignedIn);
    }

    private void OnSignedIn(ChatService service)
    {
        var chat = new ChatViewModel(service, SignOut);
        CurrentPage = chat;
        _ = chat.InitializeAsync();
    }

    private void SignOut(string serverUrl)
    {
        CurrentPage = new LoginViewModel(serverUrl, OnSignedIn);
    }
}
