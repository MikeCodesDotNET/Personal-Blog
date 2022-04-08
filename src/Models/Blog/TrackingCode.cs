using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MikeCodesDotNET.Models.Blog
{
    [Table("TrackingCodes")]
    public class TrackingCode
    {
        public string Area { get; set; }

        [Key]
        [Required]
        public string DevOpsWorkItemId { get; set; }

        public string Alias { get; set; }

        public ExternalLink Link { get; set; }

        public override string ToString()
{
            return $"?WT.mc_id={Area}-{DevOpsWorkItemId}-{Alias}";
        }
    }
}
