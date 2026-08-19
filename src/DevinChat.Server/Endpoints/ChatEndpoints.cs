using System.Globalization;
using System.Security.Claims;
using DevinChat.Server.Data;
using DevinChat.Server.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevinChat.Server.Endpoints;

public static class ChatEndpoints
{
    public static void MapChatEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api").RequireAuthorization();

        group.MapGet("/me", async (ClaimsPrincipal principal, ChatDbContext db) =>
        {
            var user = await db.Users.FindAsync(UserId(principal));
            return user is null ? Results.NotFound() : Results.Ok(AuthEndpoints.ToDto(user));
        });

        group.MapGet("/users/search", async (string q, ClaimsPrincipal principal, ChatDbContext db) =>
        {
            var meId = UserId(principal);
            var term = (q ?? string.Empty).Trim().ToLowerInvariant();
            if (term.Length == 0)
            {
                return Results.Ok(Array.Empty<UserDto>());
            }

            var users = await db.Users
                .Where(u => u.Id != meId && (u.UserName.Contains(term) || u.DisplayName.Contains(term)))
                .OrderBy(u => u.UserName)
                .Take(20)
                .Select(u => new UserDto(u.Id, u.UserName, u.DisplayName))
                .ToListAsync();

            return Results.Ok(users);
        });

        group.MapGet("/contacts", async (ClaimsPrincipal principal, ChatDbContext db) =>
        {
            var meId = UserId(principal);
            var contacts = await db.Contacts
                .Where(c => c.OwnerId == meId)
                .Join(db.Users, c => c.ContactUserId, u => u.Id, (c, u) => u)
                .OrderBy(u => u.UserName)
                .Select(u => new UserDto(u.Id, u.UserName, u.DisplayName))
                .ToListAsync();

            return Results.Ok(contacts);
        });

        group.MapPost("/contacts", async ([FromBody] AddContactRequest request, ClaimsPrincipal principal, ChatDbContext db) =>
        {
            var meId = UserId(principal);
            var userName = (request.UserName ?? string.Empty).Trim().ToLowerInvariant();
            var target = await db.Users.FirstOrDefaultAsync(u => u.UserName == userName);
            if (target is null)
            {
                return Results.NotFound(new { error = $"No user named '{userName}'." });
            }

            if (target.Id == meId)
            {
                return Results.BadRequest(new { error = "You cannot add yourself." });
            }

            if (!await db.Contacts.AnyAsync(c => c.OwnerId == meId && c.ContactUserId == target.Id))
            {
                db.Contacts.Add(new Contact { OwnerId = meId, ContactUserId = target.Id });
            }

            if (!await db.Contacts.AnyAsync(c => c.OwnerId == target.Id && c.ContactUserId == meId))
            {
                db.Contacts.Add(new Contact { OwnerId = target.Id, ContactUserId = meId });
            }

            await db.SaveChangesAsync();
            return Results.Ok(AuthEndpoints.ToDto(target));
        });

        group.MapGet("/messages/{otherUserId:int}", async (int otherUserId, ClaimsPrincipal principal, ChatDbContext db) =>
        {
            var meId = UserId(principal);
            var messages = await db.Messages
                .Where(m => (m.SenderId == meId && m.RecipientId == otherUserId)
                            || (m.SenderId == otherUserId && m.RecipientId == meId))
                .OrderBy(m => m.SentAtUtc)
                .Select(m => new MessageDto(m.Id, m.SenderId, m.RecipientId, m.Text, m.SentAtUtc))
                .ToListAsync();

            return Results.Ok(messages);
        });
    }

    private static int UserId(ClaimsPrincipal principal) =>
        int.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!, CultureInfo.InvariantCulture);
}
