using API.Entities;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class ReservationsContext : DbContext
    {
        public ReservationsContext(DbContextOptions<ReservationsContext> options) : base(options){}

        public DbSet<Reservation> Reservations => Set<Reservation>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Reservation>()
                .HasIndex(r => new { r.Date, r.Time })
                .IsUnique();
        }
    }
}
