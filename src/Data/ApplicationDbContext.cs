using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

using MikeCodesDotNET.Models;
using MikeCodesDotNET.Models.Blog;

using System.Diagnostics;
using System.Threading.Tasks;

namespace MikeCodesDotNET.Data
{
    public class ApplicationDbContext : DbContext
    {


        public ApplicationDbContext(IWebHostEnvironment environment)
        {
            dbPath = System.IO.Path.Combine(environment.ContentRootPath, "blogging.db");
        }

        string dbPath;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
            Debug.WriteLine($"{ContextId} context created.");
        }

        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //    modelBuilder.Entity<TrackingCode>(x => x.HasKey(x => x.ContentId));
        //    modelBuilder.Entity<BlogPostContent>(x => x.HasOne(x => x.Category));
        //    modelBuilder.Entity<Category>(x =>
        //    {
        //            x.HasKey(x => x.Name);
        //            x.HasMany(x => x.Posts);
        //    });
        //}


        public DbSet<BlogPost> Posts { get; set; }

        public DbSet<PostMdContent> PostRevisions { get; set; }

        public DbSet<Image> Images { get; set; }

        public DbSet<ExternalLink> ExternalLinks { get; set; }

        public DbSet<Tag> Tags { get; set; }

        public  DbSet<Category> Categories { get; set; }

        /// <summary>
        /// Dispose pattern.
        /// </summary>
        public override void Dispose()
        {
            Debug.WriteLine($"{ContextId} context disposed.");
            base.Dispose();
        }

        /// <summary>
        /// Dispose pattern.
        /// </summary>
        /// <returns>A <see cref="ValueTask"/></returns>
        public override ValueTask DisposeAsync()
        {
            Debug.WriteLine($"{ContextId} context disposed async.");
            return base.DisposeAsync();
        }
    }
}
