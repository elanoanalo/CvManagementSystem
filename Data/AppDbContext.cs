using Microsoft.EntityFrameworkCore;
using CvManagementSystem.Entities;

namespace CvManagementSystem.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<AttributeDefinition> AttributeDefinitions { get; set; }
        public DbSet<AttributeOption> AttributeOptions { get; set; }
        public DbSet<AttributeValue> AttributeValues { get; set; }
        public DbSet<Position> Positions { get; set; }
        public DbSet<PositionTag> PositionTags { get; set; }
        public DbSet<PositionAttribute> PositionAttributes { get; set; }
        public DbSet<Cv> Cvs { get; set; }
        public DbSet<CvComment> CvComments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Тут будем настраивать составные индексы, ограничения и т.д.
            // Это наш следующий шаг — пока оставим пустым
        }
    }
}