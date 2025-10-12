using Microsoft.EntityFrameworkCore;
using EasyPass.API.Models;

namespace EasyPass.API.Data;

public class EasyPassContext : DbContext
{
    public EasyPassContext(DbContextOptions<EasyPassContext> options) : base(options) { }
    public DbSet<User> Users => Set<User>();
    public DbSet<PasswordEntry> Passwords => Set<PasswordEntry>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Define one-to-many relationship: User -> PasswordEntries
        modelBuilder.Entity<User>()
            .HasMany(u => u.Passwords)
            .WithOne(p => p.User)
            .HasForeignKey(p => p.UserId);

        // Ensure each username is unique
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        base.OnModelCreating(modelBuilder);
    }
}
