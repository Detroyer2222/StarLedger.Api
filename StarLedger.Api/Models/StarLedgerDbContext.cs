using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace StarLedger.Api.Models;

public sealed class StarLedgerDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
{
    public DbSet<User> Users { get; set; }
    public DbSet<Organization> Organizations { get; set; }
    public DbSet<Resource> Resources { get; set; }
    public DbSet<UserResource> UserResources { get; set; }
    public DbSet<UserBalanceHistory> UserBalanceHistories { get; set; }
    public DbSet<ResourceQuantityHistory> ResourceQuantityHistories { get; set; }
    
    
    public StarLedgerDbContext(DbContextOptions<StarLedgerDbContext> options) 
        :base(options)
    {
        
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    
        // Configure the one-to-one relationship between User and Organization
        modelBuilder.Entity<User>()
            .HasOne(u => u.Organization) // One-to-one relationship to Organization
            .WithMany(o => o.Users)      // Each Organization can have many Users
            .HasForeignKey(u => u.OrganizationId); // Foreign key in User

        modelBuilder.Entity<User>()
            .HasMany(u => u.UserResources)
            .WithOne(ur => ur.User);

        // Composite key for UserResource
        modelBuilder.Entity<UserResource>()
            .HasKey(ur => new { ur.UserId, ur.ResourceId });
    
        // UserBalanceHistory Configuration
        modelBuilder.Entity<UserBalanceHistory>()
            .HasOne(ubh => ubh.User)
            .WithMany()
            .HasForeignKey(ubh => ubh.UserId);

        // ResourceQuantityHistory Configuration
        modelBuilder.Entity<ResourceQuantityHistory>()
            .HasOne(rqh => rqh.User)
            .WithMany()
            .HasForeignKey(rqh => rqh.UserId);

        modelBuilder.Entity<ResourceQuantityHistory>()
            .HasOne(rqh => rqh.Resource)
            .WithMany()
            .HasForeignKey(rqh => rqh.ResourceId);
    }
}