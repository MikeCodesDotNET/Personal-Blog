using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;

using J2N.Collections.Generic;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using MikeCodesDotNET.Models;

using System;
using System.Threading.Tasks;

namespace MikeCodesDotNET.Services;

public class AzureSearchService
{
    private readonly ILogger<AzureSearchService> _logger;
    private readonly SearchIndexClient _adminClient;
    private readonly SearchClient _searchClient;
    private readonly string _indexName;
    private bool _ready;

    public AzureSearchService(ILogger<AzureSearchService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _indexName = configuration.GetValue<string>("AzureSearch:IndexName");

        Uri serviceEndpoint = new Uri($"https://{configuration.GetValue<string>("AzureSearch:ServiceName")}.search.windows.net/");
        AzureKeyCredential credential = new AzureKeyCredential(configuration.GetValue<string>("AzureSearch:ApiKey"));
        _adminClient = new SearchIndexClient(serviceEndpoint, credential);
        _searchClient = new SearchClient(serviceEndpoint, _indexName, credential);
    }

    private async Task<SearchIndex> CreateIndex()
    {
        try
        {
            var searchIndex = await _adminClient.GetIndexAsync(_indexName);
            if (searchIndex != null)
            {
                _ready = true;
                return searchIndex;
            }
        }
        catch(RequestFailedException)
        {
            FieldBuilder fieldBuilder = new FieldBuilder();
            var searchFields = fieldBuilder.Build(typeof(SearchPost));

            var definition = new SearchIndex(_indexName, searchFields);

            var suggester = new SearchSuggester("suggester", new[] { "Title", "Category", "Tags", "Content" });
            definition.Suggesters.Add(suggester);

            var result = await _adminClient.CreateOrUpdateIndexAsync(definition);
            _ready = true;
            return result;
        }
        return null;
    }

    public async Task<bool> UploadPosts(System.Collections.Generic.List<BlogPost> blogPosts)
    {
        if(!_ready)
            await CreateIndex();


        IndexDocumentsBatch<SearchPost> batch = new IndexDocumentsBatch<SearchPost>();
        foreach(var post in blogPosts)
        {
            var sp = new SearchPost()
            {
                Id = post.DirectoryName,
                Title = post.MarkdownContent.Title,
                PublishedAt = post.PublishedTimestamp,
                Url = post.PublishedUrl,
                Category = post.MarkdownContent.Category.Name,
                Content = post.MarkdownContent.PlainText,
                Tags = post.MarkdownContent.Metadata.Tags,
            };
        
            batch.Actions.Add(IndexDocumentsAction.Upload(sp));
        }

        SearchClient ingesterClient = _adminClient.GetSearchClient(_indexName);
        IndexDocumentsResult result = ingesterClient.IndexDocuments(batch);

        return true;
    }


}



public partial class SearchPost
{
    [SimpleField(IsKey = true)]
    public string Id { get; set; }

    [SearchableField(IsSortable = true)]
    public string Title { get; set; }

    [SimpleField(IsFilterable = true, IsSortable = true, IsFacetable = true)]
    public DateTimeOffset? PublishedAt { get; set; }

    [SimpleField]
    public string Url { get; set; }

    [SearchableField(IsFilterable = true, IsFacetable = true)]
    public string[] Tags { get; set; }

    [SearchableField(IsFilterable = true, IsFacetable = true)]
    public string Category { get; set; }

    [SearchableField]
    public string? Content { get; set; }

}
