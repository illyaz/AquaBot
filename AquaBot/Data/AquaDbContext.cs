using System;
using System.Threading;
using System.Threading.Tasks;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;

namespace AquaBot.Data
{
    public class AquaDbContext : DbContext
    {
        public AquaDbContext(DbContextOptions<AquaDbContext> options)
            : base(options)
        {
        }
  
        public DbSet<GuildConfig> GuildConfigs { get; set; } = null!;

        public override int SaveChanges()
        {
            AddTimestamp();
            return base.SaveChanges();
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            AddTimestamp();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
            CancellationToken cancellationToken = new CancellationToken())
        {
            AddTimestamp();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            AddTimestamp();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void AddTimestamp()
        {
            var time = DateTimeOffset.Now;
            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.Entity is IUpdatedTimestamp updatedTimestamp
                    && entry.State == EntityState.Modified)
                    updatedTimestamp.UpdatedAt = time;

                if (entry.Entity is ICreatedTimestamp createdTimestamp
                    && entry.State == EntityState.Added)
                    createdTimestamp.CreatedAt = time;
            }
        }
    }
}