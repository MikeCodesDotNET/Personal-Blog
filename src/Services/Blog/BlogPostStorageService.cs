using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

using MikeCodesDotNET.Data;
using MikeCodesDotNET.Models;
using MikeCodesDotNET.Utilities;
using MikeCodesDotNET.Utilities.Extensions;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YamlDotNet.Core;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization.Utilities;

using YamlDotNet.Serialization;
using YamlDotNet.Core.Events;
using System.Diagnostics;
using Azure;
using System.Text.RegularExpressions;
using Humanizer.Configuration;
using Markdig;
using System.Reflection.Metadata;
using Microsoft.Extensions.Logging;
using MikeCodesDotNET.Pages.Administration;

namespace MikeCodesDotNET.Services.Blog
{
    public class BlogPostStorageService
    {
        private const string MarkdownLinkRegex = @"\[([^]]*)\]\(([^\s^\)]*)[\s\)]";
        private const string MarkdownImageRegex = @"!\[([^]]*)\]\(([^\s^\)]*)[\s\)]";
        private const string PostIdPropertyKey = "PostId";

        private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
        private readonly NavigationManager _navigationManager;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IConfiguration _configuration;

        private readonly BlobContainerClient _blobContainerClient;
        private readonly string _postsContainerName;
        private readonly string _revisionsContainerName;
        private readonly ILogger<BlogPostStorageService> _logger;
        private readonly AzureSearchService _searchService;

        public BlogPostStorageService(IConfiguration configuration, 
                                     IDbContextFactory<ApplicationDbContext> dbContextFactory, 
                                     NavigationManager navigationManager, 
                                     IWebHostEnvironment webHostEnvironment,
                                     ILogger<BlogPostStorageService> logger,
                                     AzureSearchService searchService)
        {
            _configuration = configuration;
            _dbContextFactory = dbContextFactory;
            _navigationManager = navigationManager;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
            _searchService = searchService;

            var blobServiceClient = new BlobServiceClient(configuration.GetConnectionString("Storage"));
            _postsContainerName = configuration.GetValue<string>("BlogConfig:PostContainersName");
            _revisionsContainerName = configuration.GetValue<string>("BlogConfig:RevisionsContainersName");

            if(configuration.GetValue<bool>("BlogConfig:UseEmulatorStorage"))
            {
                _blobContainerClient = new BlobContainerClient("UseDevelopmentStorage=true", _postsContainerName);
                _blobContainerClient.CreateIfNotExists();
            }
            else
            {
                try
                {
                    _blobContainerClient = blobServiceClient.CreateBlobContainer(_postsContainerName);
                }
                catch
                {
                    _blobContainerClient = blobServiceClient.GetBlobContainerClient(_postsContainerName);
                }
            }
        }


        public async Task<IEnumerable<BlogPost>> ImportAllPosts(bool dropExistingsPosts, CancellationToken ct)
        {
            if(dropExistingsPosts)
            {
               var dataContext = await _dbContextFactory.CreateDbContextAsync();

                // This is pretty nasty 
                dataContext.Posts.RemoveRange(dataContext.Posts);
                dataContext.PostRevisions.RemoveRange(dataContext.PostRevisions); //ewww
                dataContext.Categories.RemoveRange(dataContext.Categories); //ewww

                if (_logger is { })
                    _logger.LogInformation("Dropped existing posts from database in preperation for import");
            }


            var tmpPosts = new List<BlogPost>();
            var blobItems = await ListBlobsHierarchicalListing(_blobContainerClient, String.Empty, null);
            foreach (var blobItem in blobItems)
            {
                if (blobItem.Properties.DeletedOn.HasValue)
                    continue;

                switch (blobItem.Properties.ContentType)
                {
                    case "text/plain":
                        try
                        {
                            if(dropExistingsPosts && blobItem.Metadata.ContainsKey(PostIdPropertyKey))
                                blobItem.Metadata.Remove(PostIdPropertyKey);

                            var blogPost = await ImportPost(blobItem, ct);
                            if (blogPost != null)
                                tmpPosts.Add(blogPost);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                            //log it 
                        }
                        break;
                    case "image/png":
                        break;
                }
            }

            await _searchService.UploadPosts(tmpPosts);
            return tmpPosts;
        }

