using AuthService.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Data;

public class AuthDbContext : DbContext
{
    //ASP.NET injects database configuration
    //EF pass those setting to AuthDbContext(options) and send to parent class base(options)
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
    {
    }

    //represent a DB table
    public DbSet<User> Users => Set<User>();

    
    //config db table : Tell EF exactly how table should look.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
       //Runs default EF configuration first
       base.OnModelCreating(modelBuilder);

        //Configure User table : Everything inside applies to User
        //entity - nickname of table
        modelBuilder.Entity<User>(entity =>
        {
            //UserId : PK (Take User object and select UserId)
            entity.HasKey(u => u.UserId);
            //Create an index on Email column
            entity.HasIndex(u => u.Email).IsUnique();
            //Configure Name column : Now I'm defining rules for Name
            entity.Property(u => u.Name).IsRequired().HasMaxLength(100);
            //Configure Email column : Now I'm defining rules for Email
            entity.Property(u => u.Email).IsRequired().HasMaxLength(150);
            //Configure PasswordHash column : Now I'm defining rules for it
            entity.Property(u => u.PasswordHash).IsRequired().HasMaxLength(255);
            //Configure Name Phone : Now I'm defining rules for it
            entity.Property(u => u.Phone).IsRequired().HasMaxLength(15);
            //Configure Name Role : Now I'm defining rules for it
            entity.Property(u => u.Role).IsRequired().HasMaxLength(20);
        });
    }
}
