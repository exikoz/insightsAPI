using Microsoft.EntityFrameworkCore;
using insightsAPI.Models.Entities;

namespace insightsAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Company> Companies { get; set; }
        public DbSet<Benchmark> Benchmarks { get; set; }
        public DbSet<Fact> Facts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Benchmark>()
                .HasKey(b => new { b.SniKod, b.Storleksklass });

            modelBuilder.Entity<Fact>()
                .HasKey(f => new { f.Orgnr, f.ArsBatch, f.TagNamn });

            // Relationships
            modelBuilder.Entity<Fact>()
                .HasOne<Company>()
                .WithMany()
                .HasForeignKey(f => f.Orgnr)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
