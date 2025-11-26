using DetectorVulnerabilitatsDatabase.Models;
using Microsoft.EntityFrameworkCore;

namespace DetectorVulnerabilitatsDatabase.Context
{
    public class DetectorVulnerabilitatsDatabaseContext: DbContext
    {
        public DetectorVulnerabilitatsDatabaseContext(DbContextOptions<DetectorVulnerabilitatsDatabaseContext> options) : base(options){}

        public DbSet<Assets> Assets { get; set; }
        public DbSet<Findings> Findings { get; set; }
        public DbSet<ScanResults> ScanResults { get; set; }
        public DbSet<ScanTask> ScanTasks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Assets>(entity =>
            {
                entity.HasKey(u => u.Id);
            });
                
            modelBuilder.Entity<Findings>(entity =>
            {
                entity.HasKey(u => u.Id);
            });

            modelBuilder.Entity<ScanResults>(entity =>
            {
                entity.HasKey(u => u.Id);

                entity.HasMany(scanResult => scanResult.Findings)
                .WithOne(findings => findings.ScanResults)
                .HasForeignKey(scanResult => scanResult.Scan_result_id)
                .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ScanTask>(entity =>
            {
                entity.HasKey(u => u.Id);

                entity.HasOne(task => task.Asset)
                .WithMany(asset => asset.ScanTasks)
                .HasForeignKey(task => task.Asset_id)
                .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(task => task.ScanResults)
                .WithOne(scanResult => scanResult.ScanTask)
                .HasForeignKey<ScanResults>(scanResult => scanResult.Scan_task_id)
                .OnDelete(DeleteBehavior.Cascade);
            });                
        }
    }
}
