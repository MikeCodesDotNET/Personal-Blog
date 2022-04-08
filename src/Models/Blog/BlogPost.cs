using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace MikeCodesDotNET.Models;

[Table("Posts")]
public class BlogPost : IComparable<BlogPost>
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public int Id { get; set; }

    public string? PublishedUrl { get; set; }

    public string? DirectoryName { get; set; }

    public bool IsPublished => MarkdownContent.Published.HasValue;

    public DateTimeOffset? PublishedTimestamp => MarkdownContent?.Published.Value;

    public ICollection<PostMdContent> Revisions { get; set; }

    [NotMapped]
    public PostMdContent MarkdownContent => Revisions.OrderBy(x => x.Metadata.Published).FirstOrDefault();

    [NotMapped]
    public string PlainText => MarkdownContent.PlainText;

    public ICollection<Tag> Tags { get; set; }


    public static BlogPost CreateEmpty()
    {
        var emptyContent = new PostMdContent();
        emptyContent.Metadata.Title = "New Blog Post";
        emptyContent.Metadata.Description = "An example post";

        var bp = new BlogPost { Revisions = new List<PostMdContent> { emptyContent }, Tags = new List<Tag>() };
        bp.Revisions.Add(emptyContent);
        return bp;
    }

    public int CompareTo(BlogPost other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (ReferenceEquals(null, other)) return 1;

        if(!other.PublishedTimestamp.HasValue && PublishedTimestamp.HasValue)
            return -1;
        else if(other.PublishedTimestamp.HasValue && !PublishedTimestamp.HasValue)
            return 1;
        else if(!other.PublishedTimestamp.HasValue && !PublishedTimestamp.HasValue)
            return 0;
        else
        {
            if(other.PublishedTimestamp.Value == PublishedTimestamp.Value)
                return 0;
            else if(other.PublishedTimestamp.Value > PublishedTimestamp.Value)
                return 1;
            else
                return -1;
        }
    }
}
