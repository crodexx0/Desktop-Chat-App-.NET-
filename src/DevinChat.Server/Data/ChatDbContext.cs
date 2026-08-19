using DevinChat.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace DevinChat.Server.Data;

public class ChatDbContext(DbContextOptions<ChatDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<Message> Messages => Set<Message>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(u => u.UserName)
            .IsUnique();

        modelBuilder.Entity<Contact>()
            .HasIndex(c => new { c.OwnerId, c.ContactUserId })
            .IsUnique();

        modelBuilder.Entity<Message>()
            .HasIndex(m => new { m.SenderId, m.RecipientId, m.SentAtUtc });
    }
}
