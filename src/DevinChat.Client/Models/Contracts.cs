using System;

namespace DevinChat.Client.Models;

public record UserDto(int Id, string UserName, string DisplayName);

public record AuthResponse(string Token, UserDto User);

public record MessageDto(int Id, int SenderId, int RecipientId, string Text, DateTime SentAtUtc);

public record ErrorResponse(string? Error);
