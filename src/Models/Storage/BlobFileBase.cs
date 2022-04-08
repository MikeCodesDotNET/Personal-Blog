using System;

namespace MikeCodesDotNET.Models
{
    public class BlobFileBase
    {
        public string Id { get; set; }

        public ContentType Type { get; }

        public string FileName { get; set; }

        public Uri Url { get; set; }

        public DateTimeOffset? LastModified { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public BlobFileBase()
        {
            Id = Guid.NewGuid().ToString();
        }
    }


    public enum ContentType
    {
        Unknown,
        Image,
        Video,
        Markdown,
        Executable,
        Other
    }
}