        public async Task<BlogPost> ImportPost(string blobName, CancellationToken ct)
        {
            BlogPost result = null;
           // var postDirPath = _blobContainerClient.Uri.AbsoluteUri.AppendUrlPaths(postDirectoryName);
            await foreach(BlobItem blobItem in _blobContainerClient.GetBlobsAsync())
            {
                if(blobItem.Name == blobName)
                {
                    result = await ImportPost(blobItem, ct);
                    break;
                }  
            }
            return result;
        }

        public async Task<BlogPost> ImportPost(BlobItem blobItem, CancellationToken ct)
        {
            if(blobItem.IsMarkdownDocument() == false)
                throw new ArgumentException("BlobItem is not a markdown document");

            using var dataContext = await _dbContextFactory.CreateDbContextAsync();

            var contentDirName = blobItem.GetRootDirectoryName();
            BlogPost blogPost = null;
            try
            {
                blogPost = dataContext.Posts
                    .Include(x => x.Revisions)
                    .SingleOrDefault(x => x.DirectoryName == contentDirName);
            }
            catch(Exception ex)
            {
                if (_logger is { })
                    _logger.LogError(ex.Message);
            }


            if (blobItem.GetDirectoryName() != _revisionsContainerName && blogPost == null)
            {
                var publishedUrl = string.Empty;
                if(!string.IsNullOrEmpty(_configuration.GetValue<string>("BlogConfig:RootDomainOveride")))
                    publishedUrl = $"{_configuration.GetValue<string>("BlogConfig:RootDomainOveride")}/blog/{blobItem.GetDirectoryName()}";
                else
                    publishedUrl = $"{_navigationManager.BaseUri}blog/{blobItem.GetDirectoryName()}";

                //It's not a post revision and a blog post entry doesn't exist. This means we need to create a new BlogPost and add it to the DB
                blogPost = new BlogPost()
                {
                    DirectoryName = blobItem.GetRootDirectoryName(),
                    Revisions = new List<PostMdContent>(),
                    Tags = new List<Tag>(),
                    PublishedUrl = publishedUrl
                };

                await dataContext.Posts.AddAsync(blogPost, ct);
                await dataContext.SaveChangesAsync(ct);
               
            }
            blobItem.Metadata.Add(PostIdPropertyKey, blogPost.Id.ToString());

            var content = await ConvertBlobItemToPostContent(blobItem);
            await ResolvePostCategories(content, dataContext);


            blogPost.Revisions.Add(content);
            try
            {
                dataContext.Posts.Update(blogPost);
                await dataContext.SaveChangesAsync(ct);
            }
            catch(Exception ex)
            {
                if (_logger is { })
                    _logger.LogError(ex.InnerException.Message);
            }

            _ = await SavePostAsPlainTextToBlobStorage(blobItem, blogPost);

            if (_logger is { })
                _logger.LogInformation($"Added '{blogPost.MarkdownContent.Title}' to database.");

            SetPlainText(blogPost);
            return blogPost;
        }


        private void SetPlainText(BlogPost post)
        {
            var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().UseYamlFrontMatter().Build();
            var plainText = Markdown.ToPlainText(post.MarkdownContent.MarkdownText, pipeline);
            post.MarkdownContent.PlainText = plainText;
        }

        private async Task<bool> SavePostAsPlainTextToBlobStorage(BlobItem postBlobItem, BlogPost blogPost)
        {
            try
            {
                var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().UseYamlFrontMatter().Build();
                var plainText = Markdown.ToPlainText(blogPost.MarkdownContent.MarkdownText, pipeline);
                var contentData = Encoding.UTF8.GetBytes(plainText);
                using(var ms = new MemoryStream(contentData))
                {
                    var fileName = $"{postBlobItem.GetDirectoryName()}/content.txt";
                    try
                    {
                        _ = await _blobContainerClient.UploadBlobAsync(fileName, ms);
                    }
                    catch(RequestFailedException)
                    {
                        ms.Seek(0, SeekOrigin.Begin);
                        var bc = _blobContainerClient.GetBlobClient(fileName);
                        _ = await bc.UploadAsync(ms, overwrite: true);
                    }

                    if(_logger is { })
                        _logger.LogInformation(
                            $"Created plain text varient of '{blogPost.MarkdownContent.Title}' for Azure Search document indexer");
                }
                return true;
            }
            catch(Exception ex)
            {            
                if(_logger is { })
                    _logger.LogError(ex.Message);

                return false;
            }

        }


