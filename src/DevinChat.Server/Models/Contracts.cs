namespace DevinChat.Server.Models;

public record RegisterRequest(string UserName, string DisplayName, string Password);

public record LoginRequest(string UserName, string Password);

public record AuthResponse(string Token, UserDto User);

public record UserDto(int Id, string UserName, string DisplayName);

public record AddContactRequest(string UserName);

public record MessageDto(int Id, int SenderId, int RecipientId, string Text, DateTime SentAtUtc);
