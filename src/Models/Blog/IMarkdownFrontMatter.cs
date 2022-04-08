
namespace MikeCodesDotNET.Models
{
    public interface IMarkdownFrontMatter
    {
        string? Title { get; set; }
    }

    public interface IMarkdownDocument
    {
        string? Url { get; set; }
        IMarkdownFrontMatter? FrontMatter { get; set; }
        string? Title { get; set; }
        string? Markdown { get; set; }
    }
}
