
using Microsoft.EntityFrameworkCore;
using MikeCodesDotNET.Data;
using MikeCodesDotNET.Models;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MikeCodesDotNET.Services.Blog
{
    public class BlogService : IBlogService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
        private readonly BlogPostStorageService _blogPostImportService;
        private readonly AzureSearchService _searchService;

        public BlogService(IDbContextFactory<ApplicationDbContext> dbContextFactory, BlogPostStorageService blogPostImportService, AzureSearchService searchService)
        {
            _dbContextFactory = dbContextFactory;
            _blogPostImportService = blogPostImportService;           
            _searchService = searchService;
        }

        public async Task<bool> AddPost(BlogPost post)
        {
            try
            {
                var dataContext = await _dbContextFactory.CreateDbContextAsync();
                var result = await dataContext.Posts.AddAsync(post);
                await dataContext.SaveChangesAsync();

                if(result.State == EntityState.Added)
                    return true;

                return false;
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex);
                return false;
            }
        }

        public async Task<bool> DeletePost(BlogPost post)
        {
            try
            {
                var dataContext = await _dbContextFactory.CreateDbContextAsync();
                var result = dataContext.Posts.Remove(post);
                await dataContext.SaveChangesAsync();

                if (result.State == EntityState.Deleted)
                    return true;

                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return false;
            }
        }

        public async Task<bool> UpdatePost(BlogPost post)
        {
            try
            {
                var dataContext = await _dbContextFactory.CreateDbContextAsync();
                var result = dataContext.Posts.Update(post);
                await dataContext.SaveChangesAsync();

                await _blogPostImportService.UpdateBlogPost(post);

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return false;
            }
        }

        public async Task<BlogPost> GetPost(string postName)
        {
            try
            {
                var dataContext = await _dbContextFactory.CreateDbContextAsync();
                var result = dataContext.Posts
                   .Include(x => x.Revisions)
                   .ThenInclude(x => x.Category)
                   .Include(x => x.Revisions)
                   .ThenInclude(x => x.HeaderBackgroundImage)
                   .Include(x => x.Revisions)
                   .ThenInclude(x => x.Metadata)
                   .FirstOrDefault(x => x.DirectoryName == postName);
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return null;
            }
        }

        public async Task<List<BlogPost>> GetPosts(bool forceRefresh = false)
        {
            try
            {
                var result = await GetPosts();

                if(!result.Any())
                {
                    var syncResult = await SyncDatabase();
                    if(syncResult.Success)
                    {
                        //Try again...
                        result = await GetPosts();
                    }
                    else
                    {
                        throw new Exception("Failed to sync posts");
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return new List<BlogPost>();
            }
        }


        private async Task<List<BlogPost>> GetPosts()
        {
            var dataContext = await _dbContextFactory.CreateDbContextAsync();
            var result = await dataContext.Posts
                .Include(x => x.Revisions)
                .ThenInclude(x => x.Category)
                .Include(x => x.Revisions)
                .ThenInclude(x => x.HeaderBackgroundImage)
                .Include(x => x.Revisions)
                .ThenInclude(x => x.Metadata)
                .ToListAsync();

            result.Sort();
            return result;
        }

        public async Task<Result> SyncDatabase()
        {
            try
            {
                var posts = await _blogPostImportService.ImportAllPosts(true, CancellationToken.None);
                if(posts != null)
                    return new SuccessResult();

                return new ErrorResult("Import posts failed. Result was null");
            }
            catch (Exception ex)
            {                
                return new ErrorResult<Exception>("Import posts failed.", ex);
            }
        }

        public Task<bool> PublishBlogPost(BlogPost post)
        {
            throw new System.NotImplementedException();
        }

        public Task<bool> UnPublishBlogPost(BlogPost post)
        {
            throw new System.NotImplementedException();
        }
    }
}
