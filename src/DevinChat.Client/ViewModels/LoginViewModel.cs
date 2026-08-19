using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevinChat.Client.Services;

namespace DevinChat.Client.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    private readonly Action<ChatService> _onSignedIn;

    [ObservableProperty]
    private string _serverUrl;

    [ObservableProperty]
    private string _userName = string.Empty;

    [ObservableProperty]
    private string _displayName = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isRegisterMode;

    public LoginViewModel()
        : this(MainViewModel.DefaultServerUrl, _ => { })
    {
    }

    public LoginViewModel(string serverUrl, Action<ChatService> onSignedIn)
    {
        _serverUrl = serverUrl;
        _onSignedIn = onSignedIn;
    }

    public string SubmitText => IsRegisterMode ? "Create account" : "Sign in";

    public string ToggleText => IsRegisterMode ? "Already have an account? Sign in" : "New here? Create an account";

    partial void OnIsRegisterModeChanged(bool value)
    {
        ErrorMessage = null;
        OnPropertyChanged(nameof(SubmitText));
        OnPropertyChanged(nameof(ToggleText));
    }

    [RelayCommand]
    private void ToggleMode() => IsRegisterMode = !IsRegisterMode;

    [RelayCommand]
    private async Task SubmitAsync()
    {
        if (IsBusy)
        {
            return;
        }

        ErrorMessage = null;

        if (string.IsNullOrWhiteSpace(UserName) || string.IsNullOrEmpty(Password))
        {
            ErrorMessage = "Username and password are required.";
            return;
        }

        IsBusy = true;
        var service = new ChatService(ServerUrl);
        try
        {
            if (IsRegisterMode)
            {
                await service.RegisterAsync(UserName, DisplayName, Password);
            }
            else
            {
                await service.LoginAsync(UserName, Password);
            }

            await service.ConnectAsync();
            _onSignedIn(service);
        }
        catch (ChatServiceException ex)
        {
            ErrorMessage = ex.Message;
            await service.DisposeAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Could not reach the server at {ServerUrl}: {ex.Message}";
            await service.DisposeAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }
}
