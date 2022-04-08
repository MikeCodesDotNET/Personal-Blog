using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace MikeCodesDotNET.Models;

public class BlogPostMetaHeader : IMarkdownFrontMatter
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public int Id { get; set; }


    public string? Title { get; set; }

    public DateTimeOffset? Published { get; set; }

    [NotMapped]
    public string[] Tags { get; set; }

    [NotMapped]
    public string[] Categories { get; set; }

    public string? Description { get; set; }

    public string? CoverImage { get; set; }

    public string? Series { get; set; }

    public string? Tag
    {
        get => Tags?.FirstOrDefault();
        set
        {
            if(!string.IsNullOrWhiteSpace(value))
            {
                Tags = new[] { value };
            }
        }
    }

    public string? Category
    {
        get => Categories?.FirstOrDefault();
        set
        {
            if(!string.IsNullOrWhiteSpace(value))
            {
                Categories = new[] { value };
            }
        }
    }


}
