namespace DevinChat.Server.Models;

public class User
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class Contact
{
    public int Id { get; set; }
    public int OwnerId { get; set; }
    public int ContactUserId { get; set; }
    public DateTime AddedAtUtc { get; set; } = DateTime.UtcNow;
}

public class Message
{
    public int Id { get; set; }
    public int SenderId { get; set; }
    public int RecipientId { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime SentAtUtc { get; set; } = DateTime.UtcNow;
}
