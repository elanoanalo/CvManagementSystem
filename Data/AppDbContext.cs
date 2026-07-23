using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CvManagementSystem.Entities;

namespace CvManagementSystem.Data
{
    public class AppDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<AttributeDefinition> AttributeDefinitions { get; set; }
        public DbSet<AttributeOption> AttributeOptions { get; set; }
        public DbSet<AttributeValue> AttributeValues { get; set; }
        public DbSet<Position> Positions { get; set; }
        public DbSet<PositionTag> PositionTags { get; set; }
        public DbSet<PositionAttribute> PositionAttributes { get; set; }
        public DbSet<Cv> Cvs { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<ProjectTag> ProjectTags { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<PositionAttribute>()
                .HasIndex(pa => new { pa.PositionId, pa.AttributeDefinitionId })
                .IsUnique();

            modelBuilder.Entity<AttributeValue>()
                .HasIndex(av => new { av.CandidateId, av.AttributeDefinitionId })
                .IsUnique();

            modelBuilder.Entity<Position>()
                .Property<uint>("xmin")
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            modelBuilder.Entity<AttributeDefinition>()
                .Property<uint>("xmin")
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();
        }
    }
}