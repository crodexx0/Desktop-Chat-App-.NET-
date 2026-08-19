using DevinChat.Server.Data;
using DevinChat.Server.Models;
using DevinChat.Server.Services;
using Microsoft.EntityFrameworkCore;

namespace DevinChat.Server.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapPost("/register", async (RegisterRequest request, ChatDbContext db, TokenService tokens) =>
        {
            var userName = (request.UserName ?? string.Empty).Trim().ToLowerInvariant();
            if (userName.Length < 3 || (request.Password ?? string.Empty).Length < 6)
            {
                return Results.BadRequest(new { error = "Username must be at least 3 characters and password at least 6." });
            }

            if (await db.Users.AnyAsync(u => u.UserName == userName))
            {
                return Results.Conflict(new { error = "That username is already taken." });
            }

            var user = new User
            {
                UserName = userName,
                DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? userName : request.DisplayName.Trim(),
                PasswordHash = PasswordHasher.Hash(request.Password!)
            };

            db.Users.Add(user);
            await db.SaveChangesAsync();

            return Results.Ok(new AuthResponse(tokens.CreateToken(user), ToDto(user)));
        });

        group.MapPost("/login", async (LoginRequest request, ChatDbContext db, TokenService tokens) =>
        {
            var userName = (request.UserName ?? string.Empty).Trim().ToLowerInvariant();
            var user = await db.Users.FirstOrDefaultAsync(u => u.UserName == userName);
            if (user is null || !PasswordHasher.Verify(request.Password ?? string.Empty, user.PasswordHash))
            {
                return Results.Json(new { error = "Invalid username or password." }, statusCode: StatusCodes.Status401Unauthorized);
            }

            return Results.Ok(new AuthResponse(tokens.CreateToken(user), ToDto(user)));
        });
    }

    internal static UserDto ToDto(User user) => new(user.Id, user.UserName, user.DisplayName);
}
