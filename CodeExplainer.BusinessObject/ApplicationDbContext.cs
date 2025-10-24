using CodeExplainer.BusinessObject.Models;
using Microsoft.EntityFrameworkCore;

namespace CodeExplainer.BusinessObject;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }
    
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Notification> Notifications { get; set; } = null!;
    public DbSet<ChatSession> ChatSessions { get; set; } = null!;
    public DbSet<ChatMessage> ChatMessages { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasKey(x => x.UserId);
        
        modelBuilder.Entity<Notification>()
            .HasKey(x => x.NotificationId);
        
        modelBuilder.Entity<ChatSession>()
            .HasKey(x => x.ChatSessionId);
        
        modelBuilder.Entity<ChatMessage>()
            .HasKey(x => x.ChatMessageId);
        
        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<User>().HasIndex(u => u.UserName).IsUnique();
        modelBuilder.Entity<User>().Property(d => d.CreatedAt).HasColumnType("timestamp without time zone");
        modelBuilder.Entity<User>().Property(d => d.UpdatedAt).HasColumnType("timestamp without time zone");
        modelBuilder.Entity<Notification>().Property(d => d.CreatedAt).HasColumnType("timestamp without time zone");
        modelBuilder.Entity<ChatSession>().Property(x => x.CreatedAt).HasColumnType("timestamp without time zone");
        modelBuilder.Entity<ChatSession>().Property(x => x.UpdatedAt).HasColumnType("timestamp without time zone");
        modelBuilder.Entity<ChatMessage>().Property(x => x.CreatedAt).HasColumnType("timestamp without time zone");
        
        modelBuilder.Entity<User>()
            .HasMany(x => x.Notifications)
            .WithOne(n => n.User)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<ChatSession>()
            .HasOne(cs => cs.User)
            .WithMany(x => x.ChatSessions)
            .HasForeignKey(cs => cs.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<ChatMessage>()
            .HasOne(cm => cm.ChatSession)
            .WithMany(cs => cs.Messages)
            .HasForeignKey(cm => cm.ChatSessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}