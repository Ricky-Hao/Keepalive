using Keepalive.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Keepalive.Database.Data
{
    public class KeepaliveContext : DbContext
    {
        public KeepaliveContext(DbContextOptions<KeepaliveContext> options) : base(options)
        {

        }

        public DbSet<User> Users { get; set; }
        public DbSet<KeepaliveRecord> KeepaliveRecords { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().ToTable("User");
            modelBuilder.Entity<KeepaliveRecord>().ToTable("KeepaliveRecord");
        }
    }
}