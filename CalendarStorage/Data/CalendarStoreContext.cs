using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics;
using System.IO;

namespace CalendarStorage.Data
{
    public class CalendarStoreContext : DbContext
    {
        public DbSet<Owner> Owners { get; set; }
        public DbSet<CalendarSnapshot> Snapshots { get; set; }
        public DbSet<CalendarBlob> DataBlobs { get; set; }

        private string DbPath;

        public CalendarStoreContext()
        {
            this.DbPath = EnvConfig.DbPath;
            if (String.IsNullOrEmpty(this.DbPath))
            {
                this.DbPath = "./storage.db";
            }

            this.Database.Migrate();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.UseSqlite($"Data Source={this.DbPath};");
        }
    }
}
