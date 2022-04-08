using Azure;
using Azure.AI.TextAnalytics;

using Microsoft.Extensions.Configuration;

using MikeCodesDotNET.Models;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace MikeCodesDotNET.Services;

public class SentimentService
{
    private readonly string _endpoint;
    private readonly string _apiKey;
    private readonly TextAnalyticsClient _client;

    public SentimentService(IConfiguration configuration)
    {
        _endpoint = configuration.GetConnectionString("CogsEndpoint");
        _apiKey = configuration.GetConnectionString("CogsKey");
        _client = new TextAnalyticsClient(new Uri(_endpoint), new AzureKeyCredential(_apiKey));
    }

    public async Task AnalyzeSentiment(BlogPost blogPost, CancellationToken ct)
    {
        var response = await _client.AnalyzeSentimentAsync(blogPost.MarkdownContent.MarkdownText, cancellationToken: ct);
        var documentSentiment = response.Value;


    }
}
