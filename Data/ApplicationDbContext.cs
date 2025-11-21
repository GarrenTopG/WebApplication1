using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Data
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        // Existing DbSets
        public DbSet<Claim> Claims { get; set; }
        public DbSet<SupportingDocument> SupportingDocuments { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        // New DbSet for Lecturer
        public DbSet<Lecturer> Lecturers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Claim configuration
            modelBuilder.Entity<Claim>(entity =>
            {
                entity.Property(e => e.HoursWorked).HasPrecision(10, 2);
                entity.Property(e => e.HourlyRate).HasPrecision(10, 2);
                entity.Property(e => e.TotalAmount).HasPrecision(12, 2);

                entity.HasMany(e => e.Documents)
                      .WithOne(d => d.Claim)
                      .HasForeignKey(d => d.ClaimId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // SupportingDocument configuration
            modelBuilder.Entity<SupportingDocument>(entity =>
            {
                entity.Property(d => d.FileName).HasMaxLength(255).IsRequired();
                entity.Property(d => d.FilePath).HasMaxLength(500).IsRequired();
            });

            // Lecturer configuration
            modelBuilder.Entity<Lecturer>(entity =>
            {
                entity.Property(l => l.FullName).HasMaxLength(100).IsRequired();
                entity.Property(l => l.IdNumber).HasMaxLength(20).IsRequired();
                entity.Property(l => l.Email).HasMaxLength(100).IsRequired();
                entity.Property(l => l.DefaultHourlyRate).HasPrecision(10, 2).IsRequired();
                entity.Property(l => l.BankName).HasMaxLength(50).IsRequired();
                entity.Property(l => l.BankAccountNumber).HasMaxLength(20).IsRequired();
            });
        }
    }
}





