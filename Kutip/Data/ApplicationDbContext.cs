using Kutip.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Kutip.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Truck> Trucks { get; set; }
        public DbSet<Bin> Bin { get; set; }
        public DbSet<Schedule> Schedules { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        //dashboard reporting
        public DbSet<PickupEvent> PickupEvents { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // PickupEvent -> Bin
            modelBuilder.Entity<PickupEvent>()
                .HasOne(p => p.Bin)
                .WithMany()
                .HasForeignKey(p => p.RelatedBinId)
                .OnDelete(DeleteBehavior.Restrict);

            // PickupEvent -> Truck
            modelBuilder.Entity<PickupEvent>()
                .HasOne(p => p.Truck)
                .WithMany()
                .HasForeignKey(p => p.RelatedTruckId)
                .OnDelete(DeleteBehavior.Restrict);

            // PickupEvent -> Schedule (nullable)
            modelBuilder.Entity<PickupEvent>()
                .HasOne(p => p.RelatedSchedule)
                .WithMany()
                .HasForeignKey(p => p.RelatedScheduleId)
                .OnDelete(DeleteBehavior.SetNull); // Allow schedule deletion

            // Schedule -> Bin
            modelBuilder.Entity<Schedule>()
                .HasOne(s => s.Bin)
                .WithMany()
                .HasForeignKey(s => s.BinId)
                .OnDelete(DeleteBehavior.Restrict);

            // Schedule -> Truck
            modelBuilder.Entity<Schedule>()
                .HasOne(s => s.Truck)
                .WithMany()
                .HasForeignKey(s => s.TruckId)
                .OnDelete(DeleteBehavior.Restrict);
        }

    }
}
