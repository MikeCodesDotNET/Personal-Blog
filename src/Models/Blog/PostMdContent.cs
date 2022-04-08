using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using System.Net.Http;
using System.Threading.Tasks;

using MikeCodesDotNET.Utilities.Extensions;

using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace MikeCodesDotNET.Models;

[Table("Revisions")]
public record class PostMdContent
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public int Id { get; set; }

    [Required]
    public string MarkdownText { get; set; }

    public string PlainText { get; set; }

    public string DirectoryName { get; set; }

    public BlogPostMetaHeader Metadata { get; set; }


    [NotMapped]
    public string Title => Metadata.Title;

    [NotMapped]
    public string Description => Metadata.Description;

    [NotMapped]
    public DateTimeOffset? Published => Metadata.Published;

    [NotMapped]
    public string? ImagePath => Metadata.CoverImage;

    [NotMapped]
    public string? Series => Metadata.Series;




    public Image HeaderBackgroundImage { get; set; }

    public Image HeaderIcon { get; set; }




    public ICollection<Image> Images { get; set; }

    public ICollection<ExternalLink> ExternalLinks { get; set; }

    public ICollection<Tag> Tags { get; set; }

    public Category Category { get; set; }



    public PostMdContent()
    {
        Images = new List<Image>();
        ExternalLinks = new List<ExternalLink>();
        Tags = new List<Tag>();
        Category = new Category() { Name = "None" };
        HeaderBackgroundImage = Image.CreateBackgroundHeader();
        Metadata = new BlogPostMetaHeader();
    }    
}

public static class BlogPostContentExt
{
    public static IReadOnlyList<string> ToHtmlSections(this PostMdContent content, string splitRegex = @"\##\s*\b")
    {
        content.MarkdownText = content.MarkdownText.Replace("# ", "## ");

        var sections = new List<string>();
        var splitMarkdown = Regex.Split(content.MarkdownText, @"\##\s*\b");
        for (int i = 0; i < splitMarkdown.Length; i++)
        {
            if (i == 0)
                continue;
            sections.Add($"## {splitMarkdown[i]}".ToHtml());
        }
        return sections;
    }

    public static string ToReadingTime(this PostMdContent content)
    {
        var t = content.WordCount() / 200.0;
        var minutes = Math.Truncate(t);
        var s = Math.Round((t - minutes) * 0.60, 2);
        var seconds = 15 * Math.Truncate((s / 0.15));
        var duration = TimeSpan.FromMinutes(minutes).Add(TimeSpan.FromSeconds(seconds));

        if(duration.Seconds < 30)
            return $"{duration.Minutes} mins read";
        else
            return $"{duration.Minutes + 1} mins read";
    }

    public static int WordCount(this PostMdContent content)
    {
        int wordCount = 0, index = 0;
        var text = content.MarkdownText;

        // skip whitespace until first word
        while (index < text.Length && char.IsWhiteSpace(text[index]))
            index++;

        while (index < text.Length)
        {
            // check if current char is part of a word
            while (index < text.Length && !char.IsWhiteSpace(text[index]))
                index++;

            wordCount++;

            // skip whitespace until next word
            while (index < text.Length && char.IsWhiteSpace(text[index]))
                index++;
        }

        return wordCount;
    }

    public static void SetLinks(this PostMdContent content)
    {
        if(content.ExternalLinks == null)
            content.ExternalLinks = new List<ExternalLink>();

        var matches = Regex.Matches(content.MarkdownText, @"\[([^]]*)\]\(([^\s^\)]*)[\s\)]", RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        foreach (Group group in matches)
        {
            foreach (Match match in group.Captures)
            {
                try
                {
                    if(match.Groups.Count == 3)
                    {
                        var linkTitle = match.Groups[1].Value;
                        var linkUrl = match.Groups[2].Value;

                        var extLink = new ExternalLink() { Title = linkTitle, Url = new Uri(linkUrl) };
                        content.ExternalLinks.Add(extLink);
                    }
                }
                catch
                {
                    continue;
                }
            }
        }
    }

    public static void SetImages(this PostMdContent content, string postBlobUrl)
    {
        if (content.Images == null)
            content.Images = new List<Image>();

        var matches = Regex.Matches(content.MarkdownText, @"!\[([^]]*)\]\(([^\s^\)]*)[\s\)]", RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);
        foreach (Group group in matches)
        {
            foreach (Match match in group.Captures)
            {
                try
                {
                    if (match.Groups.Count == 3)
                    {
                        var imageTitle = match.Groups[1].Value;
                        var imageUrl = match.Groups[2].Value;

                        if(imageUrl.IsAbsoluteUrl())
                            content.Images.Add(new Image() { Title = imageTitle, Url = new Uri(imageUrl) });
                        else
                        {
                            var absImgUrl = postBlobUrl.AppendUrlPaths(imageUrl);
                            content.Images.Add(new Image() { Title = imageTitle, Url = new Uri(absImgUrl)});
                            content.MarkdownText = content.MarkdownText.Replace(imageUrl, absImgUrl);
                        }
                    }
                }
                catch
                {
                    continue;
                }
            }
        }
    }


    public static async Task<bool> IsActive(this ExternalLink link)
    {      
        try
        {
            using (var result = await _httpClient.GetAsync(link.Url))
            {
                if(result.IsSuccessStatusCode)
                    return true;

                return false;
            }
        }
        catch (WebException)
        {
            return false;
        }
    }

    private static HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };

}