        private async Task<PostMdContent> ConvertBlobItemToPostContent(BlobItem blobItem)
        {
            var markdownContent = await ReadBlobAsString(blobItem);
            if (string.IsNullOrWhiteSpace(markdownContent))
                return null;

            var content = new PostMdContent()
            {
                DirectoryName = blobItem.GetRootDirectoryName(),
                MarkdownText = markdownContent,
                Metadata = ParsePostHeader<BlogPostMetaHeader>(markdownContent)
            };

            TransformPostImagesToAbsolutePath(content);
            TransformPostTrackingLinks(content);
            return content;
        }




        private void TransformPostImagesToAbsolutePath(PostMdContent content)
        {

            //Handle the cover image
            if (!string.IsNullOrWhiteSpace(content.Metadata.CoverImage))
            {
                try
                {
                    content.HeaderBackgroundImage = new Image()
                    {
                        Url =
                            new Uri(
                                _blobContainerClient.Uri.AbsoluteUri
                                    .AppendUrlPaths(content.DirectoryName.AppendUrlPaths("images".AppendUrlPaths(content.Metadata.CoverImage)))),
                        Title = "Header",
                        Description = "Post Header Image",
                        AltText = "coloured waves with simple icon in middle"
                    };
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to set cover image: {ex.Message}");
                }
            }


            //Handle images found in the post content 
            ProcessMarkdownContent(content.MarkdownText, MarkdownImageRegex, (match) =>
            {
                try
                {
                    if (match.Groups.Count == 3)
                    {
                        var imageTitle = match.Groups[1].Value;
                        var imageUrl = match.Groups[2].Value;

                        if (imageUrl.IsAbsoluteUrl())
                            content.Images.Add(new Image() { Title = imageTitle, Url = new Uri(imageUrl) });
                        else
                        {
                            var absImgUrl = _blobContainerClient.Uri.AbsoluteUri
                                .AppendUrlPaths(content.DirectoryName.AppendUrlPaths(imageUrl));
                            content.Images.Add(new Image() { Title = imageTitle, Url = new Uri(absImgUrl) });
                            content.MarkdownText = content.MarkdownText.Replace(imageUrl, absImgUrl);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            });
        }

        private void TransformPostTrackingLinks(PostMdContent content)
        {
            //ProcessMarkdownContent(
            //    markdownContent,
            //    MarkdownLinkRegex,
            //    (match) =>
            //    {
            //        if (match.Groups.Count == 3)
            //        {
            //            var linkTitle = match.Groups[1].Value;
            //            var linkUrl = match.Groups[2].Value;

            //            var extLink = new ExternalLink() { Title = linkTitle, Url = new Uri(linkUrl) };
            //            content.ExternalLinks.Add(extLink);
            //        }
            //    });
        }

        private async Task ResolvePostCategories(PostMdContent content, ApplicationDbContext dataContext)
        {
            var sCat = content.Metadata.Categories.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(sCat))
            {
                var existingCategory = await dataContext.Categories.FindAsync(sCat);
                if (existingCategory != null)
                    content.Category = existingCategory;
                else
                    content.Category = new Category() { Name = sCat };
            }
        }




        public async Task<bool> UpdateBlogPost(BlogPost blogPost)
        {
            try
            {
                var blobClient = _blobContainerClient.GetBlobClient($"{blogPost.DirectoryName}/index.md");

                using var ms = new MemoryStream();
                using var sw = new StreamWriter(ms);
                sw.Write(blogPost.MarkdownContent.MarkdownText);
                sw.Flush();
                ms.Position = 0;
                await blobClient.UploadAsync(ms, new BlobUploadOptions());
                return true;
            }
            catch (Exception ex)
            {
               Debug.WriteLine(ex.Message);
               return false;
            }
        }

        private async Task<string> ReadBlobAsString(BlobItem blobItem)
        {
            var blobClient = _blobContainerClient.GetBlobClient(blobItem.Name);
            if (await blobClient.ExistsAsync())
            {
                var response = await blobClient.DownloadAsync();
                using var streamReader = new StreamReader(response.Value.Content);
                while (!streamReader.EndOfStream)
                    return await streamReader.ReadToEndAsync();
            }
            throw new FileNotFoundException("Unable to find file for BlobItem");
        }

        private TFrontMatter? ParsePostHeader<TFrontMatter>(string markdownContent) where TFrontMatter : class, IMarkdownFrontMatter, new()
        {
            using (var sr = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(markdownContent))))
            {
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(PascalCaseNamingConvention.Instance)
                    .BuildValueDeserializer();
                var parser = new Parser(sr);

                parser.Consume<StreamStart>();
                parser.Consume<DocumentStart>();

                var result = (TFrontMatter?)deserializer.DeserializeValue(
                    parser,
                    typeof(TFrontMatter),
                    new SerializerState(),
                    deserializer);

                // Currently, the parser has read all the document and stays at the last token of it (*before* closing `---`
                // line). At the same time, it has already peeked the first character of the next line *after* the end of
                // the document from the StreamReader instance.
                //
                // So, we have to rewind the stream. To do that, calculate position of the next document start token, and
                // rewind the basic stream and the stream reader itself.
                _ = parser.Consume<DocumentEnd>();
                var nextDocumentStart = (DocumentStart)parser.Current!;
                var position = nextDocumentStart.End.Index // points *before* the closing `---`
                               +
                    1; // points to the first character of closing `---`
                RewindReaderTo(sr, position);
                _ = sr.ReadLine(); // should be `---`
                return result;

                static void RewindReaderTo(StreamReader r, int position)
                {
                    r.BaseStream.Position = position;
                    r.DiscardBufferedData();
                }
            }
        }

