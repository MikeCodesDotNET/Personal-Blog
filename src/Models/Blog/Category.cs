
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MikeCodesDotNET.Models;

[Table("Categories")]
public class Category
{
    [Key]
    public string Name { get; set; }

    public ICollection<PostMdContent> Posts { get; set; }

}
