using Microsoft.EntityFrameworkCore;
using SDPWebApp.Models;

namespace SDPWebApp.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : DbContext(options)
    {
        public DbSet<Document> Documents { get; set; }
        public DbSet<DocumentItem> DocumentItems { get; set; }
        public DbSet<ValidationIssue> ValidationIssues { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<DocumentItem>()
                .Property(p => p.UnitPrice)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<DocumentItem>()
                .Property(p => p.LineTotal)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Document>()
                .Property(p => p.TotalAmount)
                .HasColumnType("decimal(18,2)");
        }
    }
}