using System.Globalization;
using DevinChat.Server.Data;
using DevinChat.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace DevinChat.Server.Hubs;

[Authorize]
public class ChatHub(ChatDbContext db) : Hub
{
    private int CurrentUserId => int.Parse(
        Context.UserIdentifier ?? throw new HubException("Unauthenticated."),
        CultureInfo.InvariantCulture);

    public override async Task OnConnectedAsync()
    {
        await Clients.Others.SendAsync("PresenceChanged", CurrentUserId, true);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await Clients.Others.SendAsync("PresenceChanged", CurrentUserId, false);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task<MessageDto> SendMessage(int recipientId, string text)
    {
        text = (text ?? string.Empty).Trim();
        if (text.Length == 0)
        {
            throw new HubException("Message text cannot be empty.");
        }

        var senderId = CurrentUserId;
        if (!await db.Users.AnyAsync(u => u.Id == recipientId))
        {
            throw new HubException("Recipient does not exist.");
        }

        var message = new Message { SenderId = senderId, RecipientId = recipientId, Text = text };
        db.Messages.Add(message);
        await EnsureMutualContactsAsync(senderId, recipientId);
        await db.SaveChangesAsync();

        var dto = new MessageDto(message.Id, message.SenderId, message.RecipientId, message.Text, message.SentAtUtc);
        await Clients.User(recipientId.ToString(CultureInfo.InvariantCulture)).SendAsync("ReceiveMessage", dto);
        return dto;
    }

    private async Task EnsureMutualContactsAsync(int a, int b)
    {
        if (a == b)
        {
            return;
        }

        if (!await db.Contacts.AnyAsync(c => c.OwnerId == a && c.ContactUserId == b))
        {
            db.Contacts.Add(new Contact { OwnerId = a, ContactUserId = b });
        }

        if (!await db.Contacts.AnyAsync(c => c.OwnerId == b && c.ContactUserId == a))
        {
            db.Contacts.Add(new Contact { OwnerId = b, ContactUserId = a });
        }
    }
}
