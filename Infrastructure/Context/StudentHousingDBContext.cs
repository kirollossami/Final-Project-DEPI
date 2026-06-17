using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Context;

public class StudentHousingDBContext : IdentityDbContext<User>
{
    public StudentHousingDBContext(DbContextOptions<StudentHousingDBContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(StudentHousingDBContext).Assembly);

        // Configure Identity roles
        modelBuilder.Entity<IdentityRole>().HasData(
            new IdentityRole { Id = "1", Name = "Admin", NormalizedName = "ADMIN" },
            new IdentityRole { Id = "2", Name = "Student", NormalizedName = "STUDENT" },
            new IdentityRole { Id = "3", Name = "LandLord", NormalizedName = "LANDLORD" }
        );

        // Seed admin user
        var adminUser = new User
        {
            Id = "admin-user-id-001",
            UserName = "admin@studenthousing.com",
            NormalizedUserName = "ADMIN@STUDENTHOUSING.COM",
            Email = "admin@studenthousing.com",
            NormalizedEmail = "ADMIN@STUDENTHOUSING.COM",
            EmailConfirmed = true,
            IsActive = true,
            SecurityStamp = Guid.NewGuid().ToString()
        };
        adminUser.PasswordHash = new PasswordHasher<User>().HashPassword(adminUser, "Admin@123456");
        modelBuilder.Entity<User>().HasData(adminUser);

        modelBuilder.Entity<IdentityUserRole<string>>().HasData(
            new IdentityUserRole<string> { UserId = "admin-user-id-001", RoleId = "1" }
        );
    }

    public DbSet<Booking> Bookings { get; set; }
    public DbSet<Complaint> Complaints { get; set; }
    public DbSet<HousingUnit> HousingUnits { get; set; }
    public DbSet<LandLord> LandLords { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<Room> Rooms { get; set; }
    public DbSet<Student> Students { get; set; }
    public DbSet<Wishlist> Wishlists { get; set; }
    public DbSet<CommissionRecord> CommissionRecords { get; set; }

}
