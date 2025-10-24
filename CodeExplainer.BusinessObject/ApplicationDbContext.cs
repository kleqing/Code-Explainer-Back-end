using CodeExplainer.BusinessObject.Models;
using Microsoft.EntityFrameworkCore;

namespace CodeExplainer.BusinessObject;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }
    
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<CodeRequest> CodeRequests { get; set; } = null!;
    public DbSet<Notification> Notifications { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasKey(x => x.UserId);
        
        modelBuilder.Entity<CodeRequest>()
            .HasKey(x => x.CodeRequestId);
        
        modelBuilder.Entity<Notification>()
            .HasKey(x => x.NotificationId);
        
        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<User>().HasIndex(u => u.UserName).IsUnique();
        modelBuilder.Entity<User>().Property(d => d.CreatedAt).HasColumnType("timestamp without time zone");
        modelBuilder.Entity<User>().Property(d => d.UpdatedAt).HasColumnType("timestamp without time zone");
        modelBuilder.Entity<CodeRequest>().Property(d => d.CreatedAt).HasColumnType("timestamp without time zone");
        modelBuilder.Entity<Notification>().Property(d => d.CreatedAt).HasColumnType("timestamp without time zone");
        
        modelBuilder.Entity<User>()
            .HasMany(u => u.CodeRequests)
            .WithOne(cr => cr.User)
            .HasForeignKey(cr => cr.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<User>()
            .HasMany<Notification>()
            .WithOne(n => n.User)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}