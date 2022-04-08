using MikeCodesDotNET.Models;

using System.Collections.Generic;
using System.Threading.Tasks;

namespace MikeCodesDotNET.Services.Blog;

public interface IBlogService
{
    Task<Result> SyncDatabase();

    Task<BlogPost> GetPost(string postName);

    Task<List<BlogPost>> GetPosts(bool forceRefresh = false);

    Task<bool> AddPost(BlogPost post);

    Task<bool> PublishBlogPost(BlogPost post);

    Task<bool> UnPublishBlogPost(BlogPost post);

    Task<bool> UpdatePost(BlogPost post);

    Task<bool> DeletePost(BlogPost post);


}