        private void ProcessMarkdownContent(string markdownContent, string regex, Action<Match> action)
        {
            var matches = Regex.Matches(markdownContent, @regex, RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
            foreach (Group group in matches)
            {
                foreach (Match match in group.Captures)
                {
                    try
                    {
                        action.Invoke(match);
                    }
                    catch
                    {
                        continue;
                    }
                }
            }

        }

        private async Task<List<BlobItem>> ListBlobsHierarchicalListing(BlobContainerClient container, string prefix, int? segmentSize)
        {
            try
            {
                var results = new List<BlobItem>();

                // Call the listing operation and return pages of the specified size.
                var resultSegment = container.GetBlobsByHierarchyAsync(prefix: prefix, delimiter: "/")
                    .AsPages(default, segmentSize);

                // Enumerate the blobs returned for each page.
                await foreach (Azure.Page<BlobHierarchyItem> blobPage in resultSegment)
                {
                    // A hierarchical listing may return both virtual directories and blobs.
                    foreach (BlobHierarchyItem blobhierarchyItem in blobPage.Values)
                    {
                        if (blobhierarchyItem.IsPrefix)
                        {
                            // Write out the prefix of the virtual directory.
                            Debug.WriteLine("Virtual directory prefix: {0}", blobhierarchyItem.Prefix);

                            // Call recursively with the prefix to traverse the virtual directory.
                            results.AddRange(await ListBlobsHierarchicalListing(container, blobhierarchyItem.Prefix, null));
                        }
                        else
                        {
                            // Write out the name of the blob.
                            Console.WriteLine("Blob name: {0}", blobhierarchyItem.Blob.Name);
                            results.Add(blobhierarchyItem.Blob);
                        }
                    }
                }
                return results;
            }
            catch (RequestFailedException e)
            {
                Debug.WriteLine(e.Message);
                throw;
            }
        }
    }
}
