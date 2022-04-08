using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MikeCodesDotNET.Models;

[Table("Tags")]
public class Tag
{
    [Key]
    public string Name { get; set; }


    public ICollection<BlogPost> Posts { get; set; }

}
