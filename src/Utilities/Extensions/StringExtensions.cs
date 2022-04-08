using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;


using Markdig;
using Markdig.Renderers;

namespace MikeCodesDotNET.Utilities.Extensions;

public static class StringExtensions
{

    public static string ToHtml(this string markdown)
    {
        var htmlWriter = new StringWriter();
        var htmlRenderer = new HtmlRenderer(htmlWriter);

        var mdPipeline = new MarkdownPipelineBuilder()
            .UsePipeTables()
            .UseGenericAttributes()
            .UseBootstrap()
            .UseAutoLinks()
            .UseSoftlineBreakAsHardlineBreak()
            .UseFigures()
            .UseAutoIdentifiers(Markdig.Extensions.AutoIdentifiers.AutoIdentifierOptions.GitHub)
            .UseSmartyPants()
            .UseEmojiAndSmiley()
            .Build();

        Markdig.Markdown.Convert(markdown, htmlRenderer, mdPipeline);
        var html = htmlWriter.ToString();
        return html;
    }


    public static bool IsAbsoluteUrl(this string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out _);
    }


    public static string AppendUrlPaths(this string baseUrl, params string[] paths)
    {
        return paths.Aggregate(
            baseUrl,
            (current, path) => string.Format(
            "{0}/{1}",
            current.Replace(@"\", "/").Replace(@"\\", "/").TrimEnd('/'),
            path.Replace(@"\", "/").Replace(@"\\", "/").TrimStart('/')));
    }

    public class StringConverter : JsonConverter<string>
    {

        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // deserialize numbers as strings.
            if(reader.TokenType == JsonTokenType.Number)
            {
                return reader.GetInt32().ToString();
            }
            else if(reader.TokenType == JsonTokenType.String)
            {
                return reader.GetString();
            }

            throw new System.Text.Json.JsonException();
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }

    }

}
