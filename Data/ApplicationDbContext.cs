using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Data
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Claim> Claims { get; set; }
        public DbSet<SupportingDocument> SupportingDocuments { get; set; }
        public DbSet<Notification> Notifications { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Decimal precision for Claim properties
            modelBuilder.Entity<Claim>(entity =>
            {
                entity.Property(e => e.HoursWorked)
                      .HasPrecision(10, 2);

                entity.Property(e => e.HourlyRate)
                      .HasPrecision(10, 2);

                entity.Property(e => e.TotalAmount)
                      .HasPrecision(12, 2);

                // Configure relationship with SupportingDocument
                entity.HasMany(e => e.Documents)
                      .WithOne(d => d.Claim)
                      .HasForeignKey(d => d.ClaimId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Optional: configure SupportingDocument table
            modelBuilder.Entity<SupportingDocument>(entity =>
            {
                entity.Property(d => d.FileName)
                      .HasMaxLength(255)
                      .IsRequired();

                entity.Property(d => d.FilePath)
                      .HasMaxLength(500)
                      .IsRequired();
            });
        }
    }
}




