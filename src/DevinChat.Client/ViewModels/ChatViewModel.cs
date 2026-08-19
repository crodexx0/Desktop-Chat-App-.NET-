using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevinChat.Client.Models;
using DevinChat.Client.Services;

namespace DevinChat.Client.ViewModels;

public partial class ChatViewModel : ViewModelBase
{
    private readonly ChatService _service;
    private readonly Action<string> _onSignedOut;

    [ObservableProperty]
    private ContactViewModel? _selectedContact;

    [ObservableProperty]
    private string _newContactUserName = string.Empty;

    [ObservableProperty]
    private string _draft = string.Empty;

    [ObservableProperty]
    private string? _statusMessage;

    public ChatViewModel()
        : this(new ChatService(MainViewModel.DefaultServerUrl), _ => { })
    {
    }

    public ChatViewModel(ChatService service, Action<string> onSignedOut)
    {
        _service = service;
        _onSignedOut = onSignedOut;
        _service.MessageReceived += OnMessageReceived;
    }

    public ObservableCollection<ContactViewModel> Contacts { get; } = [];

    public ObservableCollection<MessageViewModel> Messages { get; } = [];

    public string CurrentUserLabel => _service.CurrentUser is { } user
        ? $"{user.DisplayName} (@{user.UserName})"
        : "Not signed in";

    public bool HasSelectedContact => SelectedContact is not null;

    public async Task InitializeAsync()
    {
        try
        {
            foreach (var contact in await _service.GetContactsAsync())
            {
                Contacts.Add(new ContactViewModel(contact));
            }

            SelectedContact = Contacts.FirstOrDefault();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Could not load contacts: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task AddContactAsync()
    {
        var userName = NewContactUserName.Trim();
        if (userName.Length == 0)
        {
            return;
        }

        try
        {
            var user = await _service.AddContactAsync(userName);
            var existing = Contacts.FirstOrDefault(c => c.Id == user.Id);
            if (existing is null)
            {
                existing = new ContactViewModel(user);
                Contacts.Add(existing);
            }

            NewContactUserName = string.Empty;
            StatusMessage = $"Added @{user.UserName}.";
            SelectedContact = existing;
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task SendAsync()
    {
        var text = Draft.Trim();
        if (text.Length == 0 || SelectedContact is null)
        {
            return;
        }

        try
        {
            var sent = await _service.SendMessageAsync(SelectedContact.Id, text);
            Draft = string.Empty;
            Messages.Add(CreateMessage(sent));
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task SignOutAsync()
    {
        _service.MessageReceived -= OnMessageReceived;
        var serverUrl = _service.BaseAddress;
        await _service.DisposeAsync();
        _onSignedOut(serverUrl);
    }

    partial void OnSelectedContactChanged(ContactViewModel? value)
    {
        OnPropertyChanged(nameof(HasSelectedContact));
        Messages.Clear();
        if (value is not null)
        {
            value.HasUnread = false;
            _ = LoadHistoryAsync(value.Id);
        }
    }

    private async Task LoadHistoryAsync(int contactId)
    {
        try
        {
            var history = await _service.GetMessagesAsync(contactId);
            if (SelectedContact?.Id != contactId)
            {
                return;
            }

            Messages.Clear();
            foreach (var message in history)
            {
                Messages.Add(CreateMessage(message));
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Could not load messages: {ex.Message}";
        }
    }

    private void OnMessageReceived(MessageDto message) => Dispatcher.UIThread.Post(() =>
    {
        var contact = Contacts.FirstOrDefault(c => c.Id == message.SenderId);
        if (contact is null)
        {
            // Someone new started a conversation: refresh the contact list.
            _ = RefreshContactsAsync();
            StatusMessage = "New message from a new contact.";
            return;
        }

        if (SelectedContact?.Id == message.SenderId)
        {
            Messages.Add(CreateMessage(message));
        }
        else
        {
            contact.HasUnread = true;
        }
    });

    private async Task RefreshContactsAsync()
    {
        try
        {
            var contacts = await _service.GetContactsAsync();
            foreach (var contact in contacts.Where(c => Contacts.All(existing => existing.Id != c.Id)))
            {
                Contacts.Add(new ContactViewModel(contact) { HasUnread = true });
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Could not refresh contacts: {ex.Message}";
        }
    }

    private MessageViewModel CreateMessage(MessageDto message) =>
        new(message.Text, message.SenderId == _service.CurrentUser?.Id, message.SentAtUtc);
}

public partial class ContactViewModel(UserDto user) : ViewModelBase
{
    [ObservableProperty]
    private bool _hasUnread;

    public int Id { get; } = user.Id;

    public string UserName { get; } = user.UserName;

    public string DisplayName { get; } = user.DisplayName;

    public string Label => $"{DisplayName} (@{UserName})";
}

public class MessageViewModel(string text, bool isMine, DateTime sentAtUtc)
{
    public string Text { get; } = text;

    public bool IsMine { get; } = isMine;

    public string TimeLabel { get; } = sentAtUtc.ToLocalTime().ToString("HH:mm");

    public Avalonia.Layout.HorizontalAlignment Alignment { get; } =
        isMine ? Avalonia.Layout.HorizontalAlignment.Right : Avalonia.Layout.HorizontalAlignment.Left;
}
