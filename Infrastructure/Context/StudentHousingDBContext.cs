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

}
