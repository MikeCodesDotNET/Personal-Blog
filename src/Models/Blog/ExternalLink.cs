using MikeCodesDotNET.Models.Blog;
using MikeCodesDotNET.Utilities.Extensions;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace MikeCodesDotNET.Models;

[Table("Links")]
public class ExternalLink
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public int Id { get; set; }

    public string Title { get; set; }

    public Uri Url { get; set; }

    public bool Active { get; set; }

    public List<TrackingCode> TrackingCodes { get; set; }

    public List<PostMdContent> Posts { get; set; }
}


public static class ExternalLinkExt
{
    private static string alias = "mijam";
    private static string trackingCodeSignature = "?WT.mc_id";
    private static List<string> trackableDomains = new List<string>()
    {
        "docs.microsoft.com",
        "azure.microsoft.com",
        "developer.microsoft.com",
        "techcommunity.microsoft.com"
    };

    public static bool RequiresTrackingLinks(this PostMdContent blogPostContent)
    {
        if(blogPostContent != null && blogPostContent.ExternalLinks.Any())
            return blogPostContent.ExternalLinks.Where(x => x.RequiresTracking() == true).Any();

        return false;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="blogPostContent"></param>
    /// <param name="area">The topic of the content</param>
    /// <param name="id">The ID is Azuree DevOps workitem number assoicated with the content.  0000 denotes a social post.</param>
    public static void AppendTrackingLinks(this PostMdContent blogPostContent, TrackableArea area, string id = "0000")
    {
        if(blogPostContent.RequiresTrackingLinks() == false)
            return;

        foreach(var link in blogPostContent.ExternalLinks)
        {
            link.AppendTracker(area, id);
        }
    }


    public static bool RequiresTracking(this ExternalLink link)
{
        if(link.HasTrackingLink())
            return false;

        return trackableDomains.Any(link.Url.AbsoluteUri.Contains);
    }

    public static bool HasTrackingLink(this ExternalLink link)
    {
        if(link.Url.AbsoluteUri.Contains(trackingCodeSignature, StringComparison.OrdinalIgnoreCase))
            return true;
        return false;
    }

    /// <summary>
    /// Tracking codes are case-sensitive, so use all inputs will be converted to lowercase. 
    /// 
    /// </summary>
    /// <param name="link"></param>
    /// <param name="area">The topic of the content</param>
    /// <param name="id">The ID is Azuree DevOps workitem number assoicated with the content.  0000 denotes a social post.</param>
    public static void AppendTracker(this ExternalLink link, TrackableArea area, string id = "0000")
    {
        if(link.HasTrackingLink() || link.RequiresTracking() == false)
            return;


        var url = link.Url.AbsoluteUri;
        if(!trackableDomains.Any(url.Contains))
            return; //Not a trackable link 

        // Example: /?WT.mc_id=area-0000-alias	
        link.Url = new Uri(url.AppendUrlPaths($"{trackingCodeSignature}={area.ToString().ToLower()}-{id}-{alias}"));
    }
}

